# Assignment 1A — Project Plan

## 1. Assignment Objective

Implement a real-time chat application demonstrating distributed systems concepts from COMP3008 Lectures 1–4. The application must satisfy Sections A (Functional Requirements), B (Server-Side Implementation), and C (Duplex Implementation) of the assignment specification.

## 2. Marking Requirements Traceability

The implementation must directly satisfy the three assessed sections of Assignment 1A.

| Section | Requirement | Planned Implementation | Marks |
|---|---|---|---:|
| A1 | Sign in | WPF sign-in view + server-side unique user ID validation | 2 |
| A2 | Channel list | Server channel state + polling updates | 2 |
| A3 | Channel creation | Server-side unique channel validation | 2 |
| A4 | Conversation | Server message distribution + polling updates | 2 |
| A5 | Private conversation | Private-message routing + dedicated WPF windows | 2 |
| A6 | File sharing | Server-side file validation/storage + polling updates | 3 |
| A7 | Sign out | Explicit sign-out + disconnect cleanup | 1 |
| B1 | User management | Thread-safe in-memory user registry | 2 |
| B2 | Channel management | Thread-safe authoritative channel state | 2 |
| B3 | Message distribution | Server-side channel membership routing, no history | 2 |
| B4 | Private messaging | Server validates same-channel membership | 2 |
| B5 | File handling | Server validates, stores and serves file contents | 2 |
| C1 | Duplex contract | WCF duplex service/callback contract over `netTcpBinding` | 2 |
| C2 | No polling | Callback-driven updates with no timer/refresh mechanism | 2 |
| C3 | Thread safety | Server synchronisation + WPF Dispatcher marshaling | 2 |
| C4 | Disconnection handling | WCF communication failure detection + cleanup | 2 |

## 3. Functional Requirements

### 3.1 User Management
- Users sign in with a unique user ID
- Server validates user ID uniqueness
- Users sign out explicitly
- Server handles disconnection without explicit sign-out
- User IDs are released on sign-out or disconnection

### 3.2 Channel Management
- Users can view the list of available channels
- Users can create new channels with unique names
- Users can join a channel
- Users can leave a channel
- A user belongs to at most one channel at a time
- Server maintains authoritative channel membership

### 3.3 Public Messaging
- Users can send messages to their current channel
- Messages are distributed to all current channel members
- Message history is not retained
- Users joining a channel do not receive previous messages

### 3.4 Private Messaging
- Users can send private messages to other users
- Private messages are only allowed between members of the same channel
- Private messages are delivered only to the intended recipient
- Private message exchange appears in dedicated windows

### 3.5 File Sharing
- Users can share files in their current channel
- Server validates file extensions (.png, .jpg, .jpeg, .gif, .bmp, .txt)
- Server validates file size (maximum 2 MB)
- File contents are stored on the server
- Files are accessible to channel members
- Local filesystem paths are never shared between clients

### 3.6 Sign Out
- Users can explicitly sign out
- Sign-out releases the user ID
- Sign-out removes the user from their channel
- Sign-out cleans up server-side state

### 3.7 Disconnection Handling
- Server detects client disconnection
- Disconnection releases the user ID
- Disconnection removes the user from their channel
- Disconnection removes duplex callback registration
- Remaining channel members are notified
- Server continues serving other clients

## 4. Architecture

### 4.1 Distributed Architecture

The system uses a client-server distributed architecture.

```text
+--------------------+
|   Polling Client   |
|       WPF          |
+---------+----------+
          |
          | WCF / BasicHttpBinding
          |
+---------v----------+
|                    |
|    Chat Server     |
|                    |
| User Management    |
| Channel Management |
| Messaging          |
| Private Messaging  |
| File Management    |
| Callback Manager   |
|                    |
+--------------------+
          ^
          |
          | WCF / NetTcpBinding
          |
+---------+----------+
|    Duplex Client   |
|       WPF          |
+--------------------+
```

The server is the authoritative owner of shared state. Clients never communicate directly with one another. All communication passes through the Chat Server.

### 4.2 Multi-Tier Architecture

The application follows a multi-tier architecture derived from the distributed computing concepts covered in Lectures 1–4.

**Logical Tiers:**

