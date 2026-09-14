# Assignment Requirement → Implementation Mapping

This document is a marker-facing map of the COMP3008 Part A requirements to the implementation in this repository. The requirement identifiers are taken from `docs/ass/PART_A_REQUIREMENTS_TEMP.txt`, which was extracted from the official Part A material. Lab/lecture references come from the conceptual guides in `docs/labs/README.md` and `docs/lecs/README.md`.

> **Important:** A lab/lecture reference means the project applies the corresponding course concept; it does not mean the complete assignment feature was implemented in that teaching exercise.

## 1. High-level assessment mapping

| Assignment area | Requirement range | Main implementation | Course connection |
|---|---|---|---|
| Client functionality | A-F01–A-F21 | Polling WPF client, shared views/services | Lab 1; Labs 2–3; Lab 6; Lectures 1–4 |
| System architecture | A-ARC01–A-ARC16 | Contracts, WCF server, two WPF clients, shared library | Labs 1–3, 6; Lectures 1–5 |
| Polling | A-POL01–A-POL08 | `Chat.Client.Polling` polling/session coordination | Lecture 4; Labs 2–3, 6 |
| Duplex/callbacks | A-DPX01–A-DPX12 | `IDuplexChatService`, `IChatCallback`, callback manager, duplex coordinator | Lecture 5; Lab 6; Lecture 4 |
| Channels/chat | A-CHAT01–A-CHAT22 | `ChatService`, `ChannelManager`, `MessageRouter`, clients | Labs 2–3, 6; Lectures 2–5 |
| File sharing | A-FILE01–A-FILE11 | `FileHandler`, WCF operations, file message flow | Lectures 1–3 and 5; no retained lab directly reproduces the assignment feature |
| Concurrency/threading | A-CON* | WCF service concurrency, manager synchronization, callback isolation, WPF Dispatcher | Lab 6; Lectures 3–5 |

## 2. Client functionality

| Requirement | What it means in this project | Main implementation locations | Lab / lecture relationship |
|---|---|---|---|
| A-F01 | Passwordless user-ID sign-in with duplicate-ID rejection | `ChatService.SignIn`, `UserManager`, sign-in views | Lab 1 / Lectures 1–2: WPF input, objects, service boundary |
| A-F02 | Current channel list and automatic updates | `ChannelManager`, polling/duplex channel-list updates | Labs 2–3; Lecture 2; polling/callback concepts from Lectures 4–5 |
| A-F03 | At most one channel membership and return to channel list | `ChatService.JoinChannel`, `LeaveChannel`, client navigation | Labs 2–3 / Lecture 2: service-side state and rules |
| A-F04 | Create unique channels with visible failure feedback | `ChatService.CreateChannel`, `ChannelManager` | Labs 2–3 / Lecture 2 |
| A-F05 | Send and receive public channel messages | `MessageRouter`, `IChatService`, duplex callbacks, polling updates | Labs 2–3 / Lectures 2–5 |
| A-F06 | Keep member list current | `ChannelManager`, polling member refresh, duplex member callbacks | Lecture 4 polling; Lecture 5 callbacks |
| A-F07 | No historical replay to late joiners | Channel sequence/poll boundary in server state and routing | Labs 2–3 / Lectures 2–3: server-owned state and service boundary |
| A-F08 | Start one-to-one private conversation | Private-message views and routing | Lab 1 WPF event model; Labs 2–3 service boundary |
| A-F09 | Incoming private messages use separate windows | `PrivateMessageView`, client session/window coordination | Lab 1 WPF; Lecture 3 separation of responsibilities |
| A-F10 | Multiple private conversations simultaneously | Client private-window management | Lab 1 WPF/event-driven UI |
| A-F11 | Share permitted text/image files up to 2 MB | `FileHandler`, `ChatService.ShareFile`, client file handling | WCF/data-contract/service-boundary concepts from Labs 2–3 / Lectures 2–3 |
| A-F12 | Show filename and sharer to channel members | File metadata + file `Message` | Lectures 2–3/5: contract/data transfer and service notifications |
| A-F13 | Retrieve/open a shared file | `GetFile`, client download/open flow | Lecture 2 WCF request/response; Lecture 5 duplex notification + separate retrieval |
| A-F14 | Reject invalid file type/size visibly | `FileHandler` validation and client feedback | Service validation / fault-result concepts from Labs 2–3 |
| A-F15 | Sign out from either view | Main-window/session coordination | Lab 1 WPF navigation; Labs 2–3 service calls |
| A-F16 | Clean sign-out removes channel membership and user ID | `ChatService.SignOut`, `UserManager`, `ChannelManager` | Lecture 2 service-side state; Lecture 4 synchronization |
| A-F17 | Polling client automatically asks for changes | Polling session coordinator/background polling | Lecture 4 polling; Lab 6 async/background execution |
| A-F18–A-F21 | Required views, controls and sign-out availability | WPF views and shared UI | Lab 1; Lecture 1 |

