# Assignment Part A Implementation and Verification Plan

## Current Code State — 2026-08-26

P0 is merged to `main`. The completed hardening work has established server-side file authorization/storage separation, private-message authorization, polling/duplex parity, and duplex disconnect cleanup. Manual WPF verification remains deferred until the end of the engineering cycle and is not represented as completed here.

This document records implementation evidence, engineering hardening work, and outstanding verification. It does not claim demonstration marks or manual-test results that have not been performed.

## P1 Completion

The following hardening slices have been implemented and integrated into the structured test runner:

- File-sharing authorization and storage hardening
- Private-message authorization and user-scoped pending delivery
- Polling/duplex behavioural parity for public messages, private messages, file notifications, and membership updates
- Duplex disconnect cleanup
- Structured regression coverage for the above

The WPF Dispatcher responsiveness proof remains deliberately deferred. A temporary test/comment may remain until the final production-cleanup pass.

## P2 — Engineering Hardening & Production Readiness

P2 is the next implementation phase. Manual multi-client/WPF verification is intentionally deferred until after the automated engineering work.

### P2.1 — Connection lifecycle robustness

**Goal:** make client/session lifecycle deterministic for both polling and duplex clients.

Audit and harden:

1. Normal sign-out and channel cleanup.
2. Abnormal duplex disconnect cleanup.
3. Callback registration and replacement races.
4. Callback removal after disconnect/fault.
5. User-ID reuse after a dead session is cleaned up.
6. Isolation of one failed callback from other clients.
7. Reconnect behaviour without stale callback/session state.

**Current audit:** the `CallbackManager` already catches `CommunicationException`, `TimeoutException`, and `ObjectDisposedException`, removes the callback, signs the user out, removes channel membership, and notifies remaining members. The existing disconnect-cleanup integration test proves the primary abnormal-disconnect path. P2 should therefore focus on lifecycle race cases and stale-session isolation rather than reimplementing the existing cleanup path.

**Implemented:** callback cleanup now verifies that the callback which failed is still the callback registered for the user before removing the session. This prevents an old callback invocation from signing out a replacement session created after a normal sign-out/reconnect. A deterministic integration test holds an old callback invocation in flight, replaces the user session and callback, releases the stale callback to fail, and verifies that the replacement session remains signed in and receives subsequent callback notifications.

**Verification status:** implementation and structured test are committed; local build/integration execution is the next verification step.

### P2.2 — Concurrent state integrity

**Goal:** prove shared server state remains correct under concurrent operations.

Cover:

- simultaneous sign-in/sign-out;
- join/leave races;
- concurrent message sends;
- callback registration/removal races;
- concurrent channel membership changes;
- concurrent private-message/file delivery;
- collection consistency and duplicate membership prevention.

**Current audit:** `UserManager` protects its user/session dictionary with `ReaderWriterLockSlim`, including callback, membership, polling-boundary, and pending-delivery state. P2 should test the higher-level operation sequences and identify any cross-manager race windows rather than adding redundant locks blindly.

### P2.3 — Delivery reliability

**Goal:** verify delivery semantics when users or connections change during delivery.

Cover:

- recipient offline;
- recipient reconnecting;
- sender disconnecting;
- recipient leaving a channel;
- pending private-message/file delivery;
- callback failure during notification;
- no duplicate delivery after recovery.

### P2.4 — File-transfer robustness

**Goal:** extend the completed authorization work with malformed-input and storage-failure coverage.

Cover:

- extension boundaries;
- empty content;
- exact 2 MB boundary and over-limit content;
- invalid/unknown file IDs;
- repeated/concurrent downloads;
- storage failures;
- path traversal attempts;
- metadata/content consistency;
- no filesystem-path leakage.

### P2.5 — Polling/Duplex state-transition parity

The existing parity suite covers core message/file/membership notification behaviour. Extend parity testing to lifecycle and recovery transitions so that polling and duplex expose equivalent server semantics while retaining their different delivery mechanisms.

### P2.6 — Client asynchronous-state robustness

Audit the WPF client state transitions around asynchronous WCF calls and duplex callbacks:

- Dispatcher marshaling;
- background network operations;
- concurrent UI updates;
- shutdown during outstanding operations;
- callback arrival during view/state changes;
- stale client state after reconnect.

The WPF Dispatcher responsiveness test remains a deferred verification item. Do not mark it complete from automated server logs alone.

### P2.7 — Error handling and fault isolation

Verify malformed/invalid operations fail in a controlled way and one bad client cannot destabilise the service or block unrelated clients.

Cover:

- invalid identifiers;
- nonexistent users/channels/files;
- unauthorized operations;
- callback exceptions;
- disconnected clients;
- failed file operations;
- service-side exception boundaries.

### P2.8 — Resource lifecycle

Audit and test disposal/lifecycle behaviour for:

- WCF client channels;
- callbacks;
- streams and file handles;
- polling loops;
- timers/background workers;
- server shutdown.

Repeated connect/use/disconnect cycles must not accumulate stale server state.

### P2.9 — Configuration and deployment audit

Verify production-facing configuration and remove development-only assumptions:

- WCF endpoints/bindings;
- polling/duplex ports;
- file storage configuration;
- message limits;
- allowed extensions;
- logging;
- command-line configuration;
- Debug-only behaviour.

### P2.10 — Final automated regression

Keep one structured integration-test entry point. Do not create disconnected or one-off integration suites.

The final automated gate must include:

- unit tests;
- core polling/duplex integration tests;
- authorization hardening;
- polling/duplex parity;
- disconnect/lifecycle coverage;
- P2 concurrency/reliability/file/error/resource tests.

## P2 Implementation Order

1. **P2.1 — Connection lifecycle robustness** — in implementation; local verification pending
2. **P2.2 — Concurrent state integrity**
3. **P2.3 — Delivery reliability**
4. **P2.4 — File-transfer robustness**
5. **P2.5 — Polling/Duplex state-transition parity**
6. **P2.6 — Client asynchronous-state robustness**
7. **P2.7 — Error handling and fault isolation**
8. **P2.8 — Resource lifecycle**
9. **P2.9 — Configuration/deployment audit**
10. **P2.10 — Final automated regression**

## P2 Scope Rules

- Do not invent requirements merely to increase test count.
- Every new test must trace to a concrete reliability, security, concurrency, or production-readiness concern.
- Do not duplicate existing passing tests unless the new test exercises a distinct failure/race condition.
- Do not weaken assertions to make a test pass.
- Do not reintroduce polling into the duplex client.
- Do not claim WPF/manual behaviour is proven by server-side tests.
- Keep the final structured test runner consolidated.
- Record manual verification separately near the end of the project.

## Verification Command

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```
