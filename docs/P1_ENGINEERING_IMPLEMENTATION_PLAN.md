# P1 Engineering Implementation Plan

## Purpose

P1 builds on the P0 correctness baseline. The goal is to harden the assignment-required chat/file functionality without changing the core architecture: C#, .NET Framework 4.8, WPF, WCF, BasicHttp polling, NetTcp duplex callbacks, authoritative in-memory server state, and server-side filesystem content storage.

## Current repository baseline — 2026-08-25

The `feature/p1-implementation` branch contains the first P1 file-storage increment. The latest reported deterministic suite result is **6 passed, 0 failed**. The local full solution build also succeeded after the latest storage/test corrections.

These results prove the current automated server/integration paths. They do **not** prove WPF Dispatcher responsiveness, abnormal disconnect cleanup, or full cross-client manual behaviour.

## P1 work breakdown

### P1.1 — File-sharing correctness and lifecycle

**Status: substantially implemented; hardening remains.**

The current design separates file metadata from file bytes. `FileHandler` owns in-memory `SharedFile` metadata while `IFileContentStore`/`ShardedFileContentStore` persist content on the server filesystem. The storage key is derived from the file ID and content is sharded into nested directories, e.g. a key beginning `48f574...` can be stored under `StoredFiles/48/f5/...`. Metadata such as uploader, upload time and last-updated time remains application state and does not change the content key.

The server already enforces the important assignment boundary: channel files are visible only to authorised channel members and the join-time visibility boundary prevents a new member from receiving files uploaded before they joined.

Remaining work:
- audit every upload/list/download operation for authenticated identity and current channel membership;
- verify content-store failure cleanup cannot leave orphaned metadata or bytes;
- verify duplicate/empty/oversized/unsupported files are rejected deterministically;
- verify file downloads load bytes only for an explicit authorised download;
- add negative tests for non-members and invalid file identifiers;
- manually verify file upload/download with both polling and duplex clients.

**Assignment relation:** directly supports A6/B5 file sharing. The server controls visibility and authorisation; clients do not directly access server storage.

**Lecture/lab relation:** demonstrates service-boundary validation, distributed state ownership, RPC, and separation of metadata/state from persisted content.

### P1.2 — Private messaging correctness

**Status: partially implemented; requires a focused audit/test pass.**

Current code already has server-mediated PM routing, same-channel enforcement, per-session PM retrieval, PM history behaviour and leave-channel lifecycle handling. The remaining question is whether every service path consistently enforces the same authorization invariant.

Required invariant:

```text
Sender signed in
AND recipient signed in
AND sender and recipient satisfy the assignment's same-channel rule
        => PM may be accepted
otherwise
        => PM is rejected
```

Remaining work:
- audit `SendPrivateMessage` and pending-PM retrieval for identity/session validation;
- verify only the intended recipient can consume a PM;
- verify a PM cannot cross the channel boundary after either participant leaves;
- add negative tests for sender/recipient outside the permitted channel relationship;
- verify repeated sign-in/sign-out cannot expose stale PM queues to a reused user ID.

### P1.3 — Polling/duplex behavioural parity

**Status: partially implemented; needs verification rather than a redesign.**

The polling client uses explicit retrieval while the duplex client uses callbacks. They should nevertheless expose the same authoritative server semantics.

Remaining work:
- compare public-message, PM, membership and file behaviour across both transports;
- verify duplex callbacks are emitted only after server-side validation/state mutation;
- verify callback unregister/sign-out/leave behaviour;
- verify a failed callback cannot block unrelated clients;
- verify file notifications do not bypass the same membership/visibility rules used by polling;
- add focused cross-transport regression coverage where a deterministic server test can prove the invariant.

**Lecture/lab relation:** reinforces RPC, polling versus asynchronous callback notification, and consistency across different communication mechanisms.

### P1.4 — Client lifecycle and WPF UI robustness

**Status: remaining P1 work.**