1. **Display Tier** - WPF windows and controls, handles user interaction and visual presentation, must execute UI updates on the WPF UI thread
2. **Presentation Tier** - Client-side service interaction, converts WCF service responses/callbacks into data suitable for the display tier
3. **Business Tier** - Hosted within the Chat Server, contains user/channel/messaging/file-sharing rules, maintains authoritative application state
4. **Data Tier** - Application state is held in server memory, file contents are stored by the server while running

**Physical Project Structure:** The logical tiers do not necessarily correspond to separate executable projects. The solution contains `Chat.Server`, `Chat.Client.Polling`, `Chat.Client.Duplex`, `Chat.Contracts`, and `Chat.Client.Shared`.

### 4.3 Component Boundaries

The architectural components are:

**Client Components:**
- `Chat.Client.Polling` - Presentation, user interaction, client-side validation, WCF communication
- `Chat.Client.Duplex` - Presentation, user interaction, client-side validation, WCF duplex communication

**Server Service Component:**
- `Chat.Server` → `ChatService` - Exposes service operations, coordinates server-side application logic, maintains authoritative state

**Internal Server Objects:**
- `UserManager`, `ChannelManager`, `MessageRouter`, `CallbackManager`, `FileHandler` - Internal implementation objects rather than independently exposed distributed components

The distributed component boundary is defined by WCF service contracts: `IChatService`, `IDuplexChatService`, `IChatCallback`.

### 4.4 Polling Architecture

The Polling Client uses asynchronous polling to obtain updates from the server. The polling loop periodically requests changes including channel list changes, channel membership changes, new channel messages, private messages, and shared files. The polling operation must execute away from the WPF UI thread so that a network call cannot freeze the interface.

**Rationale:** Polling is intentionally used for Section A because the assignment specifically requires a pull-based strategy. The polling interval should provide a reasonable balance: too short → unnecessary server/network load; too long → poor perceived responsiveness.

### 4.5 Duplex Architecture

The Duplex Client uses WCF duplex communication. The client provides a callback implementation to the server when establishing the connection. The server maintains the callback associated with each signed-in user. When an event occurs, the server invokes the relevant callback rather than waiting for the client to poll.

**Duplex Client Restrictions:** The Duplex Client must contain no polling timer, no polling thread, no periodic refresh operation, and no refresh button used to implement core real-time updates. All real-time updates must arrive through the WCF callback channel.

## 5. Solution Structure

### 5.1 Chat Server

```text
Chat.Server/
├── Program.cs
├── Services/
│   └── ChatService.cs
├── StateManagement/
│   ├── UserManager.cs
│   ├── ChannelManager.cs
│   ├── MessageRouter.cs
│   ├── FileHandler.cs
│   └── CallbackManager.cs
└── Models/
```

### 5.2 Polling Client

```text
Chat.Client.Polling/
├── App.xaml
├── Views/
│   ├── SignInWindow.xaml
│   ├── ChannelListWindow.xaml
│   ├── ChannelWindow.xaml
│   └── PrivateChatWindow.xaml
├── Services/
│   └── PollingClientService.cs
└── ViewModels/
```

### 5.3 Duplex Client

```text
Chat.Client.Duplex/
├── App.xaml
├── Views/
│   ├── SignInWindow.xaml
│   ├── ChannelListWindow.xaml
│   ├── ChannelWindow.xaml
│   └── PrivateChatWindow.xaml
├── Services/
│   ├── DuplexClientService.cs
│   └── ChatCallbackHandler.cs
└── ViewModels/
```

### 5.4 Shared Contracts

```text
Chat.Contracts/
├── IChatService.cs
├── IDuplexChatService.cs
├── IChatCallback.cs
├── ChatMessage.cs
├── PrivateMessage.cs
├── SharedFile.cs
└── DTOs/

Chat.Client.Shared/
├── Services/
│   ├── ValidationService.cs
│   ├── FileHelperService.cs
│   └── ConfigurationService.cs
└── Resources/
    ├── Colors.xaml
    ├── Sizing.xaml
    └── Converters.xaml
```

## 6. Communication Design

### 6.1 WCF Service Contract

