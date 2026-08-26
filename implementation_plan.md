# Assignment Part A Implementation and Production Readiness Plan

## Current Code State — 2026-08-26

The core Assignment Part A implementation is complete in the current P2 engineering branch. The server, polling client, duplex client, shared contracts, structured unit/integration suite, lifecycle hardening, file authorization, PM authorization, polling/duplex parity, and duplex disconnect cleanup are implemented.

P2.1 lifecycle hardening has also been implemented and verified locally with the structured build/integration test path. The remaining work is not a rewrite of the application architecture. It is targeted reliability testing, manual WPF verification, release engineering, documentation, packaging/distribution, and production-style CI/CD.

This document records implementation evidence and outstanding verification/release work. It does not claim demonstration marks or manual GUI results until those scenarios have actually been performed.

## Feature Completion Assessment

### Required application functionality

| Area | Status | Notes |
|---|---|---|
| Sign-in / unique user IDs | Implemented | Server-authoritative validation |
| Channel list / creation / join / leave | Implemented | Server-authoritative membership |
| Public messaging | Implemented | Polling and duplex delivery paths |
| Private messaging | Implemented | Same-channel authorization and user-scoped delivery |
| File sharing | Implemented | Server validation, authorization, metadata/content separation, bounded storage |
| Sign-out | Implemented | Session and channel cleanup |
| Abnormal disconnect handling | Implemented | Duplex callback failure cleanup |
| Polling client | Implemented | Pull-based updates; no architectural changes required |
| Duplex client | Implemented | Callback-driven core updates; no polling refresh loop |
| Thread-safe server state | Implemented | Synchronisation is present in shared state managers |
| WPF Dispatcher marshaling | Implemented | Callback/UI boundary is explicitly handled |
| Structured automated tests | Implemented | Consolidated test entry point exists |

### Intentionally out of scope

Private-file transfer is not an Assignment Part A requirement and must not be treated as incomplete core functionality. The existing PM file-selection UI may remain local-only unless a separate product requirement is introduced. Do not add server-side private-file transfer merely to increase feature count.

The application also intentionally uses in-memory state and server-side runtime file storage. Persistence/database work is not required by the assignment and should not be introduced unless the deployment target later requires it.

## P1 Completion

The following hardening slices are implemented and integrated into the structured test runner:

- File-sharing authorization and storage hardening
- Private-message authorization and user-scoped pending delivery
- Polling/duplex behavioural parity for public messages, private messages, file notifications, and membership updates
- Duplex disconnect cleanup
- Structured regression coverage for the above

The WPF Dispatcher responsiveness proof remains a manual/client-side verification item. Server-side tests must not be used as evidence that the WPF UI remains responsive.

## P2 — Engineering Hardening

### P2.1 — Connection lifecycle robustness — IMPLEMENTED / VERIFIED

Implemented callback cleanup now verifies that the callback which failed is still the callback registered for the user before removing the session. This prevents an old callback invocation from signing out a replacement session created after a normal sign-out/reconnect.

Coverage includes the deterministic stale-callback race in which an old callback fails after the user has reconnected with a replacement callback/session.

**Status:** implementation committed and local build/integration verification passed.

### P2.2 — Concurrent state integrity — NEXT

Prove shared server state remains correct under concurrent operations:

- simultaneous sign-in/sign-out;
- join/leave races;
- concurrent message sends;
- callback registration/removal races;
- concurrent channel membership changes;
- concurrent private-message/file delivery;
- duplicate membership prevention;
- collection consistency after racing operations.

Do not add locks blindly. First identify the cross-manager operation sequence being protected, then test and harden the smallest required boundary.

### P2.3 — Delivery reliability

Verify delivery semantics when users or connections change during delivery:

- recipient offline;
- recipient reconnecting;
- sender disconnecting;
- recipient leaving a channel;
- pending private-message delivery;
- callback failure during notification;
- no duplicate delivery after recovery.

### P2.4 — File-transfer robustness

Extend the completed authorization work with malformed-input and storage-failure coverage:

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

The core parity suite exists. Extend it to lifecycle and recovery transitions so polling and duplex expose equivalent server semantics while retaining their different delivery mechanisms.

### P2.6 — Client asynchronous-state robustness

Audit both WPF clients around asynchronous WCF calls and duplex callbacks:

- Dispatcher marshaling;
- background network operations;
- concurrent UI updates;
- shutdown during outstanding operations;
- callback arrival during view/state changes;
- stale client state after reconnect.

The explicit Dispatcher responsiveness proof must be performed with the real client/UI. Do not mark this complete from server-side logs alone.

### P2.7 — Error handling and fault isolation

Verify malformed/invalid operations fail in a controlled way and one bad client cannot destabilise unrelated clients:

- invalid identifiers;
- nonexistent users/channels/files;
- unauthorized operations;
- callback exceptions;
- disconnected clients;
- failed file operations;
- service-side exception boundaries.

### P2.8 — Resource lifecycle

Audit disposal/lifecycle behaviour for:

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
- file storage root and permissions;
- message limits;
- allowed extensions;
- logging and log locations;
- command-line configuration;
- Debug-only behaviour;
- release configuration transforms/overrides;
- firewall/network requirements.

## P2.10 — Final Automated Regression Gate

Keep one structured integration-test entry point. The final automated gate must include:

- unit tests;
- core polling/duplex integration tests;
- authorization hardening;
- polling/duplex parity;
- disconnect/lifecycle coverage;
- concurrency tests;
- delivery/recovery tests;
- file robustness tests;
- error/fault-isolation tests;
- resource lifecycle tests.

The gate should run from a clean checkout and produce useful logs/artifacts on failure.

## P2 Implementation Order

1. **P2.1 — Connection lifecycle robustness** — implemented and locally verified
2. **P2.2 — Concurrent state integrity** — next implementation slice
3. **P2.3 — Delivery reliability**
4. **P2.4 — File-transfer robustness**
5. **P2.5 — Polling/Duplex state-transition parity**
6. **P2.6 — Client asynchronous-state robustness + real WPF verification**
7. **P2.7 — Error handling and fault isolation**
8. **P2.8 — Resource lifecycle**
9. **P2.9 — Configuration/deployment audit**
10. **P2.10 — Final automated regression gate**

## P3 — Release Engineering, CI/CD and Distribution

P3 starts only after the remaining P2 engineering tests are green. Its purpose is to make the application reproducibly buildable, testable, packageable, and deployable from a clean machine.

### P3.1 — CI hardening

The repository already has a GitHub Actions CI workflow that builds on Windows and runs the server-backed integration suite. Extend it rather than creating disconnected workflows.

Required CI stages:

1. checkout;
2. restore/build all projects;
3. unit tests;
4. structured integration tests;
5. Release configuration build;
6. packaging smoke test;
7. upload test/build logs on failure;
8. retain release artifacts for successful tagged builds.

CI must exercise the same scripts developers use locally wherever practical.

### P3.2 — Quality gates

Add deterministic checks appropriate to the existing .NET Framework/WCF codebase:

- compiler warnings reviewed and intentional warnings eliminated where practical;
- static analysis/code-quality checks where compatible;
- dependency/security audit;
- repository hygiene check for generated databases, logs, binaries, and secrets;
- Release build verification.

Do not fail CI on cosmetic warnings without first determining whether they indicate a real defect.

### P3.3 — Release configuration

Create and verify an explicit Release configuration for server and clients.

Define clearly:

- endpoint host/ports;
- file storage location;
- log location;
- maximum message/file limits;
- allowed extensions;
- runtime configuration mechanism;
- service identity/permissions;
- firewall requirements.

Secrets and machine-specific values must not be committed to source control.

### P3.4 — Packaging

Produce reproducible Windows distribution artifacts for:

- `Chat.Server`;
- `Chat.Client.Polling`;
- `Chat.Client.Duplex`;
- required shared/runtime dependencies;
- default configuration templates;
- startup scripts;
- README/quick-start documentation.

Prefer a simple versioned ZIP distribution first. An installer (for example MSI/WiX) is optional unless installation UX is required. Packaging should be tested from a clean Windows machine/environment.

### P3.5 — Containerisation decision

Do not containerise the WPF clients. They are Windows desktop applications and require an interactive Windows environment.

If containerisation is useful, evaluate it specifically for the server. Because the server is .NET Framework 4.8/WCF and exposes HTTP plus Net.TCP endpoints, the container must be Windows-based and the Net.TCP port must be explicitly exposed/configured. Containerisation is therefore a deployment option, not a replacement for normal Windows distribution.

The first release target should remain a native Windows server/client distribution because that matches the technology stack and avoids unnecessary deployment complexity.

### P3.6 — Deployment smoke test

From a clean Windows environment:

1. install/unpack the release;
2. configure the server endpoint/storage/log paths;
3. start the server;
4. connect a polling client;
5. connect a duplex client;
6. exercise sign-in, channel creation/join, public messaging, PMs, file sharing, sign-out;
7. kill a duplex client and verify cleanup;
8. restart the server and verify expected in-memory reset behaviour;
9. collect logs;
10. verify no development files are created outside the configured locations.

### P3.7 — Release versioning

Use a consistent version source and release naming convention. Create Git tags for release candidates and stable releases. A release should point to a commit for which CI has passed and the distribution artifact has been generated.

