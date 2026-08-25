# Assignment Part A Implementation and Verification Plan

## Current Code State — 2026-08-25

P0 is merged to `main`. The P1 branch contains the initial file-sharing implementation, server-side file authorization, metadata/content separation, sharded filesystem storage, and structured file-visibility tests. The next work is hardening and completing the remaining assignment-relevant verification rather than rewriting the existing architecture.

This document records implementation evidence and outstanding verification work. It does not claim demonstration marks or manual-test results that have not been performed.

## P1 Work Order

### P1.1 — File-sharing hardening

**Goal:** finish the required file-sharing path and prove server-side authorization.

1. Verify upload validation: extension/type, empty content, and 2 MB limit.
2. Verify metadata is stored separately from file bytes and remains associated with the same `FileId`.
3. Verify sharded content storage (`<root>/<key-prefix>/<key-prefix>/<blob>`) is deterministic and does not expose user-controlled filenames as filesystem paths.
4. Verify only current channel members can retrieve a channel file.
5. Verify invalid/unknown file IDs and unauthorized downloads fail cleanly.
6. Verify storage failure does not leave an accepted metadata record without content.
7. Keep file-list operations metadata-only; downloading bytes is an explicit user action.

**Assignment relation:** this supports the file-sharing requirement while preserving the assignment's rule that a client does not receive previous channel content merely by joining. A newly joined user should see only files created after the join boundary through the normal update path; existing channel-file history is not replayed as a join event.

### P1.2 — Private-message authorization audit

Before adding more PM functionality, verify the existing PM path against the assignment's same-channel rule:

- sender and recipient must both be signed in;
- both must currently belong to the same channel;
- a caller cannot spoof another user's identity;
- pending PM retrieval is scoped to the authenticated/current user;
- leaving the channel prevents further PM delivery according to the existing lifecycle rules.

Add negative integration tests for these cases. Do not broaden PM access merely to make testing easier.

### P1.3 — Polling/Duplex parity

Verify that polling and duplex expose the same server-side semantics while differing only in delivery mechanism:

- public messages;
- PM notifications;
- channel file notifications;
- membership/channel updates.

Polling is allowed to use controlled periodic fetches while a channel/PM view is open. Duplex must receive pushed updates through callbacks without introducing a polling refresh loop.

### P1.4 — WPF responsiveness proof

Close the explicit P0 test gap: prove a slow synchronous WCF operation cannot block the WPF Dispatcher.

The test should arrange a deliberately delayed WCF call, trigger it from the client, and independently prove that the UI Dispatcher continues processing input/timer work while the call is outstanding. The implementation should move blocking polling/network calls away from the Dispatcher thread before this item is marked complete.

### P1.5 — Duplex disconnect hardening

Test both normal close and abnormal process termination. The server must eventually remove the dead duplex session, channel membership, and callback registration without corrupting other sessions. A later user should be able to reuse the released ID when the assignment lifecycle permits it.

### P1.6 — Regression and CI

Run the full build and structured suite after each hardening slice. The final P1 gate is:

- build succeeds;
- all structured unit/integration tests pass;
- file authorization tests pass;
- PM negative tests pass;
- polling/duplex parity tests pass;
- WPF Dispatcher responsiveness test passes;
- disconnect cleanup test passes;
- manual multi-client verification is recorded separately.

## Existing Implementation Evidence

| Area | Current state |
|---|---|
| Server-side file validation | Implemented |
| Channel membership check for upload | Implemented |
| Channel membership check for download | Implemented |
| File metadata/content separation | Implemented |
| Sharded filesystem content storage | Implemented |
| Channel file visibility boundary | Structured test passes |
| PM routing | Implemented; authorization audit remains |
| Polling delivery | Implemented; parity/hardening remains |
| Duplex callbacks | Implemented; disconnect/Dispatcher evidence remains |
| Structured unit/integration suite | Passing before next changes |

## Important Scope Rules

- Do not expose previous channel messages or files merely because a user joins a channel.
- File metadata may be listed/announced without transferring file bytes.
- File bytes are fetched only when the user explicitly downloads/opens a file.
- PMs remain subject to the same-channel lifecycle rule established for the assignment.
- Do not reintroduce periodic duplex polling; C2 requires pushed duplex updates.
- Do not claim PM file transfer is complete unless its server contract, authorization, polling path, duplex callback path, and tests all exist.

## Immediate Next Implementation Slice

Start with **P1.1 file authorization/storage hardening tests**, then **P1.2 PM negative tests**. These are the lowest-risk changes and give us concrete evidence before changing client behavior.

After those tests pass, implement the Dispatcher responsiveness correction and its test, followed by abnormal duplex disconnect cleanup.

## Verification Command

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```

Record actual manual results in `docs/WORKING.md`; do not infer GUI behaviour from server logs alone.