## 3. Architecture

| Requirement | Implementation evidence | Course connection |
|---|---|---|
| A-ARC01 | Windows WPF desktop applications in C#/.NET Framework | Lab 1 / Lecture 1 |
| A-ARC02 | Both clients communicate through `Chat.Server`; no peer-to-peer client links | Labs 2–3 / Lecture 2 |
| A-ARC03 | Self-hosted WCF `Chat.Server` | Labs 2–3 / Lecture 2 |
| A-ARC04 | `UserManager`, `ChannelManager`, `FileHandler`, message state held server-side | Labs 2–3 / Lectures 2–3 |
| A-ARC05 | `Chat.Client.Polling` WPF application | Lab 1 / Lectures 1–2 |
| A-ARC06 | Polling client background update mechanism | Lab 6 / Lecture 4 |
| A-ARC07 | Separate `Chat.Client.Duplex` WPF application | Lecture 5 / Lab 6 |
| A-ARC08 | Both clients use the same server state and contracts | Labs 2–3 / Lecture 2 |
| A-ARC09 | `Chat.Contracts` and `Chat.Client.Shared` prevent unnecessary duplication | Labs 2–3 / Lecture 2 |
| A-ARC10 | Fixed localhost endpoints configured by server/client configuration | Labs 2–3 / Lecture 2 |
| A-ARC11 | Authoritative in-memory state; no database | Labs 2–3 / Lecture 3 architectural separation |
| A-ARC12 | Duplex client uses `NetTcpBinding` and callback contract | Lecture 5 |
| A-ARC13 | Duplex is a separate executable/project | Lecture 5 |
| A-ARC14–A-ARC15 | Shared solution and same server used by both clients | Labs 2–3 / Lecture 2 |
| A-ARC16 | Test/integration infrastructure supports multiple concurrent clients | Lecture 4 synchronization/concurrency concepts |

## 4. Polling versus Duplex

| Assignment requirement | Polling implementation | Duplex implementation | Course concept |
|---|---|---|---|
| A-POL01–A-POL05 | Periodic background requests for current state | Not used for core duplex updates | Lecture 4: polling and completion observation |
| A-POL03 | Messages, members, channels and files are refreshed by polling | Push equivalents are callback-driven | Lectures 4–5 |
| A-DPX02 | N/A | No polling timer for core real-time updates | Lecture 5 |
| A-DPX03–A-DPX08 | N/A | Client registers `IChatCallback`; server invokes callbacks | Lecture 5 / Lab 6 |
| A-DPX09 | N/A | No refresh-button/polling substitute for pushed updates | Lecture 5 |
| A-DPX10–A-DPX12 | Sign-out/disconnect handling also exists in server | Callback disconnect cleanup and safe callback invocation | Lectures 4–5: lifecycle and synchronization |

The important distinction is architectural: **polling observes server state by repeated requests; duplex receives server-initiated notifications over the callback channel.**

## 5. Chat and channels

The main server path is:

```text
WPF client
    ↓
WCF contract
    ↓
ChatService
    ↓
UserManager / ChannelManager / MessageRouter
    ↓
callback or polling response
    ↓
WPF Dispatcher / conversation view
```