### P3.8 — Documentation and operator experience

Update README and supporting docs to cover:

- architecture;
- prerequisites;
- build commands;
- test commands;
- local development setup;
- server deployment;
- endpoint configuration;
- firewall/network requirements;
- file storage behaviour;
- logs and troubleshooting;
- running polling and duplex clients;
- release artifact contents;
- known limitations;
- manual verification status.

Keep assignment/educational context separate from operational instructions so the README is useful as both a technical record and a reproducible setup guide.

## P4 — Final Verification and Demonstration

### P4.1 — Manual multi-client verification

Run the actual WPF clients against one server and record observed results. Minimum demonstration topology:

- 1 server;
- at least 2 clients for basic interaction;
- at least 3 concurrent clients for the required demonstration scenarios;
- both polling and duplex clients active against the same server.

### P4.2 — Assignment scenario checklist

Manually demonstrate and record:

- unique sign-in;
- duplicate sign-in rejection;
- channel creation;
- duplicate channel rejection;
- join/leave;
- public messaging;
- no pre-join public-message replay;
- private messaging;
- PM history within a session;
- file upload/download;
- file validation failures;
- sign-out cleanup;
- abnormal disconnect cleanup;
- duplex real-time updates;
- polling updates;
- concurrent clients.

### P4.3 — Final evidence package

Preserve:

- CI run for the release commit;
- automated test summary;
- manual test results;
- screenshots or demonstration evidence where useful;
- release artifact checksum/version;
- known limitations.

## Repository / Documentation Work Remaining

The current repository already contains build scripts, run scripts, CI, a README, project plan, working notes, license, and structured tests. The remaining documentation work is therefore refinement rather than creating an entirely new documentation system.

Priority documentation tasks:

1. refresh `README.md` after the final P2/P3 behaviour is fixed;
2. reconcile `docs/WORKING.md` with the actual current code state;
3. keep `implementation_plan.md` current as phases close;
4. document the final test commands and expected outputs;
5. document Release packaging and deployment;
6. document firewall/port requirements;
7. add troubleshooting for common WCF startup/connection failures;
8. record manual verification separately from automated evidence.

## CI/CD Target Architecture

```text
Developer branch
      |
      v
Pull Request
      |
      v
+-------------------+
| GitHub Actions CI  |
| build + unit tests |
| integration tests  |
| Release build      |
+---------+---------+
          |
          v
      main / tag
          |
          v
+-------------------+
| Release workflow  |
| package artifacts  |
| publish checksums  |
| create GitHub      |
| release assets     |
+---------+---------+
          |
          v
   Windows deployment
   +---------------+
   | Chat Server   |
   +---------------+
       ^       ^
       |       |
   Polling   Duplex
    Client    Client
```

The release workflow should build from the tagged commit rather than packaging an arbitrary working tree.

## Scope Rules

- Do not invent requirements merely to increase test count.
- Every new test must trace to a concrete reliability, security, concurrency, or deployment concern.
- Do not duplicate passing tests unless the new test exercises a distinct failure/race condition.
- Do not weaken assertions to make a test pass.
- Do not reintroduce polling into the duplex client.
- Do not claim WPF/manual behaviour is proven by server-side tests.
- Do not introduce a database or persistence layer unless a real deployment requirement justifies it.
- Do not containerise WPF desktop clients.
- Keep the structured integration-test runner consolidated.
- Keep generated runtime databases, logs, binaries, and temporary files out of version control.
- Record manual verification separately near the end of the project.

## Immediate Next Actions

### Engineering first

1. **P2.2:** add concurrency/race integration coverage.
2. **P2.3:** add delivery/reconnect/recovery coverage.
3. **P2.4:** finish malformed-input and storage-failure file tests.
4. **P2.5:** extend polling/duplex parity into lifecycle/recovery cases.
5. **P2.6:** perform real WPF Dispatcher/client lifecycle verification.
6. **P2.7/P2.8:** close fault-isolation and resource-lifecycle gaps.
7. **P2.9:** audit Release configuration and deployment assumptions.
8. **P2.10:** run the complete automated gate from a clean checkout.

### Then release engineering

9. Harden `.github/workflows/ci.yml` with unit/integration/Release/package gates.
10. Add a tagged release workflow and versioned Windows artifacts.
11. Produce a reproducible ZIP distribution first.
12. Optionally evaluate a Windows-container server image after native distribution works.
13. Update README/WORKING/release documentation.
14. Run clean-machine deployment smoke testing.
15. Perform and record final 3+ client WPF demonstration scenarios.

## Verification Commands

Local engineering gate:

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```

Release verification should additionally build the Release configuration and execute the same structured test suite against the Release binaries.
