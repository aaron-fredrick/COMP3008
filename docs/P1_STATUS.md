# P1 Implementation Status

Updated 2026-08-25 on `feature/p1-implementation`.

## Completed in this slice

### File-sharing hardening

- Added server-side integration coverage for authorised channel-file download.
- Added negative coverage proving a non-member cannot download a channel file.
- Added negative coverage for unknown file identifiers.
- Verified channel-file listings return metadata without transferring file bytes.
- Verified the existing join-time file visibility boundary remains enforced.
- Existing unit tests already cover empty/oversized/unsupported/path-traversal files, storage-write failure cleanup, lock separation, deterministic sharded paths, and metadata/content separation.

### Private-message authorization

- Added integration coverage proving PMs work when sender and recipient share a channel.
- Added negative coverage proving cross-channel PMs are rejected.
- Added negative coverage proving PM delivery stops when the recipient leaves the channel.
- Added coverage that rejected PMs are not placed in the recipient's pending queue.

### Private-file authorization and delivery metadata

- Added integration coverage for same-channel private-file sharing.
- Added authorization coverage for sender and recipient retrieval.
- Added negative coverage proving unrelated users cannot retrieve a private file.
- Added lifecycle coverage proving private-file retrieval fails after the recipient leaves the shared channel.
- Added pending-private-file coverage proving only the intended recipient receives the metadata notification.
- Pending file notifications remain metadata-only; file bytes are retrieved explicitly through the download operation.

### Polling/duplex parity

- Added a duplex integration test for channel-file notification.
- The callback test verifies the duplex notification contains file metadata but not file bytes.
- The existing polling path remains responsible for explicit retrieval; duplex remains callback-driven.

## Test runner changes

The structured test executable now runs:

1. Unit/UserManager
2. Unit/ChannelManager
3. Unit/FileStorage
4. Existing deterministic integration suite
5. P1 hardening integration suite

The new P1 suite is deliberately separate from the original deterministic suite so failures identify whether a regression is in the P0 baseline or the newer P1 hardening coverage.

## Remaining P1 work

1. **WPF Dispatcher responsiveness proof** — introduce a delayed/fake WCF operation and prove the UI Dispatcher continues processing work while the call is blocked.
2. **Client file workflow** — verify both polling and duplex WPF clients expose upload, metadata presentation, explicit download/open, and appropriate failure handling without blocking the Dispatcher.
3. **Polling lifecycle audit** — verify channel/file/PM refresh loops exist only while their corresponding views are open and are stopped on close/switch/leave/sign-out/shutdown. Ping/session communication remains independent.
4. **Duplex failure isolation** — test callback failures and abnormal process termination so one dead client cannot block or corrupt other sessions.
5. **Server identity audit** — review every public WCF operation that accepts a user ID and document the authentication/identity assumption used by the assignment protocol. Do not claim transport-level identity/authentication that WCF BasicHttp/NetTcp is not actually providing.
6. **CI regression gate** — build and run the complete structured suite after the client and disconnect work, then perform manual two-client verification.

## Assignment/lecture relationship

The hardening tests directly support the assignment's file-sharing and private-message requirements while preserving the established lifecycle rule: joining a channel does not replay previous channel messages or files. File content is persisted server-side, while metadata remains authoritative application state.

The implementation also demonstrates the distributed-systems concepts used in the project: WCF RPC boundaries, authoritative server state, polling versus duplex callback delivery, explicit resource lifecycle management, and separating persisted content from in-memory metadata.

## Verification commands

```powershell
.\build.ps1
.\scripts\ci\run-integration-tests.ps1
```

Do not mark P1 complete until the remaining client responsiveness, lifecycle, disconnect, and manual multi-client checks have been verified.
