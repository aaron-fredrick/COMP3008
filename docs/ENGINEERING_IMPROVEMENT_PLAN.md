# COMP3008 Engineering Improvement Plan

## Purpose

Improve the existing distributed chat application incrementally without changing its core architecture or introducing unrelated technologies.

Technology remains: C#, .NET Framework 4.8, WPF, WCF, NetTcpBinding, controlled polling, WCF Duplex callbacks, and authoritative in-memory server state.

## Priority Roadmap

### P0 — Correctness and distributed-system safety

#### 1. Fix the polling thread model
- Move synchronous WCF polling calls off the WPF Dispatcher/UI thread.
- Use one controlled polling cycle at a time; prevent overlapping requests.
- Stop polling on sign-out and shutdown.
- Handle communication failures without freezing or crashing the client.
- Marshal resulting UI/state updates back to the Dispatcher.
- Keep the interval controlled and configurable.

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

#### 2. Establish and protect server state invariants

Primary invariant:

```text
UserSession.CurrentChannel == X
        ⇔
Channel X contains that user
```

Audit `UserManager`, `ChannelManager`, `MessageRouter`, `CallbackManager`, `FileHandler`, and `ChatService`.

Focus on join, leave, channel switching, sign-out, disconnect, and message delivery. Define state transitions before changing locking. Do not add synchronization indiscriminately.

#### 3. Isolate Duplex callback delivery from state mutation

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

Audit callback lock scope, slow/dead clients, exceptions, registration/removal races, duplicate registration, and disconnect cleanup. A failed callback must not terminate or stall unrelated server work.

#### 4. Replace optimistic integration tests with real assertions

Turn `Chat.Server.Tests` into deterministic tests. A test must fail when behaviour is wrong; success and failure must not both count as a pass.

Coverage:

- Authentication: unique sign-in, duplicate rejection, ID reuse after sign-out/disconnect.
- Channels: creation, duplicate rejection, join, leave, switching, one-channel invariant, concurrent membership changes.
- Public messaging: current-member delivery, no non-member delivery, no pre-join replay, concurrent sends.
- Private messaging: same-channel enforcement, recipient-only delivery, invalid recipient, recipient leaving, multiple conversations.
- Files: valid files, invalid extension, empty file, exactly 2 MB, over 2 MB, member-only download, access after leaving.
- Duplex: registration, message/member/channel/file callbacks, callback failure, disconnect.
- Concurrency: 3–5 clients, concurrent joins/leaves/messages, post-concurrency state consistency.

### P1 — Security, resource handling, and communication design

#### 5. Audit server-side identity and authorization

Review every public WCF operation:

- Is the caller signed in?
- Is the supplied identity valid for the current session?
- Is the caller a member of the relevant channel?
- Is the caller authorized to access the requested state/file/message?

Pay particular attention to client-supplied `userId`, `channelName`, and file identifiers. Strengthen validation without redesigning authentication.

#### 6. Simplify file storage

Prefer:

```text
Metadata → server memory
Contents → server filesystem
```

Load bytes only when explicitly downloaded. Avoid exposing raw internal exception messages. Keep user-facing errors concise and clean up temporary/invalid files.

#### 7. Make configuration consistent

Audit server, polling client, duplex client, test configuration, README, and build scripts. Establish one canonical localhost/default endpoint for each binding. Do not introduce a configuration framework.

#### 8. Improve Duplex push semantics

Prefer:

```text
Server state change
    ↓
Callback containing required update data
    ↓
Client state/UI update
```

over callback-then-immediate-refresh-RPC where practical.

Do not add polling, timers, or refresh buttons to the Duplex client. Explicit user actions such as downloading file bytes remain normal request/response operations.

### P2 — Maintainability and shared client design

#### 9. Reduce coordinator responsibility where justified

Review `PollingSessionCoordinator` and `DuplexSessionCoordinator` after correctness work. Extract only genuine responsibilities that improve testability, reuse, or readability. Do not perform a large abstraction rewrite.

