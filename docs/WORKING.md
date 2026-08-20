# COMP3008 Chat Application - Working Notes

## Project Status

**Current Phase**: Phase 5 - Duplex Client Implementation (In Progress)

**Last Updated**: August 20, 2026

## Completed Work

### Phase 1: Foundation ✅
- Created solution structure with 5 projects targeting .NET Framework 4.8
- Defined all WCF service contracts in Chat.Contracts
- Defined data contracts (User, Channel, Message, SharedFile)
- Defined shared types (MessageType, FileType)
- Added project references per dependency structure
- Created directory structure for all projects
- Added .gitignore and .gitattributes
- Moved documentation to docs/ directory

### Phase 2: Server Implementation ✅
- Implemented UserManager with login/logout, username uniqueness, user sessions
- Implemented ChannelManager with create/join/leave channels and membership
- Implemented MessageRouter for public and private message routing
- Implemented CallbackManager for duplex callback registration and invocation
- Implemented FileHandler with file validation (2MB limit, allowed extensions)
- Implemented ChatService WCF service implementing IChatService and IDuplexChatService
- Added GetPendingMessages and GetPendingPrivateMessages for polling clients
- Implemented WCF service hosting with BasicHttpBinding (polling) and NetTcpBinding (duplex)
- Added thread-safety with ReaderWriterLockSlim throughout all managers
- Added configurable endpoints via App.config and command-line arguments
- Server maintains all state in memory (no database per assignment requirements)
- Added server integration tests

### Phase 3: Polling Client ✅
- Implemented sign-in functionality with ValidationService integration
- Implemented channel list view with automatic polling
- Implemented channel creation with validation
- Implemented conversation view with timestamp-based message polling
- Implemented member list polling
- Added background polling thread using DispatcherTimer (2-second interval)
- Implemented private messaging with multiple conversation windows
- Implemented file sharing UI with FileHelperService integration
- Implemented file download and opening
- Added sign-out functionality with proper cleanup
- Integrated shared UI resources (colors, sizing, converters)

### Phase 4: Shared Components ✅
- Created ValidationService for common validation rules (username, channel, message, file)
- Created FileHelperService for file operations (size, extension, reading, saving)
- Created ConfigurationService for server settings management
- Created shared UI resources:
  - Colors.xaml: Dark theme color brushes (Background, SecondaryBackground, Border, Foreground, Accent, Error)
  - Sizing.xaml: Font sizes, padding, margins, corner radius
  - Converters.xaml: FileSizeConverter for human-readable file sizes
  - SharedResources.xaml: Master resource dictionary merging all resources
- Refactored polling client to use shared components
- Fixed XAML resource loading issues with Page build action
- Fixed invalid StaticResource usage in margin properties
- Added button styling with proper control templates and state colors
- Added corner radius to button and input controls
- Added settings icon button to sign-in view

## COMP3008 Lecture 1 Alignment

The project demonstrates the fundamental concepts introduced in Lecture 1:

- Distributed computing
- Distributed applications
- Client-server architecture
- Inter-process communication (IPC)
- Remote Procedure Calls (RPC)
- Serialization and deserialization
- Marshaling
- Communication failures
- The distinction between local procedure calls and remote procedure calls

The application is distributed because its components execute in separate processes and communicate over network boundaries.

The project uses WCF as the RPC framework rather than implementing a custom application protocol directly over TCP/UDP.

## COMP3008 Distributed Systems Context

This project is a distributed application implemented using a client-server architecture with two RPC communication mechanisms.

### Distributed Application Characteristics

Distributed computing involves coordinating computation across multiple independent processes, typically running on different machines or networked environments, and communicating through IPC mechanisms.

The application is distributed across multiple processes:

- `Chat.Server` runs as a separate server process.
- `Chat.Client.Polling` runs as a separate client process.
- `Chat.Client.Duplex` runs as a separate client process.
- Communication occurs over network endpoints rather than direct method calls within the same process.

**Important distinction:**
```text
Physical distribution ≠ required
Process/network distribution = sufficient for this project
```

The application can operate with the server and clients on the same physical machine during development, while they remain **separate processes communicating through network endpoints**.

The application performs useful work across multiple processes:
- Clients provide the user interface and initiate operations.
- The server maintains shared application state and performs business logic.
- The server routes messages, manages users/channels, validates files, and maintains sessions.
- The duplex server additionally invokes client callbacks for real-time events.

### Client-Server vs Peer-to-Peer

This project is **not peer-to-peer**.

Clients never communicate directly:

```text
Incorrect:

Client A ◄────────► Client B
```

Instead:

```text
Correct:

Client A ─────► Server ◄───── Client B
```

The server coordinates the application and maintains authoritative state.

