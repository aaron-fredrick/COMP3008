# COMP3008 Engineering Improvement Plan

## Purpose
Improve the existing distributed chat application incrementally without changing its core architecture or introducing unrelated technologies. Technology remains C#, .NET Framework 4.8, WPF, WCF, NetTcpBinding, controlled polling, WCF Duplex callbacks, and authoritative in-memory server state.

## P0 — Correctness and distributed-system safety

P0 is complete and merged to `main`. Its polling, state-transition, callback, and deterministic-test foundations are the baseline for P1.

### P0 outcomes carried into P1

- Polling is view-scoped and off the WPF Dispatcher/UI thread.
- `GetPendingMessages` is active-view scoped.
- PM polling respects the same-channel rule.
- Polling requests do not overlap.
- Closing/switching the message view stops the relevant polling context without unnecessarily destroying the WCF client.
- Sign-out/ping/presence communication remains intact.
- Server membership/session transitions and callback delivery have deterministic structured coverage.
- CI builds the complete .NET Framework solution and executes the structured tests.

## P1 — Security, file sharing, resource handling, and communication design

P1 implements and hardens the assignment's file-sharing/private-messaging requirements while preserving the existing WCF polling/Duplex architecture. The key design principle is that **current events are delivered to eligible clients; joining a channel does not replay previous channel content as new events**.

### P1-A — File storage and lifecycle

The current file design is intentionally hybrid:

```text
File metadata / authoritative index → in-memory server state
File bytes / content store          → server filesystem (blob-style)

content key: 48f5748239...
storage:     /48/f5/<content>
```

This is compatible with the assignment because the application still owns the file-sharing behaviour and access control; blob-style filesystem storage is an implementation detail, not a replacement for the WCF service architecture.

For each stored file track metadata such as:

- stable file ID/content key;
- original filename;
- file type/extension;
- byte size;
- uploader/author;
- channel;
- upload timestamp;
- last-updated timestamp where applicable.

Metadata does **not** alter the content key. The key identifies the stored bytes; metadata describes those bytes.

Required lifecycle:

```text
Upload request
  → authenticate/authorize
  → validate filename/type/size/content
  → generate/validate content key
  → write content safely
  → publish metadata only after content succeeds
  → notify eligible current channel members

Download request
  → authenticate/authorize
  → validate file ID
  → resolve sharded path from server-side key
  → read bytes
  → return content
```

Never derive storage paths directly from user-controlled filenames. Prevent traversal and malformed paths. Failed/partial writes must not leave accepted metadata pointing at missing content.

### P1-B — File constraints and security

Enforce server-side, not only in WPF:

- maximum file size: **2 MB**;
- permitted file types/extensions according to the assignment;
- reject empty/invalid files where required;
- reject unknown file IDs;
- reject unauthorized downloads/uploads;
- reject spoofed uploader identity;
- reject path traversal and unsafe filenames;
- ensure metadata/content association is atomic from the application's perspective;
- clean temporary/partial files after failed writes.

### P1-C — File visibility / no-history boundary

The assignment requirement that clients do not see previous messages on joining applies to file notifications as well.

Therefore:

```text
User joins channel at T
        ↓
No replay of files uploaded before T
        ↓
Files uploaded after T may generate notifications
```

This does **not** mean previously stored bytes must be physically deleted. Storage persistence and event visibility are separate concerns. A currently authorized user may access a file through an explicit file operation according to the application's authorization rules; joining must not manufacture historical file events.

The existing `Channel file visibility boundary` integration test is the starting regression test for this rule.

### P1-D — Private-message authorization

PMs must continue to obey the assignment's same-channel rule.

Test and enforce:

```text
sender signed in
AND recipient signed in
AND sender and recipient are in the same channel
        → PM allowed

otherwise
        → PM rejected / not delivered
```

Also verify:

- caller cannot spoof another sender identity;
- a user cannot retrieve another user's pending PMs;
- leaving the shared channel changes PM eligibility according to the lifecycle rule;
- PM polling is only active for the appropriate open channel/message context;
- PMs are not accidentally exposed through public-channel polling.

### P1-E — Server-side authorization audit

Audit every public WCF operation using the security flow:

```text
Caller identity
    ↓
Signed-in session
    ↓
Authoritative current channel
    ↓
Requested resource
    ↓
Authorization decision
```

