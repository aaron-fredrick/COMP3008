# COMP3008 Engineering Improvement Plan

## Purpose
Improve the existing distributed chat application incrementally without changing its core architecture or introducing unrelated technologies. Technology remains C#, .NET Framework 4.8, WPF, WCF, NetTcpBinding, controlled polling, WCF Duplex callbacks, and authoritative in-memory server state.

## P0 — Correctness and distributed-system safety

All P0 work belongs on `feature/p0-correctness` and is reviewed before merging to `main`.

### 1. Polling correctness, lifecycle, and verification

Polling is **view-scoped**, not globally active for the whole signed-in session.

- Move synchronous WCF polling calls off the WPF Dispatcher/UI thread.
- Call `GetPendingMessages` only while a channel/message view is open and an active channel context exists.
- PM polling follows the active channel context because the existing application rule requires PM participants to be in the same channel.
- On channel switch, stop the old polling context before starting the new one.
- Allow at most one polling request at a time; prevent overlap.
- Keep the interval controlled/configurable.
- Handle communication failures without freezing/crashing the client.
- Marshal state/UI updates back to the Dispatcher.
- Separate **polling lifetime** from **WCF transport lifetime**.
- Closing a message view stops message polling but does not automatically destroy the WCF client.
- Sign-out must preserve any required WCF communication for ping/presence/sign-out semantics; inspect the existing protocol before changing transport lifetime.
- Application shutdown stops polling and prevents new requests; transport teardown occurs according to the overall communication lifecycle.

Target:

```text
WPF Dispatcher
    ↓ coordinate active view
Background polling worker
    ↓
WCF GetPendingMessages(active channel)
    ↓
Results
    ↓
Dispatcher.BeginInvoke
    ↓
WPF state/UI
```

Core invariants:

```text
At most one GetPendingMessages request is active per client.
No message polling occurs without an active channel/message view.
Switching/closing the view stops the previous polling context.
Stopping message polling does not implicitly destroy required WCF transport.
```

### 2. Server state invariants and atomic channel transitions

Primary invariant:

```text
UserSession.CurrentChannel == X
        ⇔
Channel X contains that user
```

Audit UserManager, ChannelManager, MessageRouter, CallbackManager, FileHandler, and ChatService. Analyze sign-in, sign-out, disconnect, create/join/leave/switch, public messaging, and private messaging. Keep state transitions atomic and never hold authoritative state locks across remote WCF callbacks.

### 3. Duplex callback isolation

Use:

```text
Validate → mutate authoritative state → capture callback targets
→ release locks → deliver callbacks safely
```

Failed/slow/dead callbacks must not stall unrelated clients. Audit registration/removal races, duplicate registration, disconnect cleanup, and callbacks after leave/disconnect.

### 4. Real asserted tests

`Chat.Server.Tests` must contain deterministic assertions. Minimum coverage: authentication, channels, public messaging, private messaging, files, Duplex callbacks, failure cases, and disconnect cleanup. Tests must assert expected state/results rather than treating completed calls or printed PASS messages as success.

### 5. Concurrency tests

Cover concurrent joins/leaves, switches, public messages, sign-out/disconnect, and callback failure. Assert final invariants, including:

```text
For every connected user:
CurrentChannel == null OR exactly one channel contains that user.
```

### 6. CI

Windows CI must build the complete .NET Framework solution and run the same deterministic automated tests. Build/test failures must fail the pipeline. Do not bypass failing tests.

## P0 acceptance criteria

- polling is off the WPF UI thread
- `GetPendingMessages` is active-view scoped
- PM polling respects the same-channel rule
- polling cannot overlap
- view switching/closing stops the old polling context
- communication failures do not freeze/crash the UI
- polling lifecycle is separated from required WCF transport/session lifetime
- sign-out/ping/presence semantics remain intact
- server membership/session transitions are concurrency-safe
- callbacks execute outside authoritative state locks
- failed callbacks do not stall unrelated clients
- automated tests contain real assertions
- concurrency tests assert invariants
- local tests pass
- CI builds/runs the supported tests
- no unrelated architecture/framework changes

## Local verification

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```

Also manually verify polling responsiveness during server delay/failure, view-scoped polling, channel switching, sign-out/ping behaviour, multi-client state consistency, Duplex failure isolation, private-message same-channel enforcement, and file authorization/size/type rules. Record manual results in `docs/WORKING.md`.

## P1 — Security, resource handling, and communication design

### 7. Server-side identity and authorization
Audit every public WCF operation for signed-in identity, current session validity, channel membership, and authorization of requested state/files/messages.

### 8. File storage
Keep metadata in memory and contents on the server filesystem; load bytes only for explicit downloads and clean up invalid/temporary files.

### 9. Configuration
Audit server, clients, tests, README, and scripts for canonical endpoints without introducing a configuration framework.

### 10. Duplex semantics
Prefer callbacks containing sufficient state-update data where practical. Do not add polling to the Duplex client.

## P2 — Maintainability and shared client design

Review polling/Duplex coordinators and consolidate genuinely shared client validation, file helpers, models, converters, WPF controls, styles/resources, and presentation behaviour in `Chat.Client.Shared`. Keep polling and Duplex transport behaviour separate; do not introduce MVVM merely for abstraction.

## P3 — Quality and documentation

Keep `README.md`, `docs/PROJECT_PLAN.md`, `docs/WORKING.md`, and this plan synchronized. Distinguish Implemented, Automatically Verified, Manually Verified, Not Yet Verified, and Deferred. Remove dead/debugging code and unrelated refactoring after substantive changes.

## Non-Goals

Do not introduce ASP.NET Core, REST, Entity Framework, SQLite/databases, MAUI, WinUI, Blazor, third-party MVVM frameworks, DI containers, external messaging systems, or generic repositories/factories without a concrete need.
