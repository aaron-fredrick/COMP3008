# COMP3008 Assignment Walkthrough Guide

This guide is a practical, code-oriented walkthrough of the distributed chat application. It is intended for demonstration, revision, debugging, and tracing an assignment requirement into the implementation.

It complements the requirement mapping in [`assignment-mapping.md`](assignment-mapping.md) and the conceptual course guides in [`labs/README.md`](labs/README.md) and [`lecs/README.md`](lecs/README.md).

---

## 1. Start the system

The solution contains one server and two independent WPF clients:

```text
Chat.Server
   ├── Polling client → BasicHttpBinding / HTTP
   └── Duplex client  → NetTcpBinding / TCP callbacks
```

The server owns the authoritative state. Both clients therefore see the same users, channels, messages and files when they connect to the same server instance.

Typical development endpoints are:

```text
Polling: http://localhost:9000/ChatService/Polling
Duplex:  net.tcp://localhost:8081/ChatService/Duplex
```

Use the repository's current configuration/scripts as the source of truth if these values change.

---

## 2. Sign in

A user enters a user ID in the WPF sign-in view.

```text
WPF SignInView
      ↓
client service proxy
      ↓
IChatService.SignIn / duplex equivalent
      ↓
ChatService.SignIn
      ↓
UserManager.TrySignIn
```

The server owns uniqueness. A second active session using the same ID is rejected rather than being silently overwritten.

**Assignment:** A-F01, A-CHAT01–A-CHAT03.

**Course:** Lab 1 / Lecture 1 for UI and object fundamentals; Labs 2–3 / Lecture 2 for the service boundary.

---

## 3. Channel list and channel creation

After sign-in, the client displays the channels known to the server.

For the polling client, current state is obtained through the polling mechanism. For the duplex client, channel-list changes are pushed through the registered callback.

Creating a channel follows:

```text
Client
  ↓
CreateChannel(name)
  ↓
ChatService.CreateChannel
  ↓
ChannelManager.TryCreateChannel
  ↓
server state changes
  ↓
polling response OR duplex callback
```

Channel names are server-authoritative and duplicates are rejected.

**Assignment:** A-F02–A-F04, A-CHAT04–A-CHAT09.

**Course:** Labs 2–3 / Lecture 2.

---

## 4. Joining a channel

The server enforces the single-channel membership rule.

Conceptually:

```text
JoinChannel(user, target)
        ↓
inspect current membership
        ↓
leave previous channel if necessary
        ↓
ChannelManager.JoinChannel
        ↓
set user's current channel
        ↓
notify affected clients
```

The server also establishes the appropriate message/file visibility boundary for the new member. This supports the requirement that late joiners do not receive earlier channel messages.

**Assignment:** A-F03, A-F07, A-CHAT06, A-CHAT12–A-CHAT14.

---

## 5. Public messages

A normal channel message follows this path:

```text
MessageTextBox
      ↓
client command
      ↓
ChatService.SendMessage
      ↓
MessageRouter.RoutePublicMessage
      ↓
current channel members
      ↓
Polling: next poll response
Duplex: callback push
```

The server is the distributor. Clients never send a message directly to another client.

The server only delivers a channel message to users who are members at the time it is sent.

**Assignment:** A-F05, A-F07, A-CHAT10–A-CHAT14.

**Course:** Labs 2–3 / Lectures 2–3; delivery and asynchronous concerns connect to Lectures 4–5.

---

## 6. Polling client: how updates arrive

The Polling client periodically asks the server for changes from a background execution path.

The important conceptual model is:

```text
background polling
       ↓
request current changes
       ↓
server returns state/events
       ↓
client processes response
       ↓
Dispatcher
       ↓
WPF view models / UI
```

Polling is intentionally different from duplex push. It is a pull strategy and therefore has a deliberate polling interval rather than a continuous tight loop.

The polling mechanism covers the state the assignment requires, including messages, membership, channel list and shared files.

**Assignment:** A-F17, A-POL01–A-POL06.

**Course:** Lecture 4 / Lab 6.

---

## 7. Duplex client: how updates arrive

The Duplex client registers a callback when it signs in.

Conceptually:

```text
Duplex client
      ↓
register IChatCallback
      ↓
server stores callback for user
      ↓
state/event occurs
      ↓
CallbackManager
      ↓
IChatCallback method
      ↓
client callback handler
      ↓
Dispatcher
      ↓
WPF view model / UI
```