The duplex callback:

```text
Server ─────► Client B
```

does not make the system peer-to-peer because the communication still originates from the server.

### Inter-Process Communication (IPC)

Because the client and server execute as separate processes, they cannot directly access each other's memory or invoke internal methods directly.

Communication occurs through WCF service calls.

For example:

```text
Chat.Client.Polling Process
        │
        │ WCF / RPC
        ▼
Chat.Server Process
        │
        ▼
UserManager
```

The client does not directly access:

```csharp
UserManager
ChannelManager
MessageRouter
FileHandler
```

Instead, it communicates through the WCF service contract.

This is an important characteristic of the distributed architecture.

### RPC Architecture

The application uses Windows Communication Foundation (WCF) as its RPC framework.

From the client's perspective, operations such as:

```csharp
service.SignIn(userId);
service.JoinChannel(userId, channelName);
service.SendMessage(message);
```

appear similar to normal local method calls. However, these operations are executed by the remote `Chat.Server` process.

Conceptually:

```text
Client Process
     │
     │ RPC Request
     ▼
Network / WCF
     │
     ▼
Server Process
     │
     │ Execute operation
     ▼
Server-side Managers
     │
     │ RPC Response
     ▼
Client Process
```

WCF provides the RPC abstraction and handles much of the communication infrastructure, including message encoding, serialization/deserialization, transport communication, and service dispatch.

The application still needs to consider:

- endpoint configuration
- connectivity
- timeouts
- communication failures
- service availability
- serialization constraints
- concurrency
- callback disconnection

### Local Procedure Call vs Remote Procedure Call

The architecture has two layers of procedure calls:

```text
CLIENT
──────────────────────────────
service.SignIn(userId)
      │
      │ RPC / WCF (Remote)
      ▼
SERVER
──────────────────────────────
ChatService.SignIn(userId)
      │
      │ Local Procedure Call
      ▼
UserManager.SignIn(userId)
```

- **Remote Procedure Call (RPC)**: `service.SignIn(userId)` from client to server via WCF
- **Local Procedure Call**: `_userManager.SignIn(userId)` within server process

### RPC Transparency and Its Problems

A major advantage of RPC is that a remote operation can appear similar to a local function call:

```csharp
service.SignIn(userId);
```

However, unlike:

```csharp
_userManager.SignIn(userId);
```

the RPC call crosses a process and network boundary.

Therefore:

```text
Local Procedure Call
--------------------
Very low latency
Normally reliable
Shared process/address space

Remote Procedure Call
---------------------
Network latency
Can fail independently
Requires serialization
Requires communication
Remote process may be unavailable
```

This directly demonstrates the Lecture 1 warning that RPC can hide the distributed nature of an application.

### Why Remote Calls Are Fundamentally Different

A local call normally has a relatively simple execution model:

```text
Call → Execute → Return
```

A remote call has multiple possible failure points:

```text
Client
  │
  │ Request
  ▼
Network
  │
  ▼
Server
  │
  │ Execute
  ▼
Network
  │
  ▼
Client
```

Failures can occur:

- Before the request reaches the server
- While the request is being transmitted
- While the server is processing it
- After the server has completed the operation
- While the response is being transmitted
- Before the client receives the response

Therefore, a communication exception does not necessarily prove that the server-side operation did not occur.

This is one of the fundamental differences between local calls and RPC.

### Communication Models

The project demonstrates two RPC communication models:

#### Polling (Request/Response RPC)

```text
Polling Client
      │
      │ Request
      ▼
    Server
      │
      │ Response
      ▼
Polling Client
      │
      │ waits 2 seconds
      ▼
    repeat
```

The polling client periodically invokes server operations such as:

- `GetMessages()`
- `GetPrivateMessages()`
- `GetChannelMembers()`
- `GetChannels()`

### Polling vs Duplex

#### Polling

The client repeatedly asks:

```text
"Do you have anything new?"
```

Example:

```text
Client ── GetMessages() ──► Server
Client ◄──── Messages ───── Server

wait 2 seconds

Client ── GetMessages() ──► Server
```

#### Duplex

The client establishes a callback channel and the server can notify the client when an event occurs:

```text
Client ───── request ─────► Server

Server ───── callback ─────► Client
```

Therefore the duplex client does not need periodic message polling.

This distinction is important for the Section C requirement.

#### Duplex RPC / Callback (Bidirectional RPC)

Duplex WCF communication provides two-way communication over a client-server relationship. The client sends service requests to the server, while the server can invoke operations on the client's callback contract.

Architecture:

```text
                WCF Duplex Channel

Client                                      Server
  │                                            │
  │────── Service Request ───────────────────►│
  │                                            │
  │                                            │
  │◄───── Callback Invocation ────────────────│
  │                                            │
```