P0 moved synchronous WCF polling away from the WPF Dispatcher. One explicit P0 carry-over test is still required: a deliberately delayed WCF call must not prevent the Dispatcher from processing UI work.

Remaining work:
- add a deterministic client-side responsiveness test using a delayed/fake WCF operation;
- prove the Dispatcher can process a UI action while network I/O is delayed;
- verify only one polling loop exists per active channel view;
- verify timers/workers stop on channel close/switch/leave/sign-out/shutdown;
- verify ping/session communication remains independent of channel-view polling;
- verify repeated sign-in/sign-out does not accumulate timers, workers or subscriptions.

**Important:** do not reintroduce polling into the duplex client. The duplex client remains callback-driven.

### P1.5 — Server identity, authorization and fault boundaries

**Status: remaining P1 work.**

Perform a service-operation audit rather than assuming that successful normal-path tests prove authorization.

For every public WCF operation, check:

```text
authenticated session?
        ↓
requested resource owned/visible to caller?
        ↓
current channel relationship valid?
        ↓
validate input
        ↓
mutate/read authoritative state
```

Remaining work:
- audit sign-in/sign-out, channel operations, messages, PMs and files;
- identify operations that accept a caller/user ID supplied by the client and verify the ID corresponds to the current authenticated session;
- ensure failed operations do not leave partial membership/session/file state;
- test invalid channel/file/user identifiers;
- test representative WCF communication failures;
- improve abnormal duplex disconnect cleanup so failed callbacks remove the affected session safely rather than relying only on later activity.

### P1.6 — Test and CI hardening

**Status: partially implemented.**

The structured test suite now gives meaningful pass/fail output and the integration runner exits cleanly on the current successful path. The next step is to increase coverage around the P1 invariants rather than adding tests merely for line coverage.

Remaining work:
- add PM authorization/negative tests;
- add file authorization/download-failure tests;
- add storage cleanup/failure tests;
- add callback failure/isolation tests;
- add repeated lifecycle/concurrency tests where deterministic;
- add the WPF Dispatcher responsiveness test;
- keep unit and integration tests independently executable;
- verify CI returns non-zero on actual failures and zero on successful termination.

## Implementation order from the current state

1. **P1.2 PM authorization audit + negative tests** — small and directly assignment-relevant.
2. **P1.1 file authorization/storage failure hardening** — complete the file-storage boundary.
3. **P1.3 polling/duplex parity tests** — prove both transports obey the same server state rules.
4. **P1.4 delayed-WCF Dispatcher test + lifecycle checks** — close the explicit P0 carry-over.
5. **P1.5 fault/disconnect handling** — harden abnormal distributed failures.
6. **P1.6 final regression/CI pass** — only after the above are stable.

Each item follows the project lifecycle: inspect current code → make the smallest change → build → run deterministic tests → inspect failures → correct → repeat → update documentation.

## P1 acceptance gate

P1 is complete only when:

- existing P0 tests remain green;
- file metadata/content separation is preserved;
- channel file visibility is join-boundary correct;
- file upload/download authorization is server-enforced;
- PM same-channel authorization is server-enforced and negatively tested;
- polling and duplex expose equivalent authorised behaviour;
- the WPF Dispatcher remains responsive during delayed WCF I/O;
- polling/view/session resources are cleaned up correctly;
- representative communication and callback failures do not corrupt authoritative state;
- deterministic tests cover each P1 defect fixed;
- CI reliably distinguishes pass/fail and terminates cleanly;
- no unrelated framework/architecture changes have been introduced.

## Explicit deferred feature: private-file transfer

The repository currently contains private-file storage support in `FileHandler`, but the public private-file WCF delivery path is **not** treated as complete merely because the UI contains private-file presentation work. Do not claim private-file transfer as implemented until the service contract, authorization, polling/duplex notification paths, download operation, tests and both clients are actually implemented and verified.

## P0 carry-over TODO

> Add a client-side behavioural test using a deliberately delayed WCF operation and assert that the WPF Dispatcher continues processing UI work while polling is blocked on network I/O.