Client-side UI restrictions are not a security boundary. The server must independently validate identity, membership, resource ownership/access, and resource existence.

Particular attention should go to ChatService, UserManager, ChannelManager, FileHandler, message routing, and callback registration.

### P1-F — Polling and Duplex parity

Polling and Duplex should have equivalent **server semantics**, but different delivery mechanisms.

Polling:

```text
active view
  → controlled background poll
  → pending updates
  → Dispatcher/UI update
```

Duplex:

```text
server state change
  → validated callback targets
  → callback notification
```

Do not introduce periodic polling into the Duplex client. File polling, where required, should retrieve lightweight metadata/events rather than repeatedly transferring file bytes. Actual file content should be obtained only by explicit download/open behaviour.

### P1-G — Resource/concurrency hardening

Audit file I/O and WCF calls for blocking operations that could hold authoritative locks or block unrelated users.

Maintain the P0 callback rule:

```text
Validate → mutate authoritative state → capture callback targets
→ release locks → invoke remote callbacks
```

Never hold server state locks across file I/O or remote WCF calls.

### P1-H — Duplex disconnect cleanup

Verify normal close and abnormal client termination. Dead callback registrations must eventually be removed and must not prevent unrelated clients from communicating.

Test:

- client closes cleanly;
- client process disappears;
- callback invocation fails;
- another client can still send/receive afterwards;
- stale membership/session state is removed consistently.

### P1-I — Dispatcher responsiveness regression

P0 established that polling is off the UI thread, but one important proof gap remains: the automated suite does not yet prove that the WPF Dispatcher remains responsive while a WCF call is deliberately slow.

Add a regression test or test harness that:

1. starts a deliberately delayed WCF operation;
2. starts the operation from the client polling path;
3. attempts a Dispatcher/UI action while the call is outstanding;
4. asserts the Dispatcher continues processing before the WCF operation completes.

This protects the assignment-relevant WPF responsiveness requirement against future regressions.

## P1 implementation order

### Step 1 — Complete deterministic file unit tests

Cover:

1. metadata/content association;
2. sharded storage path derivation;
3. 2 MB boundary;
4. invalid type/empty file;
5. invalid file ID;
6. path traversal;
7. failed/partial storage cleanup.

### Step 2 — Complete file authorization tests

Cover upload/download with:

- authorized channel member;
- non-member;
- unsigned user;
- spoofed uploader identity;
- missing/unknown file.

### Step 3 — Complete PM negative tests

Cover different-channel PM, spoofed identity, recipient isolation, unsigned users, and post-leave behaviour.

### Step 4 — Verify polling/Duplex parity

Cover public messages, PMs, membership changes, and file notifications. Confirm that file bytes are not transferred by ordinary polling.

### Step 5 — Add Dispatcher responsiveness regression

Prove a deliberately slow WCF operation cannot freeze the WPF Dispatcher.

### Step 6 — Add Duplex disconnect tests

Verify cleanup after normal and abnormal termination and callback failure.

### Step 7 — Final P1 regression gate

P1 is ready for review only when:

- `build.ps1` succeeds;
- structured unit/integration tests pass;
- file security/resource tests pass;
- PM negative tests pass;
- polling/Duplex parity tests pass;
- Dispatcher responsiveness is automatically or deterministically proven;
- disconnect cleanup tests pass;
- manual multi-client file/PM verification is recorded separately.

## P2 — Maintainability and shared client design

Review polling/Duplex coordinators and consolidate genuinely shared client validation, file helpers, models, converters, WPF controls, styles/resources, and presentation behaviour in `Chat.Client.Shared`. Keep polling and Duplex transport behaviour separate; do not introduce MVVM merely for abstraction.

## P3 — Quality and documentation

Keep `README.md`, `docs/PROJECT_PLAN.md`, `docs/WORKING.md`, and this plan synchronized. Distinguish Implemented, Automatically Verified, Manually Verified, Not Yet Verified, and Deferred. Remove dead/debugging code and unrelated refactoring after substantive changes.

## Non-Goals

Do not introduce ASP.NET Core, REST, Entity Framework, SQLite/databases, MAUI, WinUI, Blazor, third-party MVVM frameworks, DI containers, external messaging systems, or generic repositories/factories without a concrete need.

## Local verification

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```

Manual results should be recorded in `docs/WORKING.md` rather than inferred from server logs alone.