Potential boundaries include session lifecycle, communication, file operations, message operations, and client state/event coordination.

#### 10. Consolidate genuinely shared client functionality

Keep common validation, file helpers, display models, converters, reusable WPF controls, styles/resources, and presentation behaviour in `Chat.Client.Shared`.

Keep polling and Duplex transport behaviour separate. Do not convert the application to MVVM merely for abstraction purposes.

### P3 — CI, quality, documentation, and polish

#### 11. Add GitHub Actions CI

Create `.github/workflows/ci.yml`.

CI must:

1. Run on pushes to `main`.
2. Run on pull requests targeting `main`.
3. Use a Windows runner because the solution targets .NET Framework/WPF/WCF.
4. Build the complete solution in a clean environment.
5. Run deterministic, headless-safe automated tests.
6. Fail on build or test failures.
7. Avoid unrelated tooling.

WPF/manual multi-client verification remains separate from CI.

If the current test harness cannot run deterministically/headlessly, improve the test harness rather than making CI ignore failures.

#### 12. Strengthen automated tests

Prioritize deterministic server/business-rule tests because they provide high-value coverage without requiring an interactive WPF desktop session.

Include validation, user lifecycle, channel lifecycle, membership invariants, public/private routing, file authorization, callback registration/removal, and concurrency-sensitive state transitions.

#### 13. Clean documentation

Keep `README.md`, `docs/PROJECT_PLAN.md`, `docs/WORKING.md`, and this plan synchronized with the implementation.

Clearly distinguish:

```text
Implemented
Verified automatically
Verified manually
Not yet verified
Deferred
```

Documentation must describe actual verified behaviour, not intended behaviour.

#### 14. Final cleanup

After substantive changes:

- remove dead/debugging code
- remove obsolete comments
- improve misleading names
- reduce duplicated validation/transformations
- review exception boundaries
- review logging and resource cleanup
- review configuration
- avoid unrelated refactoring

## Testing Strategy

### Automated / CI

- Server state and validation tests
- User/channel lifecycle tests
- Message routing tests
- File validation and authorization tests
- Deterministic WCF integration tests
- Concurrency tests that do not require interactive WPF

### Manual distributed verification

Run the server plus at least three clients, including both polling and Duplex clients. Verify:

1. Duplicate sign-in and ID reuse.
2. Channel create/join/leave/switch.
3. Public messages only reach current members.
4. Joining after a message does not replay it.
5. Private messages, multiple PM windows, history restoration, and recipient leave.
6. Allowed/blocked file types and the 2 MB boundary.
7. Member-only file download.
8. Polling updates without UI freezes.
9. Duplex updates without timer/refresh fetching.
10. Duplex close/crash cleanup.
11. Three to five concurrent clients remain usable under activity.

Record manual results in `docs/WORKING.md`; source inspection is not manual verification.

## Build

Local build remains:

```powershell
.\build.ps1
```

CI should perform the equivalent clean solution build and test execution using the repository's existing .NET Framework tooling.

## Non-Goals

Do not introduce ASP.NET Core, REST, Entity Framework, SQLite/databases, MAUI, WinUI, Blazor, third-party MVVM frameworks, DI containers, external messaging systems, or generic repositories/factories without a concrete need.

The goal is a technically sound and understandable educational distributed WPF/WCF application, not a production-scale framework exercise.

## Definition of Done

- Polling network calls are off the WPF UI thread.
- Server state transitions are concurrency-safe and invariants are demonstrably maintained.
- Callback failures cannot stall unrelated server operations.
- Automated tests contain real assertions and cover core server behaviour.
- CI builds and runs supported automated tests on Windows.
- Server-side authorization has been audited and corrected.
- File resource handling is sensible.
- Configuration is consistent.
- Duplex core updates are genuinely callback-driven.
- Shared client functionality is not unnecessarily duplicated.
- Documentation matches implementation and verification state.
- Manual distributed scenarios are recorded separately from automated results.