The core duplex update path does **not** need a polling timer or refresh button.

Callback failures are isolated so that a dead client does not take down the server or prevent notifications to other clients.

**Assignment:** A-DPX01–A-DPX12.

**Course:** Lecture 5, reinforced by Lecture 4 and Lab 6.

---

## 8. User joined / left system messages

System events are deliberately different from normal messages.

The conversation model is:

```text
ConversationItemViewModel
    ├── MessageViewModel
    └── SystemMessageViewModel
```

A system event such as:

```text
Alice joined the channel.
10:42 AM
```

is rendered by its own WPF template. It is centered, visually subtle and has no sender/avatar/file controls.

A normal file message remains a `MessageViewModel` with `MessageType.File` and a `FileId`.

This distinction prevents a membership event such as:

```text
Bob left the channel.
```

from being interpreted by the normal message/file template.

For polling, membership transitions are inferred from successive authoritative member snapshots, with the initial snapshot establishing the baseline rather than generating false join events. For duplex, use the current callback/member-change implementation as the authoritative event path and ensure a disconnect callback does not generate a second copy of the same event.

**Assignment relationship:** member-list correctness is A-F06/A-CHAT; the system-message presentation is a project enhancement rather than a separately scored Part A item.

**Course:** Lectures 4–5 / Lab 6 for asynchronous event delivery and UI-thread coordination.

---

## 9. Private messages

Private messaging remains server-routed:

```text
Sender
  ↓
SendPrivateMessage
  ↓
ChatService
  ↓
MessageRouter
  ↓
recipient callback / polling response
  ↓
separate PrivateMessageView
```

The server verifies that sender and recipient are valid members of the same channel before allowing the private message.

Each private conversation is represented separately on the client, allowing multiple private windows to exist simultaneously.

**Assignment:** A-F08–A-F10, A-CHAT15–A-CHAT20.

---

## 10. File sharing

A channel file is not transferred peer-to-peer.

The server validates and stores the file:

```text
Client selects file
       ↓
ShareFile
       ↓
ChatService
       ↓
FileHandler
       ├── extension validation
       ├── size validation
       └── server-side storage
       ↓
file metadata + file message
```

The file message contains the file identity needed for retrieval. A duplex file notification can carry metadata without carrying the full byte array. The actual content is retrieved with the appropriate `GetFile` operation and authorisation check.

This is important when debugging: **`FileData == null` in a metadata notification is not by itself a broken file.** The subsequent file retrieval is the content transfer step.

**Assignment:** A-F11–A-F14, A-FILE01–A-FILE11.

---

## 11. Sign out and disconnect cleanup

A normal sign-out should remove the user's channel membership and release the user ID.

The server-side workflow is approximately:

```text
SignOut(user)
    ↓
identify current channel
    ↓
remove user from channel
    ↓
clear current-channel state
    ↓
unregister callback if applicable
    ↓
release user ID
    ↓
notify affected clients
```

The duplex server also detects a callback/channel that has gone away. Cleanup must occur even when the client is killed or loses its connection instead of calling `SignOut` normally.

**Assignment:** A-F15–A-F16, A-DPX10–A-DPX12, A-CHAT21–A-CHAT22.

**Course:** Lecture 4 synchronization/callback concepts and Lecture 5 duplex lifecycle.

---

## 12. Concurrency and synchronization

The WCF service is configured as a single service instance with concurrent requests. That means multiple clients can enter server code concurrently and shared state must be protected.

The important distinction is:

```text
WCF concurrency
    ≠
thread safety automatically provided by WCF
```

The application therefore uses synchronization around shared state and membership transitions rather than assuming sequential execution.

Callback notification is also isolated from the core state-management path: a failed callback should not corrupt server state or stop other clients from being notified.

**Assignment:** A-CON* requirements and A-DPX10–A-DPX12.

**Course:** Lecture 4 / Lab 6, with WCF service behaviour from Lecture 2 and duplex callbacks from Lecture 5.

---

## 13. WPF Dispatcher and callback threads

WCF callbacks do not necessarily execute on the WPF UI thread.

Therefore the client-side path is:

```text
WCF callback thread
       ↓
client callback handler
       ↓
Dispatcher.Invoke / BeginInvoke as appropriate
       ↓
ObservableCollection / ViewModel
       ↓
WPF controls
```

