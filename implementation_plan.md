# Assignment Part A Implementation and Verification Plan

## Current Code State — 2026-08-23

The solution builds successfully in Debug configuration. The implementation now covers the required project structure, WCF contracts, polling and duplex clients, server-side state management, private-message lifecycle, and server-enforced file access.

This document records implementation evidence and outstanding verification work. It does not claim demonstration marks or manual-test results that have not been performed.

## Requirement Traceability

| Requirement | Implementation evidence | Code state | Manual verification required |
|---|---|---|---|
| A1 / B1 — sign-in and unique user IDs | `UserManager.TrySignIn`, sign-in views | Implemented | Duplicate ID rejection and reuse after sign-out/disconnect |
| A2 / B2 — channel list and membership | `ChannelManager`, polling coordinator, duplex callbacks | Implemented | Join, leave, list refresh, one-channel rule |
| A3 — channel creation | `ChannelManager.TryCreateChannel`, channel-list views | Implemented | Duplicate-name feedback and cross-client refresh |
| A4 / B3 — public conversation | `MessageRouter`, polling deltas, duplex message callbacks | Implemented | Delivery to current members only; no pre-join replay |
| A5 / B4 — private conversation | `MessageRouter.RoutePrivateMessage`, PM windows, per-session history | Implemented | Multiple windows, incoming-window creation, history restore, member-leave behavior |
| A6 / B5 — file sharing | `FileHandler`, `ChatService.ShareFile/GetFile`, file UI | Implemented | Allowed/blocked files, file retrieval, non-member access rejection |
| A7 — sign-out | client coordinators and `ChatService.SignOut` | Implemented | Sign-out from both views and user-ID reuse |
| C1 — duplex contract | `IDuplexChatService`, `IChatCallback`, `NetTcpBinding` | Implemented | Register callbacks in a running client |
| C2 — pushed duplex updates with no polling | `ChatCallbackHandler`, callback-driven coordinator; periodic duplex ping removed | Implemented | Verify channel/member/message/PM/file updates arrive without refresh or timer fetches |
| C3 — thread safety | server `ReaderWriterLockSlim` usage and callback Dispatcher marshaling | Implemented, concurrency evidence pending | Concurrent clients and callback/UI stability |
| C4 — disconnection handling | callback fault handling and sign-out cleanup | Partially evidenced in code | Close/kill a duplex client and verify ID release, membership removal, and continued service |

## Completed Corrections

- Private-message windows retain history while the current account is signed in and restore it when reopened.
- PM windows close when their recipient leaves the current channel; sign-out clears local PM history.
- `SendPrivateMessage` returns server acceptance, so rejected sends do not create a local-only message or clear the input.
- Public-message polling starts from a user's join boundary, preventing pre-join channel messages from being returned after they enter a channel.
- File uploads require the uploader to be a current member of the target channel.
- File downloads require the requesting user to be a current member of the file's channel; the service contract now carries the requester ID.
- The duplex session coordinator no longer uses a periodic server ping, preserving the callback-only update model required by Section C2.

## Outstanding Work

### UI Enhancement Plan — Channel and Private Conversations

1. Make file-message entries in the channel conversation clickable so selecting a file message downloads and opens the associated server file through the existing client service path.
2. Add a private-conversation file panel with the same visual language as the channel conversation's shared-files panel.
3. Add private-conversation file selection/upload UI only: validate the local file and show the selected/shared-file presentation, while leaving the PM server contract and transfer routing explicitly deferred for a follow-up implementation.
4. Align PM message presentation with channel messages: group consecutive messages by sender within the same minute, show the sender/time metadata at group boundaries, and retain the current-user alignment.
5. Keep PM files in a right-side panel; do not mix file entries into the message stream until PM file server routing is implemented.

Acceptance criteria for this enhancement:

- A channel file message can be clicked and uses the existing download/open flow.
- Polling and duplex PM windows compile with the same message-grouping behavior.
- PM windows display a right-side files panel and an upload affordance without inventing a new server operation.
- The deferred PM file path is documented in code and working notes as a TODO, not presented as fully functional.

## Private File Sharing Implementation Plan

The current PM file panel is intentionally a local preview only. The following work is required to make private file sharing a real polling and duplex feature without weakening channel-file authorization.

### 1. Contract and data model

- Extend `SharedFile` with a private-recipient field, or introduce a focused `PrivateSharedFile` data contract, so a private file has an explicit sender and recipient scope rather than a channel scope.
- Add request/response operations to `IChatService`:
  - `bool SharePrivateFile(string senderId, string recipientId, string fileName, FileType fileType, byte[] fileData)`
  - `SharedFile GetPrivateFile(string userId, Guid fileId)`
  - `List<SharedFile> GetPendingPrivateFiles(string userId)` for polling delivery and recovery when a window was closed.