The callback is still communication across the process/network boundary. Do not describe the callback as a direct local event.

### RPC Failure Considerations

Because operations are remote, they have properties that local method calls do not:

- Network connectivity can fail.
- The server may be unavailable.
- A remote call has significantly higher latency than a local method call.
- A request may fail after the client has sent it.
- The client cannot assume that the server operation completed simply because the local call was initiated.
- Client applications must handle communication exceptions and disconnects.

The implementation therefore includes disconnect handling and service connection management.

### Serialization and Service Contracts

The WCF contracts in `Chat.Contracts` define the interface and data exchanged between client and server.

**Service Contracts** (RPC Interface / IDL equivalent):
- `IChatService` - Defines polling client operations
- `IDuplexChatService` - Defines duplex client operations
- `IChatCallback` - Defines server-to-client callback operations

**Data Contracts** (Serialization definitions):
- `User` - User information
- `Channel` - Channel information
- `Message` - Message data
- `SharedFile` - File metadata and data

### RPC Serialization Example

When the polling client calls:

```csharp
service.SendMessage(message);
```

the `Message` object cannot simply be passed as a shared in-memory object.

Conceptually:

```text
Client Memory
    │
    │ Message object
    ▼
Serialization
    │
    ▼
Network representation
    │
    ▼
Network
    │
    ▼
Deserialization
    │
    ▼
Server Memory
    │
    ▼
Message object
```

The response follows the reverse process.

The client and server therefore have independent object instances.

They do not share memory.

### Serialization vs Marshaling

#### Serialization

Converts an object/data structure into a representation suitable for transmission or storage.

Example:

```text
Message object
      ↓
Serialized representation
      ↓
Network
```

#### Marshaling

Refers more broadly to preparing data/arguments for communication between execution contexts.

In this project, WCF handles the required argument/message marshaling and serialization infrastructure.

Serialization is commonly part of the marshaling process in distributed communication systems.

### Why Use RPC Instead of Raw TCP/UDP?

TCP provides reliable byte-stream transport, but it does not define application-level operations.

A custom TCP implementation would need to define:

- Message boundaries
- Request/response format
- Serialization
- Operation identification
- Error handling
- Connection management
- Application protocol rules

WCF provides a higher-level RPC abstraction.

Therefore:

```text
TCP
 ↓
Transport-level communication

WCF RPC
 ↓
Application/service-level remote operations
```

This corresponds directly to Lecture 1's point that TCP/IP is too general-purpose for directly implementing application-level distributed operations.

### Client-Server vs Distributed Application

Although the system uses a client-server architecture, it qualifies as a distributed application because the application performs coordinated work across multiple processes.

```text
                    Distributed Chat Application
                              │
              ┌───────────────┴───────────────┐
              │                               │
        Client Processes                 Server Process
              │                               │
       ┌──────┴──────┐                ┌───────┴────────┐
       │             │                │                │
    Polling        Duplex          State            Services
    Client         Client        Management          / WCF
       │             │                │
       └──────┬──────┘                │
              │                       │
              └──── Network ──────────┘
```

The server maintains authoritative shared state while clients provide independent user interfaces and communicate with the server through RPC.

### WCF and RPC

WCF provides the RPC infrastructure used by the application.

The `Chat.Contracts` project acts as the shared service contract definition. Both clients reference these contracts, while `Chat.Server` provides the implementation.

The client uses WCF-generated/provided service proxies to invoke operations on the remote server.

The two endpoints demonstrate different communication patterns:

| Client | Binding | Endpoint | Communication |
|---|---|---|---|
| Polling | BasicHttpBinding | HTTP | Request/response RPC |
| Duplex | NetTcpBinding | TCP | Bidirectional RPC with callbacks |

WCF abstracts the underlying network communication so the client can invoke service operations using normal C# method-call syntax.

## Architecture & Logic Distribution

### Overall Architecture

```text
                    DISTRIBUTED CHAT APPLICATION
                              │
          ┌───────────────────┴───────────────────┐
          │                                       │
     CLIENT PROCESSES                       SERVER PROCESS
          │                                       │
 ┌────────┴────────┐                     ┌────────┴─────────┐
 │                 │                     │                  │
Polling Client   Duplex Client      ChatService       State Managers
 │                 │                     │                  │
 │                 │                     │       ┌──────────┼──────────┐
 │                 │                     │       │          │          │
 │                 │                     │   UserManager ChannelManager
 │                 │                     │       │          │
 │                 │                     │ MessageRouter FileHandler
 │                 │                     │       │
 │                 │                     │ CallbackManager
 │                 │                     │
 └────────┬────────┘                     └────────┬─────────┘
          │                                       │
          │             WCF RPC / IPC             │
          └──────────────────┬────────────────────┘
                             │
                    Network Endpoint
```

