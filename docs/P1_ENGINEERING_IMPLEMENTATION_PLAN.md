# P1 Engineering Implementation Plan

## Purpose

P1 builds on the P0 correctness baseline. The goal is to add the next assignment-required capabilities without weakening the concurrency, session-lifecycle, polling, duplex, and bounded-queue guarantees established in P0.

## P0 baseline carried forward

- Polling and duplex service contracts remain compatible.
- Channel membership is maintained atomically.
- Message history is bounded per channel.
- Polling message retrieval is scoped to the active channel view.
- Private-message polling remains available while the PM/channel relationship is valid.
- Ping remains independent of channel-view polling and continues through session lifecycle as required.
- WCF calls are kept off the WPF Dispatcher.
- Shared WCF proxy access is serialized where required.
- Structured unit/integration tests remain the regression gate.

## P1 work breakdown

### P1.1 — File-sharing correctness and lifecycle

**Concept:** Treat file metadata and file transfer as channel-scoped distributed state rather than UI-only state.

Tasks:
- Verify file upload/share validation at the service boundary.
- Verify files are visible only in the appropriate channel context.
- Preserve channel membership authorization for file operations.
- Ensure polling refreshes files only while the channel view is active.
- Define behaviour for duplicate names, missing files, and invalid requests.
- Add deterministic unit/integration coverage.

**Assignment relation:** supports the file-sharing requirement and the distributed-client/server separation.

### P1.2 — Private messaging correctness

**Concept:** PM delivery is a server-mediated operation with explicit authorization based on the assignment's same-channel rule.

Tasks:
- Validate sender and recipient sessions.
- Require both users to satisfy the channel relationship required by the assignment.
- Queue PMs independently from public channel history.
- Preserve polling retrieval semantics for PMs.
- Ensure PM queues are drained only by the intended recipient.
- Add negative tests for users outside the permitted channel relationship.

**Assignment relation:** directly supports private messaging while demonstrating server-side validation and state management.

### P1.3 — Polling/duplex behavioural parity

**Concept:** Both clients should expose equivalent chat semantics while using different transport mechanisms.

Tasks:
- Compare polling and duplex behaviour for sign-in, channel membership, messages, PMs, and files.
- Ensure duplex callbacks do not bypass server authorization/state rules.
- Verify unregister/sign-out stops callback delivery.
- Verify polling continues to use explicit retrieval rather than WCF callbacks.
- Add cross-transport regression tests where practical.

**Lecture/lab relation:** reinforces RPC, callbacks, polling, asynchronous notification, and distributed-state consistency concepts.

### P1.4 — Client lifecycle and UI robustness

**Concept:** Network operations must not make the WPF UI unresponsive and must not leak timers/subscriptions.

Tasks:
- Complete the P0 TODO for a delayed-WCF Dispatcher responsiveness test.
- Verify polling timers are stopped/disposed when appropriate.
- Verify a session cannot accidentally create duplicate polling loops.
- Verify channel-view refreshes stop after leaving the channel.
- Verify sign-out and shutdown clean up resources.
- Add tests for repeated sign-in/sign-out and channel transitions.

### P1.5 — Error handling and fault boundaries

**Concept:** Distributed systems fail independently; client and server must handle transport/service failures without corrupting shared state.

Tasks:
- Identify expected WCF communication failures and service exceptions.
- Ensure failed operations do not leave partial membership/session state.
- Prevent a polling exception from killing the polling lifecycle permanently.
- Provide deterministic recovery/reconnect behaviour where required.
- Add fault-injection tests for representative failures.

### P1.6 — Test and CI hardening

Tasks:
- Keep unit tests separated from integration tests.
- Keep integration tests deterministic and independent of execution order.
- Add explicit regression tests for every P1 defect fixed.
- Ensure CI returns a non-zero exit code on actual test failure.
- Ensure successful structured tests return zero and terminate cleanly.
- Preserve integration artifacts for diagnosis.

## Implementation order

1. P1.1 File-sharing correctness
2. P1.2 Private messaging correctness
3. P1.3 Polling/duplex parity
4. P1.4 Client lifecycle/UI robustness
5. P1.5 Error handling/fault boundaries
6. P1.6 Test and CI hardening

Each item should be implemented as a small change, built locally, tested locally, and then committed to this branch.

## P1 acceptance gate

P1 is not complete until:

- all existing P0 tests still pass;
- every new P1 requirement has deterministic coverage;
- polling remains non-blocking with respect to the WPF Dispatcher;
- channel/member/file/message lifecycle rules remain correct;
- PM authorization matches the assignment requirement;
- duplex and polling clients remain behaviourally consistent;
- CI reliably distinguishes pass/fail and terminates cleanly;
- no known P0 regression remains.

## P0 carry-over TODO

> Add a client-side behavioural test using a deliberately delayed WCF operation and assert that the WPF Dispatcher continues processing UI work while polling is blocked on network I/O.