`ChannelManager` owns authoritative channel membership. `MessageRouter` restricts public/private message delivery according to membership. `ChatService` applies the service-level workflow around these managers.

This corresponds most directly to Labs 2–3 and Lectures 2–3, with asynchronous delivery/threading concerns extending into Lectures 4–5.

## 6. File sharing

The file path is intentionally separate from system-message rendering:

```text
Choose file
   ↓
ChatService.ShareFile
   ↓
FileHandler validates + stores bytes
   ↓
SharedFile metadata / File Message
   ↓
Channel members
   ↓
GetFile(FileId)
   ↓
bytes returned to authorised member
```

The server enforces file type, size and channel access. A duplex file notification may carry **metadata**, while the actual file bytes are retrieved using the file ID. This is a service-boundary design, not a peer-to-peer file transfer.

There is no retained lab in `docs/labs` that directly reproduces the assignment's complete file-sharing feature, so the mapping should not claim one. The closest course concepts are WCF contracts/data transfer and service boundaries from Labs 2–3 and Lectures 2–3/5.

## 7. Concurrency, synchronization and UI threading

The project combines several course concepts:

```text
Multiple WCF requests / callbacks
            ↓
ChatService (concurrent service instance)
            ↓
Synchronized server state
            ↓
Callback invocation
            ↓
WCF callback thread
            ↓
WPF Dispatcher
            ↓
ObservableCollection / UI
```

Relevant implementation areas include the WCF `ServiceBehavior`, `ChatService` membership-transition synchronization, state-management managers, callback handling, and Dispatcher marshaling in the clients.

The direct teaching relationship is strongest with **Lecture 4 — Delegates, Callbacks, and Thread Synchronization**, **Lecture 5 — Duplex Communication**, and **Lab 6 — Delegates, Callbacks, and Asynchronous Programming**.

## 8. System messages

System events are represented separately from user/file messages:

```text
ConversationItemViewModel
    ├── MessageViewModel
    └── SystemMessageViewModel
```

The intended events are:

- `<user> joined the channel.`
- `<user> left the channel.`

They are rendered with a dedicated WPF template: centered, subtle, timestamped, and without sender/avatar/file controls. Exports use the normal timestamp format but omit a sender prefix.

This is primarily a project-level feature built from the WPF/modeling/service-notification concepts in Labs 1–3 and Lectures 1–5; it is not claimed to be a verbatim lab requirement.

## 9. Course mapping summary

| Feature in this assignment | Primary course material | Why |
|---|---|---|
| C# / WPF application structure | Lab 1, Lecture 1 | Object model, desktop UI, event-driven programming |
| Client/server and contracts | Labs 2–3, Lecture 2 | WCF service boundary, contracts, endpoints and bindings |
| Multi-tier separation | Labs 2–3, Lecture 3 | Separation of presentation, service and state responsibilities |
| Background polling | Lab 6, Lecture 4 | Polling, asynchronous work, callbacks and worker execution |
| Thread synchronization | Lab 6, Lecture 4 | Shared state, locks/waits, safe completion and UI handoff |
| Duplex callbacks | Lecture 5, Lab 6 | Client callback contract and server push |
| HTTP/basic WCF request-response | Labs 2–3, Lectures 2–3 | Service invocation across a process boundary |
| File transfer | Lectures 2–3/5 | Contracts, serialized data and service-mediated transfer |
| System events | Lectures 4–5 | Callback/event delivery and asynchronous UI updates |
| Export | Lab 1 / project-level feature | Local data transformation and file generation; no direct lab requirement claimed |

## 10. Source hierarchy for this mapping

When documenting assessment coverage, use this order of authority:

1. `docs/ass/Part A.pdf` / its repository extraction `PART_A_REQUIREMENTS_TEMP.txt` for assignment requirements.
2. `docs/labs/README.md` for the laboratory-to-concept mapping.
3. `docs/lecs/README.md` for lecture concepts and their assignment relationship.
4. Actual source code and tests for implementation evidence.
5. Other project documentation for explanatory detail, but not as authority over the official requirements.
