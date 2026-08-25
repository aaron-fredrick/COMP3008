# COMP3008 Engineering Improvement Plan

## Purpose

Improve the existing distributed chat application incrementally without changing its core architecture or introducing unrelated technologies.

Technology remains: C#, .NET Framework 4.8, WPF, WCF, NetTcpBinding, controlled polling, WCF Duplex callbacks, and authoritative in-memory server state.

## P0 — Correctness and distributed-system safety

P0 is the current implementation scope. **All P0 work belongs on `feature/p0-correctness` and must be reviewed through PR #1 before merging to `main`.** Do not implement P0 changes directly on `main`.

### 1. Polling correctness and verification

- Move synchronous WCF polling calls off the WPF Dispatcher/UI thread.
- Use one controlled polling cycle at a time; prevent overlapping requests.
- Stop polling on sign-out and shutdown.
- Handle communication failures without freezing or crashing the client.
- Marshal resulting UI/state updates back to the Dispatcher.
- Keep the interval controlled and configurable.
- Verify the implementation builds successfully in a clean Windows/.NET Framework environment.
- If CI exposes compile/runtime issues, fix those issues in the PR before continuing to later P0 work.

Target:

```text
WPF Dispatcher
    ↓ start/coordinate
Background polling worker
    ↓
WCF requests
    ↓
Results
    ↓
Dispatcher.BeginInvoke
    ↓
WPF state/UI
```

### 2. Server state invariants and atomic channel transitions

Primary invariant:

```text
UserSession.CurrentChannel == X
        ⇔
Channel X contains that user
```

Audit `UserManager`, `ChannelManager`, `MessageRouter`, `CallbackManager`, `FileHandler`, and `ChatService`.

Operations requiring explicit state-transition analysis:

- sign-in
- sign-out
- disconnect
- create channel
- join channel
- leave channel
- switch channel
- public message send/delivery
- private message send/delivery

For `JoinChannel`, `LeaveChannel`, and `SwitchChannel`, make the membership/session transition atomic from the perspective of concurrent WCF requests. A user must not be observable as belonging to two channels or to no channel when the operation has completed.

Before adding locks, identify state ownership and the complete transition. Use the simplest suitable synchronization mechanism. Avoid holding state locks across remote WCF callback calls.

### 3. Duplex callback isolation

Use this pattern:

```text
Validate request
    ↓
Mutate authoritative state
    ↓
Capture callback targets
    ↓
Release state locks
    ↓
Deliver callbacks safely
```

Audit:

- callback lock scope
- slow/dead clients
- callback exceptions
- registration/removal races
- duplicate registration
- disconnect cleanup
- callbacks occurring after a client has left/disconnected

A failed callback must not terminate or stall unrelated server work. Remote callback calls must not occur while holding authoritative state locks.

### 4. Convert `Chat.Server.Tests` into real asserted tests

Replace the current optimistic/manual test-harness behaviour with a deterministic automated test project suitable for local execution and CI.

A test must fail when behaviour is wrong. Do not treat a completed call, absence of an exception, or a printed `PASS` message as proof unless the expected state/result is actually asserted.

Minimum coverage:

**Authentication**
- unique sign-in
- duplicate sign-in rejection
- ID reuse after sign-out
- ID reuse after disconnect

**Channels**
- channel creation
- duplicate channel rejection
- join
- leave
- switching
- one-channel invariant
- concurrent membership changes

**Public messaging**
- current-member delivery
- no non-member delivery
- no pre-join replay
- concurrent sends

**Private messaging**
- same-channel enforcement
- recipient-only delivery
- invalid recipient
- recipient leaving
- multiple conversations

**Files**
- valid `.txt` and image files
- invalid extensions
- empty files
- exactly 2 MB
- over 2 MB
- member-only download
- rejection after leaving the channel

**Duplex**
- callback registration
- message callback
- member callback
- channel callback
- file callback
- callback failure
- disconnect cleanup

### 5. Add concurrency tests

Add deterministic tests for the highest-risk shared-state operations. Begin with small client counts rather than uncontrolled stress tests.

Minimum scenarios:

- 3–5 simultaneous clients joining the same channel
- concurrent joins/leaves
- concurrent channel switches
- concurrent public messages
- concurrent sign-out/disconnect during channel activity
- callback failure while other clients remain active

After each concurrency scenario, assert final server invariants rather than merely asserting that no exception was thrown.

The core invariant must hold:

```text
For every connected user:
CurrentChannel == null  OR  exactly one channel contains that user
```

### 6. Make CI enforce the real test suite

CI must remain on the P0 PR branch until the automated test suite is trustworthy.

Required pipeline:

```text
checkout
  ↓
restore/build .NET Framework solution on Windows
  ↓
run deterministic automated tests
  ↓
fail on build/test failure
```

Do **not** remove or bypass a failing test step merely to obtain a green build. If the existing test harness cannot run headlessly/deterministically, convert it first.

CI must:

1. Run on pushes to `main`.
2. Run on pull requests targeting `main`.
3. Use a Windows runner.
4. Build the complete solution.
5. Run headless-safe tests.
6. Fail on test/build failures.
7. Avoid unrelated tooling.