| Component Access | Contract             | Binding            | Address                                       | Purpose                   |
| ---------------- | -------------------- | ------------------ | --------------------------------------------- | ------------------------- |
| Polling client   | `IChatService`       | `BasicHttpBinding` | `http://localhost:8080/ChatService/Polling`   | Request/response RPC      |
| Duplex client    | `IDuplexChatService` | `NetTcpBinding`    | `net.tcp://localhost:8081/ChatService/Duplex` | Duplex RPC                |
| Server callback  | `IChatCallback`      | Duplex channel     | Client callback channel                       | Server-to-client callback |

### 6.2 Polling

The polling client repeatedly asks the server for updates using operations like `GetMessages()`, `GetPrivateMessages()`, `GetChannelMembers()`, and `GetChannels()`. The polling interval is configurable (default 2 seconds).

### 6.3 Async/Await

Potentially long-running network operations must not block the WPF UI thread. The client uses C# asynchronous programming (`Task`, `async`, `await`) for polling, file retrieval, and other potentially slow server calls.

### 6.4 One-Way Operations

Server-to-client notifications that do not require a response may use WCF one-way operations: `[OperationContract(IsOneWay = true)]`. One-way calls are appropriate for notifications because the server does not need a return value. However, one-way communication is not equivalent to completely asynchronous execution.

### 6.5 Duplex Channel

The duplex service contract follows the WCF pattern:

```csharp
[ServiceContract(CallbackContract = typeof(IChatCallback))]
public interface IDuplexChatService
{
    ...
}

[ServiceContract]
public interface IChatCallback
{
    ...
}
```

The callback contract provides operations for server-to-client notifications: `ChannelListUpdated`, `MemberListUpdated`, `MessageReceived`, `PrivateMessageReceived`, `FileShared`, `UserDisconnected`.

### 6.6 Remote Callbacks

The Duplex Client uses **remote callbacks** (server-to-client communication during a distributed operation), not async completion callbacks. Remote callbacks are distinguished from async completion callbacks which inform the caller that an asynchronous operation has completed.

## 7. Server State Model

### 7.1 Users

```text
User
- UserId
- CurrentChannel
- Connection / Callback information
```

### 7.2 Channels

```text
Channel
- Name
- Members
- SharedFiles
```

### 7.3 Messages

Messages are transient. The server does NOT maintain message history. A message is distributed only to users who are members of the channel at the time it is sent.

### 7.4 Private Messages

Private messages are delivered only to the intended recipient. The server validates that both users are members of the same channel before delivery.

### 7.5 Files

```text
SharedFile
- FileId
- FileName
- FileType
- FileSize
- SharedBy
- Channel
- FileContents
```

File contents must exist on the server. A local path from one client must never be sent to another client.

### 7.6 Callback Registry

The server maintains a registry of duplex callbacks associated with each signed-in user. Callbacks are removed on sign-out or disconnection.

## 8. Concurrency and Thread Safety

### 8.1 Shared State

Potentially shared state includes signed-in users, channel membership, channel list, private-message routing information, shared files, and registered duplex callbacks.

### 8.2 Synchronisation

All mutations of shared state must be synchronised. The implementation will select an appropriate synchronisation mechanism based on the access pattern (e.g., `ReaderWriterLockSlim` for read-heavy shared state). Synchronisation should be applied at the business-logic level rather than indiscriminately locking every low-level operation.

### 8.3 Race Conditions

The implementation must prevent duplicate user IDs caused by races, users appearing in multiple channels, lost channel membership updates, inconsistent channel state, concurrent collection modification, callback registration/removal races, and file metadata corruption.

### 8.4 Callback Threads

WCF callbacks do not necessarily execute on the WPF UI thread. Therefore, duplex callbacks must not directly modify WPF controls.

### 8.5 WPF Dispatcher

The callback handler must marshal UI updates onto the WPF Dispatcher using `Dispatcher.Invoke` or `Dispatcher.BeginInvoke`.

## 9. Client UI Design

### 9.1 Sign-In View

- User ID input field
- Sign-in button
- Validation feedback
- Connection status

### 9.2 Channel List View

- List of available channels
- Create channel input and button
- Join channel button
- Member list display
- Automatic updates (polling or callback)

### 9.3 Channel View