Directly modifying a WPF-bound collection from a callback worker thread risks cross-thread exceptions and invalid UI state.

The same principle applies to background polling work: network/background execution should not directly manipulate WPF controls.

**Course:** Lecture 3–5 / Lab 6.

---

## 14. Chat export

Export operates on conversation data rather than scraping the rendered WPF controls.

Conceptually:

```text
Conversation items
       ↓
ChatExportService
       ↓
normal MessageViewModel → timestamp + sender + content
SystemMessageViewModel  → timestamp + event text
       ↓
chat text + available files
       ↓
ZIP export
```

System messages therefore appear as message-like lines but intentionally have no sender, for example:

```text
[10:41 AM] Bob joined the channel.
```

Normal messages retain the sender prefix according to the application's established export format.

---

## 15. Scrolling behaviour

Conversation messages are maintained incrementally rather than replacing the entire `ItemsSource` after every event.

The desired rule is:

```text
At bottom?
   ├── yes → follow newly appended content
   └── no  → preserve user's reading position
```

This applies to normal messages, file messages and system events.

Multiline message composition can change the available viewport. The existing bottom-anchor logic should be preserved rather than reintroducing unconditional `ScrollIntoView` calls.

---

## 16. What to demonstrate to a marker

A compact demonstration can show the assignment concepts in this order:

1. Start the server.
2. Start a polling client and a duplex client.
3. Sign in with different user IDs.
4. Show that both clients see the same channel list.
5. Join the same channel.
6. Show the member list updating.
7. Send a public message from each client.
8. Demonstrate a join/leave system event.
9. Open a private conversation.
10. Share a permitted text/image file and open it from another client.
11. Leave/sign out and show the remaining client receiving the leave event.
12. If demonstrating duplex disconnect handling, terminate a duplex client without normal sign-out and show server cleanup.
13. Export the conversation and show normal messages plus system events in the transcript.
14. If required, run the automated test suite/CI-equivalent command.

The strongest demonstration uses multiple concurrent clients because it visibly proves that the server is authoritative and that polling and duplex clients share the same distributed state.

---

## 17. Where to look when debugging

| Symptom | First places to inspect |
|---|---|
| Duplicate user ID accepted | `UserManager`, `ChatService.SignIn` |
| Channel membership wrong | `ChannelManager`, `ChatService.JoinChannel/LeaveChannel` |
| Public message reaches wrong users | `MessageRouter`, channel membership state |
| Polling stops updating | polling/session coordinator, server polling operation, Dispatcher handoff |
| Duplex update missing | callback contract, `CallbackManager`, callback handler, `DuplexSessionCoordinator` |
| Duplex callback crashes server | callback safe-invocation/error isolation |
| UI cross-thread exception | callback/polling handler and WPF `Dispatcher` usage |
| File appears but cannot open | file `FileId`, `GetFile`, authorisation, local open flow |
| Leave event looks like a file/message | conversation item type, system-message template selector, not `FileTypeVisibilityConverter` |
| Scroll jumps to newest message | `ObservableCollection`, bottom detection, `ScrollToEnd`/`ScrollIntoView` logic |
| Export missing event | `ChatExportService` and conversation-item enumeration |

---

## 18. Requirement-to-code quick reference

| Concern | Key implementation areas |
|---|---|
| Service contracts | `src/Chat.Contracts/` |
| Server host | `src/Chat.Server/` |
| Users | `UserManager`, `ChatService` |
| Channels | `ChannelManager`, `ChatService` |
| Public/private routing | `MessageRouter` |
| Duplex callbacks | `IChatCallback`, `IDuplexChatService`, `CallbackManager`, `DuplexSessionCoordinator` |
| Polling | `Chat.Client.Polling` session/update coordination |
| Shared conversation model | `Chat.Client.Shared/ViewModels/` |
| System-message rendering | conversation views + `ConversationItemTemplateSelector` |
| File storage | `FileHandler` |
| File download | `GetFile` / client file-open flow |
| Export | `ChatExportService` |
| UI threading | WPF `Dispatcher` usage in client update paths |
| Tests | `tests/` |
| CI | `.github/workflows/` |

For exact class/method names, use the current source tree rather than treating this walkthrough as an API specification.