### RPC Lifecycle

```text
1. Client invokes proxy method
             ↓
2. WCF prepares request
             ↓
3. Arguments are serialized/marshaled
             ↓
4. Request travels across network/IPC
             ↓
5. Server receives request
             ↓
6. WCF dispatches operation
             ↓
7. ChatService executes operation
             ↓
8. Server managers perform local processing
             ↓
9. Result is serialized
             ↓
10. Response travels back
             ↓
11. Client receives/deserializes result
             ↓
12. Client processes result
```

For duplex callbacks:

```text
Server event
    ↓
Callback contract
    ↓
WCF callback invocation
    ↓
Network
    ↓
ChatCallbackHandler
    ↓
Dispatcher
    ↓
WPF UI
```

### Server Logic Distribution

**UserManager** (`Chat.Server/StateManagement/UserManager.cs`)
- **Responsibilities**: User authentication, session management, username uniqueness
- **Key Methods**:
  - `SignIn(string userId)`: Validates uniqueness, creates UserSession
  - `SignOut(string userId)`: Removes session, cleans up callbacks
  - `GetUser(string userId)`: Retrieves user session
  - `IsUserSignedIn(string userId)`: Checks active status
- **Thread Safety**: ReaderWriterLockSlim for user dictionary access
- **State**: Dictionary<string, UserSession> (in-memory)

**ChannelManager** (`Chat.Server/StateManagement/ChannelManager.cs`)
- **Responsibilities**: Channel CRUD operations, membership management
- **Key Methods**:
  - `CreateChannel(string channelName)`: Creates new channel
  - `GetChannel(string channelName)`: Retrieves channel info
  - `GetAllChannels()`: Returns all channels
  - `JoinChannel(string userId, string channelName)`: Adds user to channel
  - `LeaveChannel(string userId)`: Removes user from channel
  - `GetChannelMembers(string channelName)`: Returns member list
- **Thread Safety**: ReaderWriterLockSlim for channel dictionary access
- **State**: Dictionary<string, Channel> (in-memory)

**MessageRouter** (`Chat.Server/StateManagement/MessageRouter.cs`)
- **Responsibilities**: Public/private message routing, pending queue management
- **Key Methods**:
  - `RoutePublicMessage(Message message)`: Distributes to all channel members
  - `RoutePrivateMessage(Message message)`: Sends to specific user (same-channel validation)
  - `GetPendingMessages(string userId)`: Retrieves queued messages for polling
  - `GetPendingPrivateMessages(string userId)`: Retrieves queued private messages
- **Logic**:
  - Public messages: Queued for all channel members including sender
  - Private messages: Queued only if both users in same channel
  - Pending queues emptied after successful retrieval
- **Thread Safety**: ReaderWriterLockSlim for message operations
- **State**: UserSession contains Queue<Message> for pending messages

**CallbackManager** (`Chat.Server/StateManagement/CallbackManager.cs`)
- **Responsibilities**: Duplex callback registration and invocation
- **Key Methods**:
  - `RegisterCallback(string userId, IChatCallback callback)`: Stores callback
  - `UnregisterCallback(string userId)`: Removes callback
  - `NotifyMessageReceived(string userId, Message message)`: Invokes callback
  - `NotifyChannelMemberJoined(string channelName, string userId)`: Broadcasts join event
  - `NotifyChannelMemberLeft(string channelName, string userId)`: Broadcasts leave event
- **Logic**:
  - Callbacks invoked with try-catch to prevent failures from crashing server
  - Only registered callbacks receive notifications
- **Thread Safety**: ReaderWriterLockSlim for callback dictionary access
- **State**: UserSession contains IChatCallback reference

**FileHandler** (`Chat.Server/StateManagement/FileHandler.cs`)
- **Responsibilities**: File validation, storage, retrieval
- **Key Methods**:
  - `ValidateFile(SharedFile file)`: Checks size (2MB) and extension whitelist
  - `StoreFile(SharedFile file)`: Saves to disk with unique key
  - `GetFile(string fileKey)`: Retrieves file data
- **Validation Rules**:
  - Max size: 2,097,152 bytes (2MB)
  - Allowed extensions: .png, .jpg, .jpeg, .gif, .bmp, .txt
- **Storage**: Files stored in `Chat.Server/StoredFiles/` directory
- **File Key Format**: `{channelName}_{fileName}`