- Message history display
- Message input field
- Send button
- Member list
- File sharing controls
- Leave channel button

### 9.4 Private Chat Window

- Private message history
- Message input field
- Send button
- Window management for multiple conversations

### 9.5 File Sharing UI

- File selection dialog
- File validation feedback
- Shared files list
- Download button
- File content display

## 10. Assignment Constraints

The following implementations must not be used as substitutes for the required architecture:
- Do not allow clients to communicate directly with each other
- Do not store authoritative shared state in the clients
- Do not use a database; it is unnecessary for this assignment
- Do not persist state between server restarts
- Do not store message history
- Do not send local filesystem paths between clients
- Do not allow unsupported file types
- Do not allow files larger than 2 MB
- Do not implement the Duplex Client as a setting inside the Polling Client
- Do not use polling in the Duplex Client
- Do not add a refresh button as the core update mechanism in the Duplex Client
- Do not update WPF controls directly from WCF callback threads
- Do not assume that server methods execute sequentially
- Do not rely exclusively on client-side validation
- Do not assume a client always performs an explicit sign-out
- Do not allow a failed callback to terminate or block the server

## 11. Testing Requirements

### 11.1 Functional Tests

| Test | Expected Result | Rubric |
|---|---|---|
| Sign in with unique ID | User enters system | A1/B1 |
| Sign in with duplicate ID | Rejected with readable reason | A1/B1 |
| Create unique channel | Channel appears | A3/B2 |
| Create duplicate channel | Rejected with readable reason | A3/B2 |
| Join channel | User appears in member list | A2/A4/B2 |
| Leave channel | User removed from channel | A4/B2 |
| Send public message | All current members receive it | A4/B3 |
| Join after previous message | Previous message not shown | A4/B3 |
| Send private message | Only recipient receives it | A5/B4 |
| Private chat history | Exchange remains in dedicated window | A5 |
| Upload valid image | File appears to members | A6/B5 |
| Upload valid text file | File appears to members | A6/B5 |
| Upload unsupported file | Server rejects with reason | A6/B5 |
| Upload >2 MB | Server rejects with reason | A6/B5 |
| Open shared file | Contents retrieved from server | A6/B5 |
| Sign out | ID released and channel membership removed | A7/B1/B2 |

### 11.2 Concurrent Client Tests

- Duplicate sign-ins
- Simultaneous channel joins
- Simultaneous messages
- Private messages
- Simultaneous file sharing
- Server-side state verification

### 11.3 Polling Tests

- Polling client receives update without user action
- Polling interval configuration
- UI thread handling

### 11.4 Duplex Tests

- Duplex client receives update via callback
- Duplex client has no polling timer
- Callback updates WPF UI without cross-thread exception

### 11.5 Disconnection Tests

- Kill client with X
- Server detects/removes user
- Dead callback does not crash server
- Server continues serving others

### 11.6 File Tests

- File size validation
- File extension validation
- File transfer
- File download

## 12. Assessment Demonstration Checklist

- [ ] Polling client sees duplex-client messages
- [ ] Duplex client sees polling-client messages
- [ ] Both clients share the same channel state
- [ ] Files are accessible from both clients
- [ ] Killing a client releases its ID
- [ ] Remaining clients continue operating
- [ ] Server handles concurrent operations correctly
- [ ] Callbacks update UI without cross-thread exceptions

## 13. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| WCF duplex complexity | High | Start with polling, add duplex later |
| Thread safety issues | High | Extensive testing with concurrent clients |
| UI thread blocking | Medium | Use async/await for network operations |
| Disconnection handling | Medium | Implement robust callback failure detection |
| File validation bypass | Medium | Server-side validation is authoritative |

## 14. COMP3008 Lecture Alignment

The project explicitly demonstrates the following COMP3008 concepts:

- Decomposition into distributed components
- Service-oriented architecture
- RPC as the communication mechanism between components
- Service contracts as component interfaces
- WCF endpoints as component access points
- Separation between distributed components and internal objects
- Serialization across process boundaries
- Runtime communication through WCF channels
- Handling of network failure
- Server-side ownership of authoritative shared state

The project is not merely demonstrating "how to call a WCF method." It demonstrates:

```text
What is distributed?
        ↓
Client UI + server service
        ↓
Where is it placed?
        ↓
Separate processes
        ↓
Why?
        ↓
Independent clients require shared authoritative server state
        ↓
How do they communicate?
        ↓
WCF RPC
```

## COMP3008 Lecture 1 Alignment

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Components | Separate client/server processes | Separates distributed concerns across process boundaries |
| Service-oriented architecture | `ChatService` | Provides distributed operations through WCF |
| Component interfaces | WCF service contracts | Defines interface between distributed components |
| RPC | WCF operations | Enables remote procedure calls across network |
| Endpoint | Address + binding + contract | Specifies how clients access distributed service |
| BasicHttpBinding | Polling communication | Simple request/response communication model |
| NetTcpBinding | Duplex communication | Supports bidirectional communication |
| ChannelFactory | Client-side WCF channel creation | Establishes communication at runtime |
| Objects vs components | Internal managers vs exposed service | Distinguishes implementation from distributed boundary |
| Serialization | WCF data contracts | Enables data transfer across process boundaries |
| Distributed state | Server-owned state | Central authoritative state for coordination |
| Network failure | Communication/disconnection handling | Handles distributed system failure modes |
| Runtime communication | WCF channels | Establishes communication at runtime |
| Service encapsulation | Server implementation hidden from clients | Protects internal implementation details |

## COMP3008 Lecture 3 Alignment

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Multi-tier architecture | Display/Client + Business/Service tiers | Separates presentation from business logic |
| Display tier | WPF clients | Handles user interface and input |
| Business tier | ChatService + managers | Contains application logic and state |
| Data tier | In-memory state | Assignment requirement for in-memory state |
| Async/await | Task-based WCF operations | Prevents UI thread blocking during network calls |
| Task vs Thread | Task for async operations | Avoids thread blocking for network I/O |
| UI responsiveness | Dispatcher marshaling | Keeps GUI responsive during async operations |
| One-way operations | `[OperationContract(IsOneWay = true)]` | For fire-and-forget operations where response not needed |
| Duplex communication | WCF duplex callbacks | Enables server-to-client notifications |
| Callback vs async | Separate concepts distinguished | Callbacks are remote notifications, async is execution model |
| Concurrency | Multiple simultaneous clients | Server handles concurrent requests |
| Thread safety | Synchronisation mechanism | Protects shared mutable state |

## COMP3008 Lecture 4 Alignment

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Operation classification | Explicit sync/async/one-way/callback decisions | Prevents ad-hoc decisions |
| Async decision rule | Use async only when operation is genuinely long-running | Avoids unnecessary complexity |
| Thread-safety requirement | Components with shared state are thread-safe | Distributed components receive concurrent calls |
| Race condition analysis | Concurrent client testing | Identifies and prevents race conditions |
| Synchronization strategy | Business-logic level locking | Controlled concurrency, not maximum locking |
| One-way vs async distinction | Contract vs execution model | Different concepts used independently |
| Remote callback architecture | Duplex channel for server notifications | Server-initiated notifications |
| GUI thread safety | Dispatcher marshaling for UI updates | Prevents cross-thread GUI access |
| UI responsiveness NFR | Async operations for long tasks | Explicit non-functional requirement |
| Async error handling | Exception handling around awaited operations | Handles network failures and timeouts |
| Architecture decision process | Systematic component analysis | Prevents arbitrary design decisions |
| Concurrency testing | Simultaneous client tests | Verifies thread-safety under load |
| Architecture trade-offs | Benefits vs costs of distribution | Justifies architectural decisions |

## 16. Success Criteria

The implementation targets all assessed assignment requirements and is structured to support the full available mark allocation. The system must demonstrate:

- Clear separation between client presentation and server business logic
- Distributed service boundary exposed through WCF contracts
- No direct client access to server implementation objects
- Server maintains authoritative shared state
- Architecture avoids unnecessary additional tiers
- Appropriate use of synchronous and asynchronous operations
- Thread-safe shared state management
- Proper handling of network failures and disconnections
- Effective use of WCF duplex callbacks for real-time updates
- WPF UI updates correctly marshaled to the UI thread