### P0 acceptance criteria

P0 is ready to merge only when all of the following are true:

- polling network calls are off the WPF UI thread
- polling cannot overlap and stops cleanly
- polling communication failures do not freeze/crash the UI
- server channel membership/session transitions are concurrency-safe
- membership invariants are explicitly tested
- callbacks are delivered outside authoritative state locks
- failed/dead callbacks do not stall unrelated clients
- `Chat.Server.Tests` contains real assertions
- concurrency scenarios have deterministic assertions
- local automated tests pass
- CI builds and runs the same supported automated tests successfully
- no unrelated architecture/framework changes were introduced

## Local verification required before PR review

Run these checks locally on Windows before treating P0 as ready.

### Build

```powershell
.\build.ps1
```

Then confirm the full solution builds without errors.

### Automated tests

Run the repository's test command once the test project has been converted. Confirm that:

- all tests pass
- a deliberately broken assertion makes the test run fail (then restore it)
- no test relies on manual console inspection to determine success
- tests clean up WCF hosts, channels, callbacks, temporary files, and server state

### Polling manual test

Run the server and polling client.

1. Sign in.
2. Confirm normal polling updates arrive.
3. Make the server unavailable or otherwise force a communication failure.
4. Confirm the WPF UI remains responsive.
5. Restore the server and confirm the client recovers according to the existing connection behaviour.
6. Sign out and confirm polling stops.
7. Close the client and confirm no background polling continues.
8. If possible, temporarily make a server request slow and confirm the UI remains responsive.

### Distributed manual test

Run the server plus at least three clients, including at least one polling client and one Duplex client.

Verify:

1. Duplicate sign-in is rejected.
2. Sign-out/disconnect makes the ID reusable.
3. Create/join/leave/switch channel works.
4. A user cannot end up in two channels.
5. Public messages only reach current members.
6. Joining after a message does not replay it.
7. Private messages enforce the existing same-channel rules.
8. A client leaving/disconnecting does not corrupt remaining membership.
9. Duplex clients receive callbacks without manual refresh.
10. A disconnected Duplex client does not prevent other clients receiving updates.
11. Three to five clients can perform concurrent activity without visible state corruption.

### File verification

Verify the existing file rules, including:

- valid text/image file
- invalid extension
- empty file
- exactly 2 MB
- greater than 2 MB
- member download allowed
- non-member/left-member download rejected

Record manual results in `docs/WORKING.md`. Source inspection is not manual verification.

## P1 — Security, resource handling, and communication design

### 7. Audit server-side identity and authorization

Review every public WCF operation:

- Is the caller signed in?
- Is the supplied identity valid for the current session?
- Is the caller a member of the relevant channel?
- Is the caller authorized to access the requested state/file/message?

Pay particular attention to client-supplied `userId`, `channelName`, and file identifiers.

### 8. Simplify file storage

Prefer:

```text
Metadata → server memory
Contents → server filesystem
```

Load bytes only when explicitly downloaded. Avoid exposing raw internal exception messages. Keep user-facing errors concise and clean up temporary/invalid files.

### 9. Make configuration consistent

Audit server, polling client, Duplex client, test configuration, README, and build scripts. Establish one canonical localhost/default endpoint for each binding. Do not introduce a configuration framework.

### 10. Improve Duplex push semantics

Prefer callbacks containing the data needed for client state updates over callback-then-immediate-refresh-RPC where practical.

Do not add polling, timers, or refresh buttons to the Duplex client. Explicit user actions such as downloading file bytes remain normal request/response operations.

## P2 — Maintainability and shared client design

### 11. Reduce coordinator responsibility where justified

Review `PollingSessionCoordinator` and `DuplexSessionCoordinator` after correctness work. Extract only genuine responsibilities that improve testability, reuse, or readability.

### 12. Consolidate genuinely shared client functionality

Keep common validation, file helpers, display models, converters, reusable WPF controls, styles/resources, and presentation behaviour in `Chat.Client.Shared`.

Keep polling and Duplex transport behaviour separate. Do not convert the application to MVVM merely for abstraction purposes.

## P3 — Quality, documentation, and polish

### 13. Clean documentation

Keep `README.md`, `docs/PROJECT_PLAN.md`, `docs/WORKING.md`, and this plan synchronized with implementation and verification state.

Clearly distinguish:

```text
Implemented
Verified automatically
Verified manually
Not yet verified
Deferred
```

### 14. Final cleanup

After substantive changes:

- remove dead/debugging code
- remove obsolete comments
- improve misleading names
- reduce duplicated validation/transformations
- review exception boundaries
- review logging and resource cleanup
- review configuration
- avoid unrelated refactoring

## Non-Goals

Do not introduce ASP.NET Core, REST, Entity Framework, SQLite/databases, MAUI, WinUI, Blazor, third-party MVVM frameworks, DI containers, external messaging systems, or generic repositories/factories without a concrete need.

The goal is a technically sound and understandable educational distributed WPF/WCF application, not a production-scale framework exercise.