**ChatService** (`Chat.Server/Services/ChatService.cs`)
- **Responsibilities**: WCF service implementation, coordinates all managers
- **Implements**: IChatService, IDuplexChatService
- **Key Methods**:
  - `SignIn(string userId)`: Delegates to UserManager
  - `SignOut(string userId)`: Delegates to UserManager, ChannelManager, CallbackManager
  - `CreateChannel(string channelName)`: Delegates to ChannelManager
  - `GetChannels()`: Delegates to ChannelManager
  - `JoinChannel(string userId, string channelName)`: Delegates to ChannelManager, MessageRouter
  - `LeaveChannel(string userId)`: Delegates to ChannelManager
  - `SendMessage(Message message)`: Delegates to MessageRouter
  - `SendPrivateMessage(Message message)`: Delegates to MessageRouter
  - `GetMessages(string userId, DateTime since)`: Delegates to MessageRouter
  - `GetPrivateMessages(string userId, DateTime since)`: Delegates to MessageRouter
  - `ShareFile(SharedFile file)`: Delegates to FileHandler, MessageRouter
  - `GetFiles(string channelName)`: Returns channel files
  - `RegisterCallback()`: Delegates to CallbackManager
- **Thread Safety**: Coordinates locks across managers for atomic operations

### Shared Components Logic Distribution

**ValidationService** (`Chat.Client.Shared/Services/ValidationService.cs`)
- **Responsibilities**: Client-side validation rules (communication-agnostic)
- **Key Methods**:
  - `ValidateUsername(string username)`: Checks not empty, allowed chars, max length
  - `ValidateChannelName(string channelName)`: Checks not empty, max length, invalid chars
  - `ValidateMessage(string message)`: Checks not empty, max length
  - `ValidateFile(string fileName, long fileSize)`: Checks extension and size
- **Logic**:
  - Returns ValidationResult with IsValid and ErrorMessage
  - Rules match server validation rules
  - Used by both polling and duplex clients
- **No Dependencies**: Pure validation logic, no WCF, no WPF

**FileHelperService** (`Chat.Client.Shared/Services/FileHelperService.cs`)
- **Responsibilities**: File operations (communication-agnostic)
- **Key Methods**:
  - `GetFileSize(string filePath)`: Returns file size in bytes
  - `GetFileExtension(string fileName)`: Returns extension with dot
  - `ValidateSupportedFile(string fileName, long fileSize)`: Checks against allowed types
  - `ReadFile(string filePath)`: Reads file into byte[]
  - `SaveFile(byte[] data, string filePath)`: Saves byte[] to disk
  - `OpenFile(string filePath)`: Opens file with default application
- **Constants**:
  - `MaxFileSizeBytes = 2,097,152` (2MB)
  - `SupportedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt"]`
- **Logic**:
  - File I/O operations only
  - No WCF calls
  - Used by both polling and duplex clients

**ConfigurationService** (`Chat.Client.Shared/Services/ConfigurationService.cs`)
- **Responsibilities**: Server configuration management
- **Key Methods**:
  - `GetServerHost()`: Returns server host from config
  - `GetPollingPort()`: Returns polling port from config
  - `GetDuplexPort()`: Returns duplex port from config
  - `GetPollingInterval()`: Returns polling interval from config
- **Logic**:
  - Reads from App.config appSettings
  - Provides defaults if not configured
  - Used by both polling and duplex clients