## 14. COMP3008 Lecture Alignment

The project explicitly demonstrates the following COMP3008 concepts:

- Decomposition into distributed components
- Service-oriented architecture
- RPC as the communication mechanism between components
- Service contracts as component interfaces
- WCF endpoints as component access points
- Separation between distributed components and internal objects
- Serialization across process boundaries
- Runtime communication through WCF channels
- Handling of network failure
- Server-side ownership of authoritative shared state

The project is not merely demonstrating "how to call a WCF method." It demonstrates:

```text
What is distributed?
        ↓
Client UI + server service
        ↓
Where is it placed?
        ↓
Separate processes
        ↓
Why?
        ↓
Independent clients require shared authoritative server state
        ↓
How do they communicate?
        ↓
WCF RPC
```

### Lecture 1: Distributed Components and RPC

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Components | Separate client/server processes | Separates distributed concerns across process boundaries |
| Service-oriented architecture | `ChatService` | Provides distributed operations through WCF |
| Component interfaces | WCF service contracts | Defines interface between distributed components |
| RPC | WCF operations | Enables remote procedure calls across network |
| Endpoint | Address + binding + contract | Specifies how clients access distributed service |
| BasicHttpBinding | Polling communication | Simple request/response communication model |
| NetTcpBinding | Duplex communication | Supports bidirectional communication |
| ChannelFactory | Client-side WCF channel creation | Establishes communication at runtime |
| Objects vs components | Internal managers vs exposed service | Distinguishes implementation from distributed boundary |
| Serialization | WCF data contracts | Enables data transfer across process boundaries |
| Distributed state | Server-owned state | Central authoritative state for coordination |
| Network failure | Communication/disconnection handling | Handles distributed system failure modes |
| Runtime communication | WCF channels | Establishes communication at runtime |
| Service encapsulation | Server implementation hidden from clients | Protects internal implementation details |

### Lecture 3: Multi-Tier Architecture and Asynchronous Communication

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Multi-tier architecture | Display/Client + Business/Service tiers | Separates presentation from business logic |
| Display tier | WPF clients | Handles user interface and input |
| Business tier | ChatService + managers | Contains application logic and state |
| Server-side application state | In-memory state | Assignment requirement for in-memory state |
| Async/await | Task-based WCF operations | Prevents UI thread blocking during network calls |
| Task vs Thread | Task for async operations | Avoids thread blocking for network I/O |
| UI responsiveness | Dispatcher marshaling | Keeps GUI responsive during async operations |
| One-way operations | `[OperationContract(IsOneWay = true)]` | For fire-and-forget operations where response not needed |
| Duplex communication | WCF duplex callbacks | Enables server-to-client notifications |
| Callback vs async | Separate concepts distinguished | Callbacks are remote notifications, async is execution model |
| Concurrency | Multiple simultaneous clients | Server handles concurrent requests |
| Thread safety | Synchronisation mechanism | Protects shared mutable state |

### Lecture 4: Operation Classification and Thread Safety

| Concept | Demonstrated By | Why Applied |
|---|---|---|
| Operation classification | Explicit sync/async/one-way/callback decisions | Prevents ad-hoc decisions |
| Async decision rule | Use async only when operation is genuinely long-running | Avoids unnecessary complexity |
| Thread-safety requirement | Components with shared state are thread-safe | Distributed components receive concurrent calls |
| Race condition analysis | Concurrent client testing | Identifies and prevents race conditions |
| Synchronization strategy | Business-logic level locking | Controlled concurrency, not maximum locking |
| One-way vs async distinction | Contract vs execution model | Different concepts used independently |
| Remote callback architecture | Duplex channel for server notifications | Server-initiated notifications |
| GUI thread safety | Dispatcher marshaling for UI updates | Prevents cross-thread GUI access |
| UI responsiveness NFR | Async operations for long tasks | Explicit non-functional requirement |
| Async error handling | Exception handling around awaited operations | Handles network failures and timeouts |
| Architecture decision process | Systematic component analysis | Prevents arbitrary design decisions |
| Concurrency testing | Simultaneous client tests | Verifies thread-safety under load |
| Architecture trade-offs | Benefits vs costs of distribution | Justifies architectural decisions | |