- Add `OnPrivateFileShared(SharedFile file)` to `IChatCallback` for duplex delivery. Keep `OnFileShared` for channel files so the two scopes cannot be confused.
- Keep the existing 2 MB and extension rules as shared server-side validation. Do not reuse `ChannelName` as a private-recipient identifier.

### 2. Server storage and authorization

- Extend `FileHandler` metadata/storage to retain private sender and recipient ownership. Store file bytes on the server exactly as channel files are stored.
- In `ChatService.SharePrivateFile`, require both users to be signed in and currently in the same channel. Reject empty/self IDs, invalid content, invalid type/size, and a recipient who leaves before the request is accepted.
- In `ChatService.GetPrivateFile`, allow access only to the original sender or recipient and only while both remain eligible under the private-message same-channel rule. Return no file data for unauthorized callers.
- Add a private-file pending queue to `UserSession` or a dedicated manager. Enqueue only for the recipient; do not expose private files through channel file lists or public callbacks.
- On successful storage, create a private file notification containing the file metadata and ID. Do not put private file bytes in callbacks or polling notifications.
- On sign-out/disconnect, retain server files according to the project’s in-memory lifetime policy, but remove pending delivery state and callback registration safely.

### 3. Polling client flow

- Add `SharePrivateFile`, `GetPrivateFile`, and `GetPendingPrivateFiles` wrappers to `ChatServiceClient` and expose them through `PollingSessionCoordinator`.
- Poll private-file notifications alongside private messages at the existing controlled interval. Raise a dedicated `PrivateFileReceived` event containing the other user ID and metadata.
- When a private file arrives, open the existing PM window for that user if needed and add the file to its right-side files panel.
- On upload, validate locally for immediate feedback, send the bytes to the server, and add the file to the panel only after the server returns success. Preserve the selected file in the input/UI when the request fails.
- On file click, call `GetPrivateFile` with the current user ID, save the returned bytes through `FileHelperService`, and open the local copy. Display a concise error for expired, unauthorized, or unavailable files.

### 4. Duplex client flow

- Add the private-file operation wrappers to `DuplexServiceClient` and the coordinator.
- Register `OnPrivateFileShared` in `ChatCallbackHandler`, marshal it through the existing WPF `Dispatcher`, and raise a dedicated coordinator event.
- Do not add a timer, refresh loop, or refresh button. Private-file metadata must arrive through the duplex callback; downloading bytes remains an explicit user action.
- Use the same PM window upload, panel, click-to-download, success/failure, and recipient-leave behavior as the polling client.

### 5. PM lifecycle and safety rules

- A PM file window is valid only for the current sender/recipient conversation. Closing and reopening the window restores metadata retained in the current signed-in session, subject to server availability.
- When either participant leaves the channel, disable or close the PM file UI and reject further uploads/downloads server-side.
- Never broadcast private-file notifications to channel members, other PM windows, or public message history.
- Handle duplicate notifications idempotently by `FileId` in each PM window.
- Do not add a local-only file entry before server acceptance once real transfer is implemented.

### 6. Verification checklist

- Polling sender uploads an allowed image/text file; only the recipient receives it and can download it.
- Duplex sender uploads an allowed image/text file; recipient receives it through `OnPrivateFileShared` without polling.
- Both users can see the appropriate metadata; unrelated channel members cannot.
- Invalid extension, empty file, and files over 2 MB are refused with readable feedback.
- Sender, recipient, and unrelated user access attempts are checked after leaving/rejoining channels.
- Close/reopen PM windows, sign out/sign in again, duplicate callbacks, and client disconnects do not leak private files into another account or conversation.

### P0 — Required manual verification

Run the server plus at least three client instances, including both polling and duplex clients. Record actual results in `docs/WORKING.md`.

1. Duplicate sign-in, channel create/join/leave, and sign-out from both views.
2. Public message delivery only to current members; join after a message and confirm no earlier message appears.
3. PM creation, incoming PM window, reply, multiple PM windows, reopen-history, recipient leave, and rejected send.
4. Share allowed `.txt` and image files, reject invalid extension and files over 2 MB, then download/open a shared file.
5. Attempt an upload or download after leaving the file's channel; the server must reject it.
6. Polling and duplex clients in one channel: verify each direction of public, private, and file updates.
7. Close a duplex client with the window close button and verify the remaining clients observe its departure and can continue using the server.

### P1 — Hardening after manual verification

- Improve abnormal duplex-disconnect cleanup so a failed callback actively removes the affected session, rather than relying solely on later activity or normal sign-out.
- Move synchronous polling WCF calls off the WPF dispatcher thread before final delivery; `DispatcherTimer` schedules polling but its current network calls execute on the UI thread.
- Replace the console integration harness's optimistic/hard-coded result reporting with deterministic automated assertions. This is engineering hardening, not a substitute for WPF manual verification.

## Build Verification

```powershell
.\build.ps1
```

The full solution was built successfully after the changes recorded in this plan. No GUI or live-server test result is asserted by this document.