**Shared UI Resources** (`Chat.Client.Shared/`)
- **Colors.xaml**: Dark theme color brushes
  - BackgroundBrush (#1e1e1e)
  - SecondaryBackgroundBrush (#2d2d2d)
  - BorderBrush (#404040)
  - ForegroundBrush (#ffffff)
  - AccentBrush (#00bcd4)
  - AccentHoverBrush (#00acc1)
  - AccentPressedBrush (#0097a7)
  - AccentDisabledBrush (#555555)
  - ErrorBrush (#ff5252)
- **Sizing.xaml**: Font sizes, padding, margins, corner radius
  - Font sizes: Small (10), Normal (12), Medium (14), Large (16), ExtraLarge (24), Huge (28)
  - Padding: Small (5), Normal (8), Medium (10), Large (15), ExtraLarge (20)
  - Margins: Small (5), Normal (10), Medium (15), Large (20), ExtraLarge (30)
  - Directional margins: Bottom*, Top*, Left*, Right* variants
  - Corner radius: Normal (4), Small (2)
- **Converters.xaml**: FileSizeConverter for human-readable file sizes
- **SharedResources.xaml**: Master dictionary merging all resources

### Polling Client Logic Distribution

**MainWindow** (`Chat.Client.Polling/MainWindow.xaml.cs`)
- **Responsibilities**: Sign-in view, application entry point, view navigation
- **Key Methods**:
  - `SignInButton_Click()`: Validates username via ValidationService, calls WCF SignIn
  - `UsernameTextBox_KeyDown()`: Handles Enter key for sign-in
  - `ShowChannelListView()`: Navigates to channel list
  - `SignOut()`: Cleans up state, returns to sign-in
  - `SettingsButton_Click()`: Placeholder for settings view
- **State**: _serviceClient, _currentUserId, _channelListView, _conversationView, _privateMessageViews
- **Polling**: DispatcherTimer calls PollingTimer_Tick every 2 seconds

**ChannelListView** (`Chat.Client.Polling/Views/ChannelListView.xaml.cs`)
- **Responsibilities**: Channel list display, channel creation, navigation
- **Key Methods**:
  - `RefreshChannels()`: Calls WCF GetChannels, updates ListBox
  - `CreateChannelButton_Click()`: Validates via ValidationService, calls WCF CreateChannel
  - `JoinButton_Click()`: Calls WCF JoinChannel, navigates to ConversationView
  - `MembersListBox_MouseDoubleClick()`: Opens PrivateMessageView for selected member
  - `SignOutButton_Click()`: Calls MainWindow.SignOut
- **Polling**: Updates channel list on timer tick

**ConversationView** (`Chat.Client.Polling/Views/ConversationView.xaml.cs`)
- **Responsibilities**: Channel conversation display, message sending, file sharing
- **Key Methods**:
  - `Initialize(string channelName)`: Sets up view for specific channel
  - `RefreshMessages()`: Calls WCF GetMessages with last timestamp, updates ListBox
  - `RefreshMembers()`: Calls WCF GetChannelMembers, updates ListBox
  - `RefreshFiles()`: Calls WCF GetFiles, updates ListBox
  - `SendButton_Click()`: Creates Message, calls WCF SendMessage
  - `ShareFileButton_Click()`: Opens file dialog, validates via FileHelperService, calls WCF ShareFile
  - `FilesListBox_MouseDoubleClick()`: Downloads and opens file via FileHelperService
  - `MembersListBox_MouseDoubleClick()`: Opens PrivateMessageView
  - `LeaveButton_Click()`: Calls WCF LeaveChannel, returns to ChannelListView
- **Polling**: Updates messages, members, files on timer tick
- **State**: _lastMessageTimestamp for efficient delta polling

**PrivateMessageView** (`Chat.Client.Polling/Views/PrivateMessageView.xaml.cs`)
- **Responsibilities**: Private conversation display, private messaging
- **Key Methods**:
  - `Initialize(string recipientId)`: Sets up view for specific user
  - `RefreshMessages()`: Calls WCF GetPrivateMessages with last timestamp, updates ListBox
  - `SendButton_Click()`: Creates private Message, calls WCF SendPrivateMessage
- **Polling**: Updates private messages on timer tick
- **State**: _lastMessageTimestamp for efficient delta polling

### Polling Client Threading

The polling client uses WPF `DispatcherTimer`, whose tick handler executes on the UI dispatcher thread. Therefore UI controls can be updated directly from the polling callback, although long-running network operations should still be handled carefully to avoid blocking the UI.

### Duplex Callback Threading

WCF callback methods are not guaranteed to execute on the WPF UI thread.

Therefore:

```text
Callback thread
      ↓
ChatCallbackHandler
      ↓
Dispatcher.Invoke / Dispatcher.BeginInvoke
      ↓
WPF UI thread
      ↓
Update controls
```

This is an important implementation detail for Section C.

### Polling Logic Flow

```text
DispatcherTimer (2s)
    ↓
PollingTimer_Tick
    ↓
If signed in and in channel:
    ↓
    RefreshMessages() → GetMessages(since) → Update UI
    RefreshMembers() → GetChannelMembers() → Update UI
    RefreshFiles() → GetFiles() → Update UI
If signed in and in private chat:
    ↓
    RefreshMessages() → GetPrivateMessages(since) → Update UI
```

### Server as Coordinator

The server acts as the authoritative coordinator for shared application state.

It maintains:

- Active users
- Channel membership
- Pending messages
- Private-message routing
- File metadata/storage
- Registered duplex callbacks

Clients do not coordinate directly with one another.

For example:

```text
Client A ───────► Server ───────► Client B
                    │
                    │
                    ▼
              Shared State
```

This means the system remains a client-server distributed application rather than a peer-to-peer system.

### Distribution Transparency

WCF allows the client to interact with the remote service using familiar C# method-call syntax.

For example:

```csharp
service.SendMessage(message);
```

This hides much of the underlying networking implementation.

However, the application is not truly equivalent to a local application because:

- Latency exists
- Network communication can fail
- The server can become unavailable
- Data must be serialized
- Callbacks can disconnect
- Concurrent clients can modify shared state

Therefore the project demonstrates both the convenience and the limitations of RPC abstraction.

### WPF and C# in the Project

The clients are implemented using:

- C#
- .NET Framework 4.8
- WPF

WPF provides the local graphical user interface, while WCF provides communication with the distributed server.

This creates a clear separation:

```text
WPF
 ↓
User Interface

C#
 ↓
Application Logic

WCF
 ↓
RPC / Network Communication

Chat.Server
 ↓
Distributed Server Logic
```

WPF itself is not the distributed component. It is the local UI framework used by the client processes.

### Duplex Client Logic Distribution (To Be Implemented)

**Planned Structure**:
- **MainWindow**: Sign-in view (similar to polling)
- **ChannelListView**: Channel list with callback-based updates
- **ConversationView**: Conversation with real-time message callbacks
- **PrivateMessageView**: Private chat with real-time message callbacks
- **DuplexServiceClient**: WCF duplex client wrapper
- **ChatCallbackHandler**: Implements IChatCallback, receives server callbacks
- **Dispatcher marshaling**: Thread-safe UI updates from callback thread

**Planned Callback Flow**:
```
Server Event
    ↓
IChatCallback method (on WCF thread)
    ↓
ChatCallbackHandler
    ↓
Dispatcher.Invoke (marshal to UI thread)
    ↓
UI Update (on UI thread)
```

## Current Architecture

### Server Components
- **UserManager**: Manages user sessions, authentication, pending message queues
- **ChannelManager**: Manages channel creation, membership, member lists
- **MessageRouter**: Routes public and private messages with same-channel validation
- **CallbackManager**: Manages duplex callback registration and safe invocation
- **FileHandler**: Validates files (2MB, specific extensions), stores and retrieves
- **ChatService**: WCF service implementing both IChatService and IDuplexChatService
- **ChatServiceHost**: WCF service hosting with configurable endpoints

### Server Endpoints
- **Polling**: `http://localhost:8080/ChatService/Polling` (BasicHttpBinding)
- **Duplex**: `net.tcp://localhost:8081/ChatService/Duplex` (NetTcpBinding)

### Configuration
Server endpoints can be configured via:
1. **App.config**: Set PollingHost, PollingPort, DuplexHost, DuplexPort
2. **Command-line**: --polling-host, --polling-port, --duplex-host, --duplex-port
3. Command-line arguments override App.config values

## Key Decisions

### Assignment Constraints
- **No server-side database**: All state held in memory per assignment requirements
- **No server message history**: Users only see messages from time they join
- **File restrictions**: Only .png, .jpg, .jpeg, .gif, .bmp, .txt, max 2MB
- **Private messaging**: Only allowed between users in same channel
- **Files travel through server**: Client A → Server → Client B (no direct paths)

### Architecture Approach
- **Code-behind over MVVM**: Using WPF code-behind for simplicity and assignment requirements
- **Shared components**: Only communication-independent logic in Chat.Client.Shared
- **No over-engineering**: Avoid unnecessary abstractions, MVVM frameworks, dependency injection
- **Preserve polling client**: Polling client is working reference, do not destabilize

### Thread Safety
- **Server**: ReaderWriterLockSlim for all shared state
  - Write locks for modifications (sign-in/out, join/leave, message routing)
  - Read locks for queries (get channels, get members, get callbacks)
- **Polling client**: DispatcherTimer runs on UI thread, no cross-thread issues
- **Duplex client**: WCF callbacks on background thread, Dispatcher.Invoke for UI updates
- **Callbacks**: Invoked with try-catch to prevent failures from crashing server

### Performance
- Efficient timestamp-based delta polling
- Configurable polling interval
- In-memory server state for fast access
- Callback-based updates for the duplex client
- File size limited to 2 MB by assignment requirements

### Message Routing
- Public messages distributed to all channel members at send time
- Sender also receives their own message for consistency
- Private messages only between users in same channel
- Pending queues emptied after successful retrieval (polling)
- Timestamp-based delta polling for efficiency

## Lecture 1 Takeaways Demonstrated by This Project

| Lecture 1 Concept         | Project Demonstration                         |
| ------------------------- | --------------------------------------------- |
| Distributed computing     | Separate client/server processes              |
| IPC                       | WCF communication                             |
| Client-server             | Clients communicate through central server    |
| RPC                       | WCF service operations                        |
| Local procedure call      | Manager calls inside server                   |
| Serialization             | WCF data contract transmission                |
| Marshaling                | RPC argument/result preparation               |
| Communication failure     | WCF communication exceptions                  |
| Polling                   | Client-initiated periodic requests            |
| Duplex communication      | Server callbacks                              |
| Coordination              | Server routes messages between clients        |
| Non-shared memory         | Client/server have independent address spaces |
| Peer-to-peer distinction  | Clients never directly communicate            |
| Distribution transparency | Remote calls resemble local C# calls          |

## Phase 5 Definition of Done

Phase 5 is complete when:

- [ ] DuplexChannelFactory successfully connects to server
- [ ] Callback handler successfully registers
- [ ] Server can invoke callback methods
- [ ] Callback methods safely marshal to WPF Dispatcher
- [ ] No periodic message polling exists in duplex client
- [ ] Channel updates are received through callbacks
- [ ] Public messages are received through callbacks
- [ ] Private messages are received through callbacks
- [ ] File notifications/updates are received through callbacks where applicable
- [ ] Sign-out unregisters/cleans callback state
- [ ] Communication failures are handled
- [ ] Multiple duplex clients can communicate simultaneously

## Pending Work

### Phase 5: Duplex Client 🔄 IN PROGRESS
- [ ] Implement DuplexChannelFactory for WCF duplex connection
- [ ] Implement ChatCallbackHandler for IChatCallback
- [ ] Implement Dispatcher marshaling for thread-safe UI updates
- [ ] Build duplex client UI (MainWindow, ChannelListView, ConversationView, PrivateMessageView)
- [ ] Integrate shared services (ValidationService, FileHelperService, ConfigurationService)
- [ ] Integrate shared UI resources (colors, sizing, converters)
- [ ] Test duplex client sign-in/sign-out
- [ ] Test duplex client channel management
- [ ] Test duplex client public messaging with callbacks
- [ ] Test duplex client private messaging with callbacks
- [ ] Test duplex client file sharing

### Phase 6: Testing & Polish ⏳ PENDING
- [ ] Test with 3+ concurrent clients
- [ ] Test both clients against same server
- [ ] Performance optimization
- [ ] Code review and cleanup

### Phase 7: Documentation & Submission ⏳ PENDING
- [ ] Add XML documentation comments
- [ ] Create user guide
- [ ] Prepare demonstration script
- [ ] Complete declaration of originality
- [ ] Create submission zip
- [ ] Verify submission by re-downloading

## Known Issues

None currently.

## Technical Notes

### WCF Service Hosting
- ServiceBehavior set to InstanceContextMode.Single for singleton service
- BasicHttpBinding for polling (request/response)
- NetTcpBinding for duplex (callbacks)
- Endpoints configurable via App.config or command-line
- Large message size limits configured (maxReceivedMessageSize = 2147483647)

### File Storage
- Files stored in Chat.Server/StoredFiles directory
- File key format: `{channelName}_{fileName}`
- Files do not survive server restart (per assignment)
- File data included in SharedFile for transfer

### Message Timestamps
- All messages use DateTime.UtcNow
- Server generates timestamp, not client
- Efficient delta polling using last timestamp
- SortedSet for proper message ordering

### User Sessions
- UserSession contains: UserId, CurrentChannel, PendingChannelMessages, PendingPrivateMessages, Callback
- Pending queues are Queue<Message>
- Callback is IChatCallback for duplex clients

### XAML Resource Management
- Shared resources defined in Chat.Client.Shared
- SharedResources.xaml merges Colors.xaml, Sizing.xaml, Converters.xaml
- App.xaml in polling client merges SharedResources.xaml
- Page build action for XAML files (not Resource) to enable proper compilation
- CornerRadius resources require Page build action for proper resolution

## Git Commits (Recent)

- **8e6ad91**: Update PROJECT_PLAN.md with current implementation status
- **f476c4c**: Update sprint plan and README with current progress
- **47df8f1**: Add settings icon button to sign-in view
- **3f54d24**: Fix CornerRadius resource resolution by using Page build action
- **41f0a8d**: Add corner radius to button and input controls
- **be255c1**: Fix invalid StaticResource usage in XAML margins
- **8e64464**: Fix button styling with proper control template and state colors

## Next Steps

1. Begin Phase 5: Duplex Client implementation
2. Implement DuplexChannelFactory for WCF duplex connection
3. Implement ChatCallbackHandler for IChatCallback
4. Implement Dispatcher marshaling for thread-safe UI updates
5. Build duplex client UI reusing polling client structure
6. Integrate shared services and UI resources
7. Test duplex client against running server

## Resources

- [README.md](../README.md) - Project overview and getting started
- [PROJECT_PLAN.md](PROJECT_PLAN.md) - Detailed project plan
- [SHARED_COMPONENTS_SPRINT.md](../SHARED_COMPONENTS_SPRINT.md) - Sprint plan for shared components and duplex client
- [Part A.md](Part%20A.md) - Assignment requirements

