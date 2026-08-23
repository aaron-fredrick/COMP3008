# COMP3008 Chat Application - Working Document

## Current Status

**Phase:** Phase 6 — Integration Testing & Final Polish

**Overall Status:** Core implementation is complete and Debug-build verified. Manual GUI, cross-client, concurrency, and abnormal-disconnect verification remain pending.

**Last Updated:** 2026-08-23

### Completed
- [x] Service contracts defined (IChatService, IDuplexChatService, IChatCallback)
- [x] DTOs created (Channel, Message, PrivateMessage, SharedFile)
- [x] Server implementation (ChatService, UserManager, ChannelManager, MessageRouter, FileHandler, CallbackManager)
- [x] Polling client implementation (sign-in, channel management, messaging, file sharing)
- [x] Thread-safe server state management
- [x] Configuration service (host/port configuration)
- [x] Validation service (user ID, channel name, file validation)
- [x] File helper service (file type/size validation)
- [x] Duplex client WCF integration with DuplexChannelFactory
- [x] Duplex callback handler (IChatCallback)
- [x] WPF Dispatcher marshaling for callbacks
- [x] Duplex client connection loss detection
- [x] Duplex ChannelListView UI (matching polling client: grid/list view toggle, search, create channel)
- [x] Duplex ConversationView code-behind (events, message/member/file update methods all work)
- [x] Duplex ConversationView XAML UI upgrade — full parity with polling client's 3-column layout
- [x] Duplex PrivateMessageView UI polish
- [x] Global SessionCoordinator pattern implemented in both clients
- [x] Global button hover effects implementation (ControlHelper CornerRadius, Overlay-based styling)
- [x] Project run scripts cleanup and directory organization
- [x] Final UI layout polish (Header alignments, active view toggle default selection)
- [x] Procedural SVG ribbon avatars (client-side generated, deterministic paths based on username)
- [x] Engineering guideline compliance audit and cleanup:
  - Fixed empty catch blocks in ChatService (GetClientIpAddress, DetectClientType)
  - Fixed empty catch block in ServerLogger.Write()
  - Removed unused parameter from UserManager.GetChannelMembers()
  - Renamed GetPendingPrivateMessages to ConsumePendingPrivateMessages for CQS compliance
  - Verified thread synchronization in ChannelManager
  - Verified resource definitions in Colors.xaml
- [x] Updated assignment traceability documentation without claiming unverified marks
- [x] Updated .gitignore to exclude agentic config directories
- [x] Private messaging enhancements:
  - Added local PM history storage in both Polling and Duplex clients
  - Implemented PM history restoration when reopening conversation windows
  - PM windows now close automatically when leaving a channel (channel membership requirement)
- [x] Server-enforced file channel access for uploads and downloads
- [x] Join-time polling boundary prevents pre-join public-message replay
- [x] Duplex client periodic ping removed so core duplex updates are callback-only
- [x] Channel file-message cards are clickable and use the existing download/open flow
- [x] PM windows now use channel-style grouped message metadata and a right-side file panel
- [x] PM file upload UI validates and previews a local file; server/client private-file transfer remains explicitly deferred

### In Progress
- [/] End-to-end integration testing (both clients against same server)
- [/] Final verification of all assignment requirements

### Blocked
- None

### Next Priorities
1. End-to-end integration testing (both clients against same server)
2. Demo preparation (3+ concurrent clients)

> **Implementation Rule**
>
> Do not implement architecture that exists only in this document without
> first checking the current source code. This document describes the intended
> architecture and implementation state, but the repository is the authority
> for what is actually implemented.
>
> Before modifying an existing component, inspect its current implementation
> and make the smallest change necessary to satisfy the assignment.

## Status Legend

- `[x]` Implemented and build-verified
- `[~]` Implemented but not fully tested
- `[ ]` Planned/not implemented
- `[!]` Known issue

## Assignment Requirements Traceability

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

## Project Status

**Current Phase**: Phase 6 - Final Verification (Manual verification pending)

**Last Updated**: August 23, 2026

## Completed Work

### Phase 1: Foundation 
- Created solution structure with 5 projects targeting .NET Framework 4.8
- Defined all WCF service contracts in Chat.Contracts
- Defined data contracts (User, Channel, Message, SharedFile)
- Defined shared types (MessageType, FileType)
- Added project references per dependency structure
- Created directory structure for all projects
- Added .gitignore and .gitattributes
- Moved documentation to docs/ directory

### Phase 2: Server Implementation 
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

### Phase 3: Polling Client 
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

### Phase 4: Shared Components 
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

### Phase 5: Duplex Client & Architecture Refinement
- Implemented DuplexServiceClient with WCF duplex callbacks
- Implemented ChatCallbackHandler to safely route callbacks to the WPF Dispatcher
- Built the Duplex client UI to match the visual parity of the Polling client (3-column layouts, rich message bubbles)
- Implemented `DuplexSessionCoordinator` as a global singleton to manage WCF state, abstracting logic out of the `MainWindow`
- Refactored `PollingSessionCoordinator` in the Polling client to match the same global singleton pattern
- Ensured both clients' MainWindows are now purely UI event routers without embedded WCF logic

## Architecture & Logic Distribution

### Overall Architecture

The application is distributed across multiple processes (server, polling client, duplex client) that communicate via WCF RPC over network endpoints. This demonstrates distributed systems concepts of coordinating computation across independent processes using IPC mechanisms (Lecture 1). The server and clients can run on the same physical machine during development but remain separate processes communicating through network endpoints.

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

The project is not peer-to-peer - clients never communicate directly. All communication flows through the central server, which maintains authoritative shared state.

### Distributed Components

The application is composed of independently executing components/processes:

```text
Distributed Chat Application
│
├── Chat.Server
│   ├── ChatService
│   ├── UserManager
│   ├── ChannelManager
│   ├── MessageRouter
│   ├── CallbackManager
│   └── FileHandler
│
├── Chat.Client.Polling
│   └── WPF client component
│
├── Chat.Client.Duplex
│   └── WPF client component
│
└── Chat.Contracts
    └── Shared contract/data definitions
```

Not every class in the system is itself a distributed component. `UserManager`, `ChannelManager`, `MessageRouter`, `CallbackManager`, and `FileHandler` are primarily internal server-side objects/classes. The externally accessible distributed service component is exposed through the WCF service boundary represented by `ChatService` implementing `IChatService`, `IDuplexChatService`, and `IChatCallback`.

### Why These Components Are Distributed

The architectural reasoning addresses the lecture's question: What do we distribute, where do we put the parts, and why?

**CLIENT SIDE**
- Presentation/UI
- User interaction
- Client-side validation
- Local file operations
- WCF proxy/channel

**SERVER SIDE**
- Authoritative users
- Channels and membership
- Message routing
- File validation/storage
- Duplex callback registration
- Business/application state

Clients require an independent UI/process. Shared application state must be authoritative and coordinated centrally. Multiple clients need to access the same users, channels, messages, and files. The server therefore owns the authoritative application state. Clients do not directly access server-side objects or memory. WCF provides the service boundary through which clients communicate with the server. This separation allows multiple independent client processes to interact with one server process.

### Objects vs Distributed Components

The distinction from Lecture 1:

```text
Client
   │
   │ WCF RPC
   ▼
Distributed Service Component
ChatService
   │
   │ local method calls
   ▼
Internal Objects
├── UserManager
├── ChannelManager
├── MessageRouter
├── CallbackManager
└── FileHandler
```

`ChatService` is the externally exposed service boundary. The managers are implementation objects inside the server. Clients should not know about or directly reference those internal objects. Clients communicate through service contracts. The implementation details behind the service boundary are hidden from the client.

Example: `service.SignIn(userId)` is a remote operation. Inside the server, `_userManager.SignIn(userId)` is a local operation. The distributed boundary exists between the client WCF proxy/channel and the server WCF service, not between every individual server class.

### RPC Lifecycle

WCF provides the RPC infrastructure, handling serialization, marshaling, and transport. From the client's perspective, remote calls (`service.SignIn()`) appear similar to local method calls, but they cross process/network boundaries with different failure characteristics (Lecture 1). The architecture has two layers: RPC calls from client to server via WCF, then local procedure calls within the server (e.g., `_userManager.SignIn()`).

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

Remote calls have multiple possible failure points (network, server availability, serialization) that local calls do not. A communication exception does not necessarily prove the server-side operation did not occur.

### RPC as Communication Between Components

RPC is the communication mechanism between distributed components, not the architectural goal itself.

```text
Distributed Component
        │
        │ RPC
        ▼
Distributed Component
```

Mapped to this application:

```text
Polling Client Component
        │
        │ BasicHttpBinding / WCF RPC
        ▼
Chat.Server Service Component
```

and:

```text
Duplex Client Component
        │
        │ NetTcpBinding / WCF RPC
        ▼
Chat.Server Service Component
        │
        │ Callback RPC
        ▼
Duplex Client Component
```

Lecture 1 distinguishes the communication problem from the architectural question: RPC solves how components communicate; the architecture determines what functionality is placed into each component.

### Service-Oriented Architecture

The application follows a service-oriented approach at the distributed boundary. `ChatService` is a service that exposes operations to clients.

```text
Client
  │
  ├── SignIn()
  ├── CreateChannel()
  ├── JoinChannel()
  ├── SendMessage()
  ├── SendPrivateMessage()
  ├── ShareFile()
  └── GetMessages()
  │
  ▼
ChatService
```

Clients request services rather than accessing server objects. Server implementation is encapsulated behind the service contract. Clients depend on the contract rather than the implementation. The server remains responsible for shared state and business logic.

### WCF Service Architecture

The three fundamental WCF endpoint concepts:

```text
Endpoint
├── Address
├── Binding
└── Contract
```

Mapped to the project:

**Polling Endpoint**
```text
Contract: IChatService
Binding: BasicHttpBinding
Address: http://localhost:9000/ChatService/Polling
```

**Duplex Endpoint**
```text
Contract: IDuplexChatService
Binding: NetTcpBinding
Address: net.tcp://localhost:8081/ChatService/Duplex
```

The contract defines what operations are available. The binding defines how communication is performed. The address identifies where the service is located. Together these form the WCF endpoint.

### WCF ChannelFactory

The client uses the shared service contract and binding configuration to create a communication channel to the server.

```text
Service Contract
      +
Binding
      ↓
ChannelFactory
      ↓
WCF Channel / Proxy
      ↓
Remote ChatService
```

For duplex communication, the duplex channel additionally associates the callback implementation with the WCF channel. The client does not directly instantiate `ChatService`.

### Runtime Service Connection

The client and server are compiled as separate applications. The client does not statically link the server's implementation classes. The client references the service contract and creates a WCF communication channel. The actual communication occurs at runtime over the configured endpoint. The server implementation can therefore remain encapsulated within the server process.

```text
Compile Time
────────────
Client
   │
   └── references Chat.Contracts

Runtime
────────
Client WCF Channel
       │
       │ network
       ▼
Server WCF Endpoint
       │
       ▼
ChatService implementation
```

`Chat.Contracts` is shared source/binary contract code, but `Chat.Server` implementation is not linked into the client.

### The Network Is Not RAM

A remote call cannot be treated as equivalent to a local method call. For this project, `service.SendMessage(message)` may fail because the server is stopped, network connection is unavailable, endpoint is unreachable, WCF communication channel has faulted, timeout occurs, or server-side operation throws an exception. This is fundamentally different from `_messageRouter.RoutePublicMessage(message)`, which is a local server-side call. A distributed system must account for communication failure because components do not share the same memory space.

### No Shared Memory Across Components

```text
Polling Client Memory
        X
        │
        X   No shared memory
        │
        X
Server Memory
```

The same applies to the duplex client. Client and server run in separate processes. Server dictionaries are not directly accessible by clients. C# object references cannot simply be passed between machines/processes. Data must cross the service boundary through WCF serialization. Data contracts define the information exchanged between processes.

### Serialization at the Component Boundary

```text
Client Object
     │
     │ Serialization
     ▼
Network Message
     │
     │ WCF
     ▼
Server
     │
     │ Deserialization
     ▼
Server Object
```

`Message`, `User`, `Channel`, and `SharedFile` are data transferred across the component boundary. The client and server do not share the same object instance. Each side receives its own deserialized representation.

### Duplex Architecture

The callback is itself a remote invocation:

```text
                 Duplex RPC

Client A
   │
   │ Request RPC
   ▼
Chat.Server
   │
   │ Callback RPC
   ▼
Client B
```

Therefore `callback.NotifyMessageReceived(message)` is not a normal local callback when viewed architecturally. It crosses the network from the server process to the client process.

### Component Boundary Diagram

```text
┌───────────────────────────────┐
│ Chat.Client.Polling           │
│                               │
│ WPF UI                        │
│ Client Services               │
│ WCF Channel                   │
└───────────────┬───────────────┘
                │
                │ RPC
                │ BasicHttpBinding
                ▼
┌─────────────────────────────────────────┐
│ Chat.Server                             │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ WCF Service Boundary                │ │
│ │ ChatService                         │ │
│ └──────────────────┬──────────────────┘ │
│                    │                    │
│             Local Calls                 │
│                    ▼                    │
│ ┌────────────┐ ┌────────────┐          │
│ │UserManager │ │ChannelMgr  │          │
│ ├────────────┤ ├────────────┤          │
│ │MessageRouter││CallbackMgr │          │
│ ├────────────┤ ├────────────┤          │
│ │FileHandler │ │In-memory   │          │
│ │            │ │state       │          │
│ └────────────┘ └────────────┘          │
└─────────────────────────────────────────┘
                ▲
                │
                │ RPC + Callback
                │ NetTcpBinding
                │
┌───────────────┴───────────────┐
│ Chat.Client.Duplex             │
│                                │
│ WPF UI                         │
│ Callback Handler               │
│ WCF Duplex Channel              │
└────────────────────────────────┘
```

### Multi-Tier Architecture

The application follows a multi-tier architecture derived from the distributed computing concepts covered in Lectures 1–4.

### Logical Tiers

1. **Display Tier**
   - WPF windows and controls
   - Handles user interaction and visual presentation
   - Must execute UI updates on the WPF UI thread

2. **Presentation Tier**
   - Client-side service interaction
   - Converts WCF service responses/callbacks into data suitable for the display tier
   - Provides the client-facing interface to the server

3. **Business Tier**
   - Hosted within the Chat Server
   - Contains user, channel, messaging and file-sharing rules
   - Maintains authoritative application state
   - Validates operations before modifying state

4. **Server-Side Application State**
   - For this assignment, persistent storage is not required
   - Application state is held in server memory
   - File contents are stored by the server while the server is running

### Physical Project Structure

The logical tiers do not necessarily correspond to separate executable projects. The solution contains:
- `Chat.Server`
- `Chat.Client.Polling`
- `Chat.Client.Duplex`
- `Chat.Contracts` (shared class library)
- `Chat.Client.Shared` (shared validation, configuration, UI resources)

Shared contracts and DTOs are placed in the shared project rather than duplicated between clients.

```text
┌─────────────────────────────────────────────────────────────┐
│                    DISPLAY / CLIENT TIER                    │
│                                                             │
│  Chat.Client.Polling          Chat.Client.Duplex            │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │ WPF Views        │         │ WPF Views        │          │
│  │ ViewModels       │         │ ViewModels       │          │
│  │ User Input       │         │ User Input       │          │
│  │ UI Validation    │         │ UI Validation    │          │
│  └────────┬─────────┘         └────────┬─────────┘          │
└───────────┼────────────────────────────┼────────────────────┘
            │                            │
            │ WCF RPC                    │ WCF Duplex RPC
            ▼                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    BUSINESS / SERVICE TIER                  │
│                                                             │
│                       Chat.Server                           │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ ChatService                                            │  │
│  │ UserManager                                            │  │
│  │ ChannelManager                                         │  │
│  │ MessageRouter                                          │  │
│  │ FileHandler                                            │  │
│  │ CallbackManager                                        │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│                    In-Memory Application State               │
└─────────────────────────────────────────────────────────────┘
```

**Why This Architecture?**

The application separates presentation concerns from server-side business logic. The WPF clients are responsible for displaying information to users, collecting user input, performing client-side validation, managing UI state, initiating WCF operations, and processing polling results or callback events. The server is responsible for user management, channel management, message routing, private messaging, file validation and storage, maintaining authoritative application state, and managing duplex callback connections.

**Why No Data Tier?**

The assignment explicitly requires server state to be held in memory and does not require persistent storage. A traditional database-backed Data Tier is intentionally not implemented. Introducing a database would add an unnecessary distributed component and would conflict with the assignment's requirement that application state does not need to survive a server restart.

**Why Not a Separate Presentation Tier?**

The WPF clients combine presentation and display responsibilities. The application does not expose a separate web/API presentation tier because the WPF applications communicate directly with the WCF service. Display + Presentation → WCF → Business/Service is sufficient for the requirements of this application.

**Architectural Trade-Off**

A four-tier architecture could theoretically separate Display Tier, Presentation Tier, Business Tier, and Data Tier. However, implementing all four would introduce additional network boundaries and components without providing a meaningful benefit for this relatively small application. The selected architecture prioritizes low coupling, clear separation of responsibilities, simplicity, maintainability, appropriate distribution, and compliance with assignment requirements.

### Client-Server and Multi-Tier Classification

The system is both a **client-server application** and a **distributed application**. The client-server classification describes the communication relationship: Client ───── RPC ─────► Server. The distributed-system classification describes how application responsibilities and execution are separated across processes and machines.

The application distributes responsibilities as follows:

| Responsibility            | Location        |
| ------------------------- | --------------- |
| User interface            | Client          |
| User input                | Client          |
| Client-side validation    | Client          |
| WCF communication         | Client / Server |
| User management           | Server          |
| Channel management        | Server          |
| Message routing           | Server          |
| File validation           | Server          |
| Shared application state  | Server          |
| Duplex event notification | Server → Client |

This separation creates a low-coupling boundary between the clients and the server. The clients do not access server implementation classes directly. They interact through the WCF service contracts.

```text
Client implementation
       │
       │ Contract boundary
       ▼
IChatService / IDuplexChatService
       │
       │ RPC
       ▼
ChatService
       │
       ▼
Server managers
```

The WCF service therefore acts as the distributed component interface between the client and business/service tier.

### Components vs Internal Objects

The project distinguishes between distributed components and internal implementation objects.

**Distributed Components**

The WCF services represent the externally accessible components of the server. The primary component interfaces are `IChatService`, `IDuplexChatService`, and `IChatCallback`. These interfaces define the operations available across the process boundary.

```text
Client
   │
   │ WCF
   ▼
┌──────────────────────────────┐
│ Distributed Service Component│
│                              │
│ IChatService                 │
│ IDuplexChatService           │
└──────────────┬───────────────┘
               │
               ▼
        Internal objects
```

**Internal Objects**

Classes such as `UserManager`, `ChannelManager`, `MessageRouter`, `FileHandler`, and `CallbackManager` are internal implementation objects. They are not directly accessible to clients.

```text
Client
  │
  │ RPC
  ▼
ChatService.SignIn()
  │
  │ Local method call
  ▼
UserManager.SignIn()
```

`ChatService` forms part of the distributed service boundary, whereas `UserManager` is an internal server-side implementation object. This distinction is important because internal object references cannot be directly shared across the network. Only the service contract and serializable data cross the distributed boundary.

### Distributed Boundary and Network Failure

The WCF service boundary introduces distributed-system failure modes that do not exist with ordinary local method calls. For example, `_userManager.SignIn(userId)` is a local operation inside the server process. In contrast, `service.SignIn(userId)` from the client crosses the distributed boundary.

The remote call may be affected by network failure, server failure, connection timeout, endpoint configuration errors, serialization failures, client disconnection, or server-side exceptions. Therefore, a remote method call must not be treated as equivalent to a local method call. The client must handle communication failures explicitly and provide appropriate feedback to the user.

**Distributed State**

The server maintains the authoritative application state. Clients maintain local UI state, but they cannot directly modify server state.

```text
Client A ──────┐
               │
Client B ──────┼──► Server State
               │
Client C ──────┘
```

This ensures that operations such as user registration, channel membership, and message routing are centrally coordinated by the server.

### Polling Strategy

The polling client periodically initiates request/response RPC operations to check for updates. The polling interval is configurable, with a default interval of 2 seconds.

```text
Client
   │
   │ GetMessages()
   ▼
Server
   │
   │ Response
   ▼
Client
   │
   │ wait
   ▼
Client
   │
   │ GetMessages()
   ▼
Server
```

Polling is inherently synchronous at the communication level because the client sends a request and receives a response. However, the client should avoid blocking the WPF UI thread while waiting for remote operations. Where supported by the implementation, asynchronous WCF calls can be represented using `Task` and `await`.

```csharp
private async Task PollMessagesAsync()
{
    var messages = await service.GetMessagesAsync(...);
    UpdateMessages(messages);
}
```

The important distinction is: Polling determines when the client asks the server for updates. Async/Await determines how the client waits for the remote operation. WCF RPC determines how the request is communicated to the server. These are separate concepts.

**UI Responsiveness**

The polling client must not block the WPF UI thread while waiting for network communication.

```text
WPF UI Thread
     │
     ├── Display UI
     │
     ├── Handle user input
     │
     └── Start asynchronous RPC
                │
                ▼
          WCF Communication
                │
                ▼
             Server
                │
                ▼
          RPC completion
                │
                ▼
       Resume UI operation
```

This allows the application to remain responsive while network operations are in progress.

### Asynchronous Communication

The application distinguishes between synchronous RPC, asynchronous execution, and one-way WCF operations. These mechanisms solve different problems.

**Synchronous Request/Response RPC**

A normal WCF operation follows: Client → Request → Server → Response → Client. The caller logically waits for the operation to complete. For a WPF application, performing a long-running remote operation synchronously on the UI thread can make the interface appear frozen.

**Task-Based Asynchronous Operations**

C# provides `Task` and `async`/`await` for asynchronous operations.

```csharp
public async Task SendMessageAsync(Message message)
{
    await service.SendMessageAsync(message);
}
```

`await` allows the method to suspend while the asynchronous operation is incomplete without blocking the current thread. This is particularly useful for WPF because the UI thread remains available to process user input, window rendering, animations, UI events, and other application work.

**Task vs Thread**

A `Task` should not be treated as simply another name for a thread. A thread represents an execution resource. A `Task` represents an asynchronous operation that may complete in the future. For network I/O, asynchronous operations can avoid occupying a thread while waiting for the network operation to complete.

**Async Exception Handling**

Exceptions from an awaited asynchronous operation are propagated through the `await` expression. Therefore remote communication should be handled using normal exception handling:

```text
try
{
    await SendMessageAsync(message);
}
catch (CommunicationException)
{
    // Handle WCF communication failure
}
catch (TimeoutException)
{
    // Handle timeout
}
```

### One-Way WCF Operations

WCF supports one-way operations using `[OperationContract(IsOneWay = true)]`. A one-way operation does not return a result or `out` parameter to the caller.

```text
[OperationContract(IsOneWay = true)]
void ProcessData(string data);
```

The client does not receive a normal operation response. However, one-way does not mean that the operation is completely independent of the network. The client still needs to establish communication with the service, and communication failures can still occur. Furthermore, the client does not receive confirmation that the server-side operation completed successfully. Therefore, one-way operations should only be used where the application does not require a response or confirmation.

For this chat application, normal request/response operations remain appropriate for operations where the client needs a result, such as sign-in, getting channels, getting messages, creating a channel, and downloading files. One-way communication may be considered for suitable fire-and-forget operations, but it should not replace request/response communication where confirmation is required.

### Duplex Communication and Asynchronous Execution

The duplex client provides a different form of asynchronous communication from `async`/`await`.

**Duplex Communication**

WCF duplex communication allows the server to initiate communication with the client through a callback contract: Client ───── Request ─────► Server, Client ◄──── Callback ───── Server. The client does not need to continuously request new messages.

**Async/Await**

`async`/`await` is a C# programming model for asynchronously waiting for operations. Duplex: Who initiates communication? Server can initiate callbacks. Async/Await: How does the application wait for an operation? The caller can await completion without blocking the current thread. These are complementary concepts rather than alternatives.

The duplex client therefore uses WCF duplex callbacks for server-initiated events, Dispatcher marshaling for UI thread safety, `Task`/`async`/`await` where asynchronous client operations are appropriate, and WCF communication exception handling for network failures.

### Thread Safety and Synchronization

Different parts of the application have different concurrency requirements.

**Server**

The server may process multiple client requests concurrently. Shared state therefore requires synchronization. The server uses `ReaderWriterLockSlim` to protect shared in-memory state. This prevents concurrent operations from corrupting user collections, channel collections, message collections, file metadata, and callback registrations.

**Polling Client**

The WPF polling client uses the WPF Dispatcher for UI operations. If remote operations are performed asynchronously, the UI must only be updated from the appropriate UI context.

```text
Async WCF operation
        │
        ▼
     Result
        │
        ▼
WPF Dispatcher
        │
        ▼
    UI update
```

**Duplex Client**

WCF callbacks may execute on a thread other than the WPF UI thread. Therefore callback handlers must marshal UI changes through the WPF Dispatcher.

```text
Application.Current.Dispatcher.Invoke(() =>
{
    // Update UI
});
```

The callback handler should perform minimal work before dispatching the update to the UI.

### Operation Classification

Every major service/API operation should be classified during implementation planning.

| Operation Type | Expected Behaviour | Example |
|---|---|---|
| Synchronous | Caller waits for result | GetChannels() |
| Asynchronous | Caller continues while operation executes | SendMessageAsync() |
| One-way | Caller sends command without requiring result | (potential future use) |
| Remote callback | Server sends notification to client | NotifyMessageReceived() |
| Local GUI dispatch | Worker thread updates GUI through UI thread | UpdateMessageList() |

This classification should be documented for important service operations rather than deciding asynchronously on an ad-hoc basis during implementation.

### Asynchronous Communication Decision Rule

The system should distinguish between synchronous request/response operations, asynchronous operations, one-way operations, remote callbacks/server-to-client notifications, GUI-thread updates, and thread-safe access to shared state. Do not make every remote operation asynchronous by default.

**Use asynchronous execution when:**
- The operation may take a significant amount of time
- The operation involves heavy computation
- The operation involves intensive disk I/O
- The operation involves long-running database operations
- The operation involves remote communication where the caller should remain responsive
- A GUI client would otherwise become unresponsive
- Multiple independent operations can execute concurrently

**Keep an operation synchronous when:**
- It is short-running
- The caller genuinely needs the result immediately
- Making it asynchronous would add unnecessary complexity
- There is no meaningful responsiveness or resource-utilisation benefit

### Async/Await/Task Implementation

The C# implementation uses the modern Task-based asynchronous programming model where appropriate. Core concepts: `async` identifies an asynchronous method, `await` asynchronously waits for a `Task`, `Task` represents an asynchronous operation, `Task<T>` represents an asynchronous operation that produces a result.

```text
public async Task<Student> GetStudentAsync(int id)
{
    return await dataAccess.GetStudentAsync(id);
}
```

The important architectural point is that asynchronous behaviour should propagate through the relevant layers: UI → Presentation/API → Business Tier → Data Tier → Database/External Service. Avoid introducing asynchronous code in only one layer while the surrounding layers remain unnecessarily blocking.

### Async Propagation Through Tiers

Where an operation is genuinely asynchronous, the project plans for asynchronous propagation across the applicable tiers.

```text
UI
 |
 | await where appropriate
 v
WCF Client
 |
 | remote operation
 v
Chat Server
 |
 +--> Business Logic
 |
 +--> In-Memory State
```

This prevents a situation where an asynchronous operation is immediately converted back into a blocking operation at another layer. Avoid patterns such as `var result = SomeAsyncMethod().Result;` or `SomeAsyncMethod().Wait();` in GUI/application code unless there is a specific architectural reason. Prefer `var result = await SomeAsyncMethod();`.

Note: The application uses in-memory state rather than a database. Server-side business logic primarily uses synchronous operations, while asynchronous execution is used for client-side network operations to maintain UI responsiveness.

### Thread-Safety as Design Requirement

Distributed components must not assume that clients will serialize access to them. A service may receive multiple simultaneous client calls. Therefore: Components containing shared mutable state must be designed to be externally thread-safe. The design should minimise shared mutable state wherever possible. Prefer local variables, immutable data, stateless services, isolated state per request, database transactions where appropriate, and controlled synchronization around genuinely shared state. Avoid unnecessary global/member state.

### Race-Condition Analysis

The implementation plan includes explicit testing for concurrency-related failures. Potential race-condition outcomes include lost updates, out-of-date data, inconsistent state, incorrect counters, duplicate operations, data corruption, and invalid intermediate states.

```text
Client A ----\
              ---> Shared Resource
Client B ----/
```

If both clients update the same resource concurrently, the design must define how that access is synchronized.

### Synchronization Strategy

Where synchronization is required, a deliberate synchronization strategy is selected. Possible approaches include framework-provided synchronization, `lock`, mutexes, semaphores, database-level transactions, atomic operations, architectural elimination of shared state. For this project, synchronization is applied at the business-logic level where possible rather than indiscriminately locking low-level collections or every method.

**Important rule:** Do not synchronize everything. Excessive locking can reduce concurrency, increase latency, create contention, cause deadlocks, and make the system slower than a single-threaded implementation. The objective is controlled concurrency, not maximum locking.

### Distinguishing One-Way from Async Calls

The architecture documentation explicitly distinguishes these concepts.

**One-Way:**
```text
Client
  |
  | send command
  v
Server
  |
  | continues processing
  v
(no result returned)
```

**Asynchronous call:**
```text
Client
  |
  | start operation
  v
Background execution
  |
  | result later
  v
Client
```

**Key distinction:** A one-way operation is about the communication contract ("I don't need a response"). An asynchronous call is about the execution model ("I don't want the caller to wait for completion"). These concepts can be used independently.

### Remote Callback Architecture

For long-running operations where the client needs progress information, a callback mechanism is considered.

```text
Client
  |
  | StartLongRunningJob()
  v
Server
  |
  | processing
  |
  +----> ProgressUpdate(20)
  |
  +----> ProgressUpdate(40)
  |
  +----> ProgressUpdate(60)
  |
  +----> ProgressUpdate(100)
```

For WCF this can be implemented using a duplex channel. The callback contract can be defined separately and associated with the service contract.

### GUI Thread Safety Requirements

For the WPF GUI, callbacks and asynchronous operations must not directly modify GUI controls from arbitrary worker threads. The WPF GUI operates around an event loop.

```text
Worker Thread
     |
     | request GUI update
     v
Dispatcher
     |
     v
GUI/Event Thread
     |
     v
GUI Control
```

```text
Application.Current.Dispatcher.Invoke(() =>
{
    progressBar.Value = progress;
});
```

For non-blocking GUI dispatch, use the appropriate asynchronous dispatcher mechanism. The architecture treats GUI state as belonging to the GUI thread.

### UI Responsiveness as Non-Functional Requirement

For the WPF GUI, responsiveness is explicitly documented as a requirement. Long-running operations should not execute directly on the GUI event thread. Avoid Button Click → Long Operation → GUI frozen. Prefer Button Click → Start Async Operation → GUI remains responsive → Operation completes → Update GUI. The UI should provide appropriate feedback where an operation is expected to take noticeable time: progress indicator, spinner, status message, progress percentage, cancellation option where appropriate.

### Async Error Handling

Asynchronous operations explicitly consider failure. Potential failures include network timeout, server unavailable, database failure, remote exception, cancellation, connection loss, and invalid response. Use appropriate exception handling around awaited operations.

```text
try
{
    var result = await service.ProcessAsync();
}
catch (TimeoutException)
{
    // Handle timeout
}
catch (Exception ex)
{
    // Handle unexpected failure
}
```

Do not silently swallow asynchronous exceptions.

### Cancellation for Long-Running Operations

For genuinely long-running operations, cancellation is considered. Example conceptual API: `Task<Result> ProcessAsync(CancellationToken cancellationToken);`. The project determines whether each long-running operation needs progress reporting, cancellation, timeout handling, retry behaviour, and failure notification. These are explicit design decisions rather than accidental behaviour.

### Architecture Decision Process

For every major component/service, the project plan answers:

```text
Component: <component name>
Tier: <presentation / business / data / display>
Responsibilities: <responsibilities>
Communication: <request-response / one-way / duplex>
Long-running operations: <yes/no>
Async required: <yes/no>
Reason: <reason>
Shared mutable state: <yes/no>
Thread-safe: <yes/no>
Synchronization: <mechanism or N/A>
Progress reporting: <yes/no>
Cancellation: <yes/no>
Failure/timeout handling: <approach>
```

### Concurrency Testing

Testing verifies not only that individual operations work, but also simultaneous execution. Minimum concurrency test cases:
- Two clients calling the same operation simultaneously
- Multiple clients modifying the same resource
- Multiple asynchronous operations running simultaneously
- Server callback while the client is performing other work
- GUI callback while the GUI is processing another event
- Server failure during an asynchronous operation
- Client disconnect during a callback
- Timeout during a remote call
- Long-running operation followed by cancellation

### Architecture Trade-Offs

The architecture explicitly recognises that distribution introduces costs.

**Benefits:** Modularity, lower coupling, scalability, load balancing, fault isolation, multiple clients, specialised services, better separation of concerns.

**Costs:** Network latency, network failure, serialization/deserialization, timeout handling, concurrency, thread synchronization, distributed state, more complex debugging, more complicated deployment, more difficult failure diagnosis.

The architecture distributes components only where the benefits justify these costs.

### Distribution Strategy

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

### Polling Client Communication Strategy

The Polling Client uses asynchronous polling to obtain updates from the server. The polling loop periodically requests changes including:
- channel list changes
- channel membership changes
- new channel messages
- private messages
- shared files

The polling timer itself executes on the WPF Dispatcher thread. However, the network operation triggered by the timer should not block the UI thread when the operation may take noticeable time.

**Rationale:** Polling is intentionally used for Section A because the assignment specifically requires a pull-based strategy. The polling interval should provide a reasonable balance: too short → unnecessary server/network load; too long → poor perceived responsiveness. The polling mechanism is therefore configurable rather than using hard-coded delays.

### Polling vs Duplex Comparison

| Feature | Polling Client | Duplex Client |
|---|---|---|
| Communication model | Pull | Push |
| Update mechanism | Periodic server requests | WCF callbacks |
| Background polling | Required | Prohibited |
| Refresh button | Optional fallback only | Prohibited for core updates |
| WCF channel | Normal service channel | Duplex channel |
| Callback contract | No | Yes |
| Real-time updates | Polling interval dependent | Server initiated |
| UI thread handling | Required for background results | Required for callbacks |
| Section | A/B | C |

### Duplex Client Architecture

The Duplex Client uses WCF duplex communication. The client provides a callback implementation to the server when establishing the connection.

```text
Client                          Server

IServer
  |                               |
  |---- Register / Connect ------>|
  |                               |
  |<--- Callback -----------------|
  |                               |
  |<--- ChannelUpdate ------------|
  |<--- MessageReceived ----------|
  |<--- MemberUpdate -------------|
  |<--- PrivateMessage -----------|
  |<--- FileShared ---------------|
```

The server maintains the callback associated with each signed-in user. When an event occurs, the server invokes the relevant callback rather than waiting for the client to poll.

**Duplex Client Restrictions:** The Duplex Client must contain no polling timer, no polling thread, no periodic refresh operation, and no refresh button used to implement core real-time updates. All real-time updates must arrive through the WCF callback channel.

### Duplex Contracts

The duplex service contract follows the WCF pattern:

```csharp
[ServiceContract(CallbackContract = typeof(IChatCallback))]
public interface IChatService
{
    ...
}

[ServiceContract]
public interface IChatCallback
{
    ...
}
```

The callback contract provides operations for server-to-client notifications:
- `ChannelListUpdated`
- `MemberListUpdated`
- `MessageReceived`
- `PrivateMessageReceived`
- `FileShared`
- `UserDisconnected`

Callbacks that do not require a response are candidates for `[OperationContract(IsOneWay = true)]` to avoid unnecessarily blocking the server while delivering notifications.

### Callback Terminology

The project distinguishes between two different concepts.

**Async Completion Callback:** A completion callback informs the caller that an asynchronous operation has completed.

```text
Client
 |
 | start async operation
 v
Task / worker
 |
 | completed
 v
Completion callback
```

**Remote Callback:** A remote callback is server-to-client communication during a distributed operation.

```text
Client
 |
 | RPC
 v
Server
 |
 | callback
 v
Client
```

The Duplex Client uses **remote callbacks**. It must not be described as merely an "async completion callback."

### Disconnection Handling

The server must handle clients disappearing without performing an explicit sign-out. Possible causes include:
- WPF window closed using X
- application crash
- process termination
- network/channel failure

The server must:
1. Detect the failed/disconnected client
2. Release its user ID
3. Remove it from its current channel
4. Remove its duplex callback registration
5. Notify remaining channel members
6. Continue serving other connected clients

A failed callback must not crash the server or prevent notifications to other clients.

**Cleanup Invariant:** After a client has disconnected:
```text
User ID       → released
Channel       → user removed
Callback      → removed
Other clients → notified
Server        → continues running
```

### Server State

The server owns all authoritative application state.

**User State:**
```text
User
- UserId
- CurrentChannel
- Connection / Callback information
```

**Channel State:**
```text
Channel
- Name
- Members
- SharedFiles
```

**Message State:** Messages are transient. The server does NOT maintain message history. A message is distributed only to users who are members of the channel at the time it is sent.

**File State:**
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

File contents must exist on the server. A local path from one client must never be sent to another client as the mechanism for file sharing.

### Server Invariants

The following conditions must always hold:
1. Every signed-in user ID is unique
2. A user belongs to at most one channel
3. A channel name is unique
4. A user can only send channel messages to their current channel
5. A private message can only be sent between members of the same channel
6. A channel message is delivered only to members present when it is sent
7. Message history is not retained
8. A file is accessible only to members of the channel where it was shared
9. Files exceeding 2 MB are rejected
10. Unsupported file extensions are rejected
11. A disconnected user no longer occupies their user ID
12. A disconnected user is removed from their channel
13. A dead callback cannot terminate the server
14. Duplex clients receive updates through callbacks rather than polling

### File Sharing

**Allowed Extensions:** Only `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`, `.txt` are accepted.

**Size Limit:** Maximum 2 MB. The server is responsible for enforcing both extension and size restrictions.

**Transfer Model:**
```text
Client
  |
  | file bytes
  v
Server
  |
  | store
  v
Server File State
  |
  | file bytes
  v
Client
```

The system must never rely on clients sharing local filesystem paths.

**Security/Validation Principle:** Validation must occur on the server even if the client performs preliminary validation. The client-side validation exists for user experience. The server-side validation is authoritative.

### Message Semantics

**Channel Messages:** When a user sends a message:
1. Server verifies the sender is currently in a channel
2. Server obtains the current members of that channel
3. Server distributes the message to those members
4. No message history is stored

A user joining later must not receive messages sent before joining.

**Private Messages:** When a user sends a private message:
1. Verify sender is signed in
2. Verify recipient exists
3. Verify both users are currently members of the same channel
4. Deliver only to the intended recipient
5. The server must not broadcast the private message to the channel

### Solution Structure

```text
Assignment1/
│
├── Chat.Server/
│   ├── Program.cs
│   ├── Services/
│   │   └── ChatService.cs
│   ├── StateManagement/
│   │   ├── UserManager.cs
│   │   ├── ChannelManager.cs
│   │   ├── MessageRouter.cs
│   │   ├── FileHandler.cs
│   │   └── CallbackManager.cs
│   └── Models/
│
├── Chat.Client.Polling/
│   ├── App.xaml
│   ├── Views/
│   │   ├── SignInWindow.xaml
│   │   ├── ChannelListWindow.xaml
│   │   ├── ChannelWindow.xaml
│   │   └── PrivateChatWindow.xaml
│   ├── Services/
│   │   └── PollingClientService.cs
│   └── ViewModels/
│
├── Chat.Client.Duplex/
│   ├── App.xaml
│   ├── Views/
│   │   ├── SignInWindow.xaml
│   │   ├── ChannelListWindow.xaml
│   │   ├── ChannelWindow.xaml
│   │   └── PrivateChatWindow.xaml
│   ├── Services/
│   │   ├── DuplexClientService.cs
│   │   └── ChatCallbackHandler.cs
│   └── ViewModels/
│
├── Chat.Contracts/
│   ├── IChatService.cs
│   ├── IDuplexChatService.cs
│   ├── IChatCallback.cs
│   ├── ChatMessage.cs
│   ├── PrivateMessage.cs
│   ├── SharedFile.cs
│   └── DTOs/
│
└── Chat.Client.Shared/
    ├── Services/
    │   ├── ValidationService.cs
    │   ├── FileHelperService.cs
    │   └── ConfigurationService.cs
    └── Resources/
        ├── Colors.xaml
        ├── Sizing.xaml
        └── Converters.xaml
```

### Implementation Order

Implementation must follow the assignment's recommended progression.

**Phase 1 — Shared Contracts:** Implement service contract, DTOs, message models, channel models, file metadata models. Do not implement UI yet.

**Phase 2 — Server Core:** Implement and test user sign-in, sign-out, channel creation, join/leave channel, channel listing, public messaging, private messaging, file upload/download, thread-safe state management.

**Phase 3 — Polling Client:** Implement sign-in, channel list, channel creation, join/leave, public conversation, member list, private conversations, file sharing, sign-out, background polling, UI thread marshaling. The polling client must be functionally complete before beginning the duplex client.

**Phase 4 — Concurrency Testing:** Run 3–5 clients, duplicate sign-ins, simultaneous channel joins, simultaneous messages, private messages, simultaneous file sharing, client disconnect via X, server-side state verification.

**Phase 5 — Duplex Client:** Implement callback contract, duplex `ChannelFactory`, callback registration, server callback storage, public message callbacks, member updates, channel updates, private-message callbacks, file notifications, WPF Dispatcher handling, disconnection cleanup.

**Phase 6 — Demonstration Testing:** Run both client types simultaneously against the same server. Verify that polling client sees duplex-client messages, duplex client sees polling-client messages, both clients share the same channel state, files are accessible from both clients, killing a client releases its ID, remaining clients continue operating.

### Assessment Test Matrix

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
| Polling client receives update | Update arrives without user action | A2/A4/A6 |
| Duplex client receives update | Callback delivers update | C1/C2 |
| Duplex client has no polling | No timer/background refresh path | C2 |
| Concurrent server operations | State remains consistent | C3 |
| Callback updates WPF UI | No cross-thread exception | C3 |
| Kill client with X | Server detects/removes user | C4 |
| Dead callback | Server continues serving others | C4 |
| Polling + duplex together | Both share same server state | C1/C2 |

### Assignment Constraints

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

### Lecture-to-Implementation Mapping

| Lecture Concept | Application |
|---|---|
| Components | Chat Server, Polling Client and Duplex Client act as distributed components |
| Service-oriented architecture | WCF service exposes chat functionality |
| RPC | Clients invoke operations on the remote Chat Server |
| Multi-tier architecture | Display, presentation and server-side business responsibilities are separated |
| Business tier | User/channel/message/file rules live on the server |
| Presentation tier | Client-facing WCF service interface |
| Display tier | WPF UI |
| Asynchronous communication | Polling and long-running client operations avoid blocking the UI |
| `async` / `await` | Client network operations |
| Threads | Concurrent WCF requests and background polling |
| Thread safety | Synchronisation of shared server state |
| One-way calls | Suitable server-to-client notification callbacks |
| Remote callbacks | Duplex client's server-to-client updates |
| Duplex channels | WCF callback communication |
| Dispatcher | Marshals callback updates onto WPF UI thread |
| Delegates | Used through WPF/WCF callback and Dispatcher mechanisms |
| Lambda expressions | May be used for concise Dispatcher actions |

### Implementation Priority

The implementation order is approximately:
1. Identify application responsibilities
2. Identify appropriate tiers
3. Define interfaces between tiers
4. Identify components/services that should be remotely accessible
5. Identify long-running operations
6. Classify operations as synchronous, asynchronous, one-way or callback-based
7. Define shared state
8. Define concurrency requirements
9. Implement thread-safe service components
10. Implement asynchronous operations using `Task`/`async`/`await` where appropriate
11. Implement callbacks/duplex communication only where required
12. Implement GUI dispatcher logic where required
13. Add timeout/error/cancellation handling
14. Test concurrent execution
15. Test network/service failures
16. Measure responsiveness and performance
17. Reassess whether the distribution actually provides a benefit

### Design Principle Hierarchy

The project is NOT designed around "everything should be asynchronous." Instead: "Operations should use the simplest communication and execution model that satisfies their performance, responsiveness and distribution requirements."

**Decision hierarchy:**
```text
Does the operation need a result?
        |
       Yes
        |
        v
Is it expected to be long-running?
      /   \
    No     Yes
    |       |
    v       v
Sync     Async/Task
```

For operations that do not require a result:
```text
Does the client need confirmation?
      /        \
    Yes         No
     |           |
     v           v
Normal        One-way
request       operation
```

For operations where the server must proactively notify the client:
```text
Server needs to notify client?
            |
           Yes
            |
            v
       Remote Callback
            |
            v
      Duplex Channel
```

### Relationship Between Lecture 1-4 Concepts

The architecture connects the concepts from the lectures rather than treating them as independent topics.

```text
Components
    |
    v
Services / RPC
    |
    v
Multi-Tier Architecture
    |
    v
Distributed Components
    |
    v
Network Communication
    |
    +------------------+
    |                  |
    v                  v
Synchronous        Asynchronous
Calls              Calls
                       |
                       v
                 Task / await
                       |
                       v
                 Concurrency
                       |
                       v
                Thread Safety
                       |
             +---------+---------+
             |                   |
             v                   v
        Synchronization     Remote Callback
                                 |
                                 v
                         Duplex Communication
```

### Final Architecture Checklist

Before finalising the system architecture, verify:
- [ ] Each component has a clearly defined responsibility
- [ ] Each tier has a clearly defined responsibility
- [ ] Interfaces between tiers are explicitly defined
- [ ] Distribution is justified rather than arbitrary
- [ ] Long-running operations have been identified
- [ ] Async operations are explicitly identified
- [ ] `async`/`await`/`Task` are used where appropriate
- [ ] Synchronous operations remain synchronous where appropriate
- [ ] One-way operations are used only where no response is required
- [ ] Remote callbacks are used only where server-to-client notification is required
- [ ] GUI updates occur on the GUI/event thread
- [ ] Shared mutable state has been identified
- [ ] Stateful components are thread-safe
- [ ] Race conditions have been considered
- [ ] Synchronization is applied deliberately
- [ ] Network failures and timeouts are handled
- [ ] Long-running operations have appropriate user feedback
- [ ] Cancellation is considered where appropriate
- [ ] Concurrent clients have been tested
- [ ] The number of tiers is justified
- [ ] Distribution provides a measurable architectural benefit

### Architectural Rationale

The selected architecture is intentionally simpler than a full four-tier distributed system.

**Requirements**

The assignment requires a server, a polling client, a duplex client, shared server-side state, real-time communication, file sharing, private messaging, and multiple concurrent clients.

**Architectural Decision**

The application uses WPF Clients → WCF → Chat.Server → In-Memory State. This provides sufficient separation between display/presentation, distributed service boundary, business logic, and application state without introducing unnecessary distributed components.

**Why Not Add a Database?**

The assignment specifies in-memory server state. A database would introduce another component (Client → Server → Database), creating an additional distributed boundary without being necessary for the required functionality.

**Why Not Add a Separate API Gateway?**

The application has only two WPF client implementations and one server. A separate presentation/API tier would add another network hop (Client → API → Business Service). For this project, this would increase complexity without providing meaningful load-balancing or service-specialisation benefits.

**Why Have Two Clients?**

The two clients are deliberately implemented using different communication models:

| Client  | Communication    | Architectural Purpose                       |
| ------- | ---------------- | ------------------------------------------- |
| Polling | Request/Response | Demonstrates client-initiated communication |
| Duplex  | Callback RPC     | Demonstrates server-initiated communication |

This makes the project useful for demonstrating distributed communication concepts rather than simply implementing a single chat client.

### Communication Model Comparison

The project demonstrates two different approaches to distributing communication.

| Property | Polling Client | Duplex Client |
|---|---|---|
| Communication initiation | Client | Client + Server |
| New message detection | Periodic polling | Server callback |
| WCF model | Request/Response | Duplex |
| Client requests required for updates | Yes | No |
| Network overhead | Periodic requests | Event-driven |
| Real-time behaviour | Depends on polling interval | Immediate callback |
| UI asynchronous handling | Important | Important |
| Server callback required | No | Yes |
| Main educational purpose | Request/response RPC | Bidirectional RPC |

**Polling**
```text
Client ── GetMessages() ──► Server
Client ◄──── Messages ───── Server
       wait 2 seconds
Client ── GetMessages() ──► Server
```

**Duplex**
```text
Client ───── Request ─────► Server
Server ─── Callback ──────► Client
Server ─── Callback ──────► Client
Server ─── Callback ──────► Client
```

The duplex architecture avoids repeatedly querying the server for new events. However, it introduces additional complexity around callback lifetime, connection management, concurrency, and UI-thread synchronization.

### Project Components Mapped to Multi-Tier Architecture

| Project Component | Tier / Role | Responsibility |
|---|---|---|
| `Chat.Client.Polling` | Display + Presentation | WPF interface and polling communication |
| `Chat.Client.Duplex` | Display + Presentation | WPF interface and callback communication |
| `Chat.Client.Shared` | Client Support | Shared validation, configuration and UI resources |
| `IChatService` | Service Interface | Defines polling RPC operations |
| `IDuplexChatService` | Service Interface | Defines duplex RPC operations |
| `IChatCallback` | Callback Interface | Defines server-to-client callbacks |
| `ChatService` | Business / Service Tier | Exposes distributed operations |
| `UserManager` | Business Logic | User management |
| `ChannelManager` | Business Logic | Channel management |
| `MessageRouter` | Business Logic | Message distribution |
| `FileHandler` | Business Logic | File validation and storage |
| `CallbackManager` | Service Infrastructure | Callback registration and notification |
| In-memory dictionaries | Data State | Server-side authoritative state |

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

WCF callback methods are not guaranteed to execute on the WPF UI thread. The duplex client must marshal callback invocations to the UI thread using `Dispatcher.Invoke` or `Dispatcher.BeginInvoke` (Section C requirement).

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

### Polling Logic Flow

The polling client repeatedly asks the server for updates (request/response RPC pattern). The duplex client, by contrast, establishes a callback channel and the server can notify the client when events occur, eliminating the need for periodic polling (Section C requirement).

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

The server acts as the authoritative coordinator for shared application state, maintaining active users, channel membership, pending messages, private-message routing, file metadata/storage, and registered duplex callbacks. Clients do not coordinate directly with one another - all communication flows through the server.

```text
Client A ───────► Server ───────► Client B
                    │
                    │
                    ▼
              Shared State
```

### Distribution Transparency

WCF allows the client to interact with the remote service using familiar C# method-call syntax (`service.SendMessage(message)`), hiding much of the underlying networking implementation. However, the application is not equivalent to a local application due to latency, network failures, server unavailability, serialization requirements, and shared state modifications by concurrent clients. This demonstrates both the convenience and limitations of RPC abstraction (Lecture 1).

### WPF and C# in the Project

The clients are implemented using C#, .NET Framework 4.8, and WPF. WPF provides the local graphical user interface, while WCF provides RPC communication with the distributed server. WPF itself is not the distributed component - it's the local UI framework used by the client processes.

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
- **Polling**: `http://localhost:9000/ChatService/Polling` (BasicHttpBinding)
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

## COMP3008 Lecture 1 Concept Mapping

| Lecture Concept | Project Implementation | Why Applied |
|---|---|---|
| Component | Client/server service boundary | Separates distributed concerns across process boundaries |
| Service | `ChatService` | Provides distributed operations through WCF |
| Service contract | `IChatService`, `IDuplexChatService`, `IChatCallback` | Defines interface between distributed components |
| RPC | WCF service invocation | Enables remote procedure calls across network |
| Endpoint | Address + binding + contract | Specifies how clients access distributed service |
| BasicHttpBinding | Polling RPC | Simple request/response communication model |
| NetTcpBinding | Duplex RPC | Supports bidirectional communication |
| Distributed state | Server-side user/channel/message state | Central authoritative state for coordination |
| Objects | Server managers and client-side classes | Internal implementation within component boundaries |
| Component boundary | WCF service boundary | Defines what crosses the distributed boundary |
| Serialization | WCF data contract serialization | Enables data transfer across process boundaries |
| Network failure | WCF communication exceptions/disconnects | Handles distributed system failure modes |
| Runtime communication | WCF ChannelFactory/channel | Establishes communication at runtime |
| Service-oriented architecture | Clients consume server-provided services | Separates service provision from consumption |

## COMP3008 Lecture 3 Concept Mapping

| Lecture Concept | Project Implementation | Why Applied |
|---|---|---|
| Multi-tier architecture | Display/Client + Business/Service tiers | Separates presentation from business logic |
| Display tier | WPF clients (Polling, Duplex) | Handles user interface and input |
| Business tier | ChatService + managers | Contains application logic and state |
| Data tier | In-memory state (no database) | Assignment requirement for in-memory state |
| Async/await | Task-based WCF operations | Prevents UI thread blocking during network calls |
| Task vs Thread | Task for async operations | Avoids thread blocking for network I/O |
| UI responsiveness | Dispatcher marshaling | Keeps GUI responsive during async operations |
| One-way operations | `[OperationContract(IsOneWay = true)]` | For fire-and-forget operations where response not needed |
| Duplex communication | WCF duplex callbacks | Enables server-to-client notifications |
| Callback vs async | Separate concepts distinguished | Callbacks are remote notifications, async is execution model |
| Concurrency | Multiple simultaneous clients | Server handles concurrent requests |
| Thread safety | ReaderWriterLockSlim | Protects shared mutable state |

## COMP3008 Lecture 4 Concept Mapping

| Lecture Concept | Project Implementation | Why Applied |
|---|---|---|
| Operation classification | Sync/async/one-way/callback table | Explicit classification prevents ad-hoc decisions |
| Async decision rule | Use async only when long-running | Avoids unnecessary complexity for short operations |
| Async propagation | await through UI → Business → Data | Prevents async-to-blocking conversion |
| Thread-safety requirement | Components with shared state are thread-safe | Distributed components receive concurrent calls |
| Race condition analysis | Concurrent client testing | Identifies and prevents race conditions |
| Synchronization strategy | Business-logic level locking | Controlled concurrency, not maximum locking |
| One-way vs async distinction | Contract vs execution model | Different concepts used independently |
| Remote callback architecture | Duplex channel for progress | Server-initiated notifications |
| GUI thread safety | Dispatcher.Invoke for UI updates | Prevents cross-thread GUI access |
| UI responsiveness NFR | Async operations for long tasks | Explicit non-functional requirement |
| Async error handling | try-catch around awaited operations | Handles network failures and timeouts |
| Cancellation | CancellationToken consideration | For long-running operations |
| Architecture decision process | Component analysis template | Systematic approach to design decisions |
| Concurrency testing | Simultaneous client tests | Verifies thread-safety under load |
| Architecture trade-offs | Benefits vs costs of distribution | Justifies architectural decisions |
| Implementation priority | 17-step implementation order | Ensures systematic development |
| Design principle hierarchy | Decision trees for operation type | Prevents over-engineering |

# Working Document Instructions

This document is the authoritative implementation reference for the project.

When implementing or modifying the system, follow these rules unless the assignment requirements explicitly require otherwise.

## 1. Source of Truth

The implementation must remain consistent with:

1. Assignment requirements
2. WCF service contracts
3. This `working.md`
4. Existing working server/polling-client implementation

When these conflict, the assignment requirements take priority.

Do not introduce architectural changes merely for convenience.

## 2. Preserve the Existing Working Implementation

The existing server and polling client are considered the working baseline.

When implementing the Duplex Client:

- Do not rewrite working server functionality unnecessarily.
- Do not refactor the polling client unless required for shared functionality.
- Do not change existing service behaviour merely to make the Duplex Client easier to implement.
- Reuse existing contracts, DTOs, validation rules, configuration and models where appropriate.
- Any server-side modification required for duplex communication must preserve polling-client compatibility.

The Duplex Client must be an additional client implementation, not a replacement for the polling client.

## 3. Duplex Client Must Not Poll

The Duplex Client must receive real-time updates exclusively through WCF callbacks.

The Duplex Client must contain:

- No `DispatcherTimer` used for server polling
- No polling thread
- No periodic `Task.Delay` polling loop
- No `GetMessages()` loop
- No `RefreshMessages()` timer
- No periodic `GetChannels()` calls
- No refresh button used as the core real-time update mechanism

The server must proactively notify the Duplex Client through `IChatCallback`.

A manual refresh may exist only for non-real-time functionality if explicitly justified and must not replace callback-based updates.

## 4. Duplex Connection

The Duplex Client must establish its WCF connection using:

- `InstanceContext`
- `DuplexChannelFactory<T>`
- `NetTcpBinding`
- The configured duplex endpoint

Conceptually:

```text
WPF Client
    |
    | InstanceContext
    v
ChatCallbackHandler
    |
    v
DuplexChannelFactory<IDuplexChatService>
    |
    | NetTcpBinding
    v
Chat Server
```

The callback implementation must be registered when the duplex channel is established.

## 5. Callback Contract

`IChatCallback` is a remote callback contract.

It must contain only server-to-client notification operations.

Example:

```csharp
[ServiceContract]
public interface IChatCallback
{
    [OperationContract(IsOneWay = true)]
    void ChannelListUpdated(...);

    [OperationContract(IsOneWay = true)]
    void MemberListUpdated(...);

    [OperationContract(IsOneWay = true)]
    void MessageReceived(...);

    [OperationContract(IsOneWay = true)]
    void PrivateMessageReceived(...);

    [OperationContract(IsOneWay = true)]
    void FileShared(...);

    [OperationContract(IsOneWay = true)]
    void UserDisconnected(...);
}
```

The exact operation signatures must match the existing project contracts and assignment requirements.

Do not add callback operations merely because they appear useful.

## 6. One-Way Operations

Do not equate one-way communication with asynchronous execution.

```text
One-way:
"I do not require a response."

Async:
"I do not want the caller to block while waiting."
```

A one-way WCF operation can still experience communication failure.

Use `[OperationContract(IsOneWay = true)]` primarily for notifications where the sender does not require an operation response.

Do not convert normal request/response operations into one-way operations merely to make them appear asynchronous.

Operations such as:

* SignIn
* GetChannels
* CreateChannel
* JoinChannel
* LeaveChannel
* SendMessage
* SendPrivateMessage
* Upload/ShareFile
* Download/GetFile

must remain request/response where the caller requires confirmation or a result.

## 7. Async/Await

Do not make every WCF operation asynchronous by default.

Use `Task`/`async`/`await` where asynchronous execution provides a real benefit, particularly for:

* Network operations performed from the WPF UI
* File transfers
* Long-running operations
* Potentially expensive processing
* Operations where blocking the GUI would reduce responsiveness

Prefer:

```csharp
var result = await SomeAsyncOperation();
```

Avoid:

```csharp
var result = SomeAsyncOperation().Result;
```

and:

```csharp
SomeAsyncOperation().Wait();
```

in WPF application code.

Do not introduce fake asynchronous code such as wrapping every synchronous WCF call in `Task.Run()` without an architectural reason.

## 8. WPF Thread Affinity

WPF controls belong to the WPF UI thread.

The following must not directly modify WPF controls when executing on a WCF callback thread:

```csharp
void MessageReceived(...)
{
    messageList.Items.Add(...); // Incorrect
}
```

Instead:

```csharp
void MessageReceived(...)
{
    Application.Current.Dispatcher.BeginInvoke(() =>
    {
        messageList.Items.Add(...);
    });
}
```

Use `BeginInvoke`/appropriate asynchronous Dispatcher mechanisms where possible so that callback processing does not unnecessarily block the callback thread.

Callback handlers should perform minimal work before dispatching to the UI.

## 9. Callback Thread Safety

A WCF callback is a remote operation.

Do not assume that callbacks:

* Execute on the WPF UI thread
* Execute sequentially
* Execute on the same thread
* Complete immediately

The callback implementation must therefore be thread-safe.

Do not perform expensive processing inside the callback method.

Preferred flow:

```text
WCF Callback Thread
        |
        v
Receive notification
        |
        v
Prepare minimal data
        |
        v
Dispatcher.BeginInvoke
        |
        v
WPF UI Thread
```

## 10. CallbackManager Locking

Never hold a synchronization lock while performing a remote WCF callback.

Avoid:

```csharp
lock (_callbacks)
{
    callback.MessageReceived(message);
}
```

A remote callback may block, fail, timeout or disconnect.

Prefer:

```text
Acquire lock
    ↓
Copy required callback references
    ↓
Release lock
    ↓
Invoke callbacks
    ↓
Handle failures
    ↓
Remove failed callbacks
```

The synchronization mechanism protects callback registration state, not the duration of remote network communication.

## 11. Dead Callback Handling

A failed callback must not:

* Crash the server
* Block other clients
* Prevent other callbacks
* Corrupt callback state
* Hold a synchronization lock indefinitely

Callback invocation must handle communication failures.

Conceptually:

```csharp
try
{
    callback.MessageReceived(message);
}
catch (CommunicationException)
{
    // Mark/remove failed callback
}
catch (TimeoutException)
{
    // Mark/remove failed callback
}
```

The exact exception-handling strategy must remain consistent with the WCF configuration.

After a callback failure, the server must clean up the associated client state as appropriate.

## 12. Server State Consistency

The server is the authoritative owner of:

* Users
* Current channel membership
* Pending polling messages
* Private messages
* File metadata
* File contents
* Duplex callback registrations

Clients must never become authoritative sources of shared state.

Client-side state is only a representation/cache of server state.

## 13. Atomic State Changes

Operations that modify related pieces of server state must maintain the required invariants.

For example, leaving a channel may require:

```text
Remove user from channel
        +
Clear CurrentChannel
        +
Update callback/pending state
        +
Notify remaining members
```

Do not update only one part of the state and assume another operation will eventually correct it.

Where multiple managers participate in a single logical operation, the implementation must ensure that the resulting state is consistent.

## 14. Do Not Hold Locks During Network Operations

Server-side locks must protect shared in-memory state only.

Do not perform:

* WCF callbacks
* File transfers
* Long-running processing
* Blocking I/O

while holding a synchronization lock unless there is a specific and documented reason.

The preferred pattern is:

```text
Acquire lock
    ↓
Read/update shared state
    ↓
Copy required data
    ↓
Release lock
    ↓
Perform external/expensive operation
```

## 15. Message Semantics

The server remains responsible for message routing.

### Public Messages

A public message is delivered to users who are members of the channel at the time the message is sent.

Do not:

* Store permanent message history
* Deliver old messages to users joining later
* Allow clients to determine recipients
* Send messages directly between clients

### Private Messages

A private message is delivered only to the intended recipient.

Before sending:

```text
Sender exists?
    ↓
Recipient exists?
    ↓
Both users signed in?
    ↓
Both users currently in same channel?
    ↓
Deliver message
```

The server performs the authoritative validation.

## 16. Polling Message Queues

The polling client may use pending message queues because it obtains updates through request/response polling.

The Duplex Client must not depend on polling queues to receive real-time events.

The server may internally share routing logic between polling and duplex clients, but the delivery mechanisms remain distinct:

```text
Polling:
Server → pending state → GetMessages() → Polling Client

Duplex:
Server → CallbackManager → IChatCallback → Duplex Client
```

## 17. Shared Server Logic

Polling and Duplex clients must use the same authoritative business rules.

Do not implement separate versions of:

* User validation
* Channel membership rules
* Private-message rules
* File validation
* File access rules
* Server state management

The communication mechanism may differ, but business rules must remain centralized on the server.

## 18. File Sharing

All file validation must be performed server-side.

Allowed:

```text
.png
.jpg
.jpeg
.gif
.bmp
.txt
```

Maximum size:

```text
2,097,152 bytes
```

The client may perform preliminary validation for user experience, but this is not authoritative.

Never send a local filesystem path to another client.

Correct model:

```text
Client A
   |
   | file bytes
   v
Server
   |
   | stored file
   v
Client B
```

## 19. File Access Control

A user must only access files belonging to a channel they are currently authorised to access.

The server must verify channel membership before returning protected file data.

Do not rely on the client hiding unauthorized files.

## 20. Configuration

Do not hard-code server addresses, ports or polling intervals inside client implementation code.

Use the existing configuration mechanism:

```text
App.config
    ↓
ConfigurationService
    ↓
Client service
```

Command-line configuration may override configuration-file values according to the existing server design.

## 21. Implementation Style

Do not introduce unnecessary architecture.

Avoid adding:

* Database layers
* Repository patterns
* Dependency injection frameworks
* MVVM frameworks
* API gateways
* Message brokers
* Event buses
* Additional services
* Additional network boundaries

unless the assignment explicitly requires them.

The current architecture is intentionally simple:

```text
WPF Client
    ↓
WCF
    ↓
Chat Server
    ↓
In-Memory State
```

## 22. Duplex Client UI

The Duplex Client should follow the existing Polling Client structure where practical.

Reuse:

* DTOs
* ValidationService
* FileHelperService
* ConfigurationService
* Shared UI resources
* Common UI conventions

However, communication logic must remain separate.

Do not simply copy the polling implementation and leave its polling mechanism enabled.

## 23. Duplex Client Service Separation

The Duplex Client should have a dedicated communication service responsible for:

* Creating the duplex channel
* Managing `InstanceContext`
* Connecting
* Disconnecting
* Calling service operations
* Exposing the callback handler to the UI/application layer
* Handling WCF communication failures

The callback handler should be responsible for receiving server notifications, not for owning the entire application state.

## 24. Connection Lifecycle

The Duplex Client lifecycle should be:

```text
Application Start
      ↓
Sign-In Window
      ↓
User enters ID
      ↓
Create callback handler
      ↓
Create InstanceContext
      ↓
Create DuplexChannelFactory
      ↓
Open channel
      ↓
Sign in
      ↓
Register callback
      ↓
Normal operation
      ↓
Sign out / disconnect
      ↓
Close/abort channel
      ↓
Return to sign-in
```

If communication fails, the client must not assume the channel is still usable.

## 25. Graceful vs Abnormal Disconnect

The implementation must handle both:

### Graceful disconnect

```text
User clicks Sign Out
        ↓
Server SignOut
        ↓
Server cleanup
        ↓
Client closes WCF channel
```

### Abnormal disconnect

```text
Window closed / process killed / network failure
        ↓
WCF communication failure
        ↓
Server detects disconnected client
        ↓
Server cleanup
        ↓
Remaining clients notified
```

The server must not depend exclusively on explicit `SignOut()` calls.

## 26. Testing Requirements

Every feature must be tested individually and under concurrent use.

At minimum test:

* Two clients signing in simultaneously
* Duplicate sign-in
* Simultaneous channel creation
* Simultaneous channel joins
* Simultaneous public messages
* Simultaneous private messages
* Simultaneous file uploads
* Polling and Duplex clients communicating together
* Duplex callback while another operation is running
* Client terminated using X
* Network/channel failure
* Dead callback
* Server continuing after a client failure

## 27. Regression Testing

After modifying the server for Duplex functionality, verify that the Polling Client still passes:

* Sign-in
* Sign-out
* Channel creation
* Channel listing
* Join
* Leave
* Public messages
* Private messages
* File upload
* File download
* Polling updates

A Duplex feature is not considered complete if it breaks the existing polling implementation.

## 28. Documentation Accuracy

The documentation must distinguish between:

```text
Implemented
Planned
Partially implemented
Tested
Untested
Known issue
```

Do not mark a feature `[x]` merely because the architecture has been designed.

For example:

```text
[x] Duplex callback contract implemented
[ ] Duplex callback contract planned
```

must reflect the actual repository state.

The document must not claim that a feature is working unless it has been implemented and tested.

## 29. Before Modifying the Server

Before changing server code for Duplex functionality:

1. Inspect the existing service contracts.
2. Inspect the existing `ChatService`.
3. Inspect the existing callback/state management code.
4. Identify which functionality already exists.
5. Determine the minimum required change.
6. Preserve existing polling behaviour.
7. Implement the smallest compatible change.
8. Build.
9. Test the polling client.
10. Then test the Duplex Client.

Do not rewrite working components based solely on assumptions about their implementation.

## 30. Before Implementing a New Operation

For every new operation, explicitly determine:

```text
Operation:
Purpose:
Caller:
Result required:
Communication model:
Sync / Async:
One-way:
Callback:
Long-running:
Shared state:
Synchronization required:
Failure modes:
Timeout:
Cancellation:
UI update required:
```

Only then implement the operation.

## 31. Definition of Done — Duplex Client

The Duplex Client is considered complete only when:

* [ ] DuplexChannelFactory is working
* [ ] InstanceContext is correctly configured
* [ ] Callback handler is registered
* [ ] Sign-in works
* [ ] Sign-out works
* [ ] Channel creation works
* [ ] Channel listing updates through callbacks
* [ ] Channel membership updates through callbacks
* [ ] Public messages arrive through callbacks
* [ ] Private messages arrive through callbacks
* [ ] File notifications arrive through callbacks
* [ ] File download works
* [ ] No polling mechanism exists
* [ ] No refresh button is required for core updates
* [ ] Callback UI updates use Dispatcher
* [ ] Communication failures are handled
* [ ] Dead callbacks do not crash the server
* [ ] Polling and Duplex clients interoperate
* [ ] Three or more concurrent clients have been tested
* [ ] Abnormal client termination has been tested

## 32. Definition of Done — Final System

The final system must demonstrate:

```text
                    ┌──────────────────┐
                    │   Chat Server    │
                    │                  │
                    │ Authoritative    │
                    │ In-Memory State  │
                    └────────┬─────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
            BasicHttp                 NetTcp
                 │                       │
                 ▼                       ▼
       ┌─────────────────┐     ┌─────────────────┐
       │ Polling Client  │     │ Duplex Client   │
       │                 │     │                 │
       │ Request/Response│     │ Server Callback │
       └─────────────────┘     └─────────────────┘
```

Both clients must operate against the same server instance and observe consistent shared state.

The architecture must demonstrate:

* Distributed components
* WCF services
* RPC
* Request/response communication
* Polling
* Duplex communication
* Remote callbacks
* Asynchronous execution where appropriate
* WPF Dispatcher usage
* Concurrent clients
* Thread-safe shared state
* Network failure handling
* Controlled synchronization
* Server-authoritative state

## Design Decisions

### DD-001 — Server Owns Authoritative State

**Decision:**
All users, channels, memberships and shared-file metadata are owned by the server.

**Reason:**
Clients must not maintain authoritative distributed state.

**Alternatives Considered:**
- Client-owned state
- Shared database

**Decision Rationale:**
The assignment requires a central server architecture and does not require persistent storage.

**Status:** Accepted

### DD-002 — Polling vs Duplex Communication

**Decision:**
Section A uses polling (request/response). Section C uses duplex callbacks.

**Reason:**
Assignment requirements explicitly require both communication models.

**Alternatives Considered:**
- Polling for all clients
- Duplex for all clients

**Decision Rationale:**
Demonstrates understanding of both pull-based and push-based communication patterns.

**Status:** Accepted

### DD-003 — In-Memory State Only

**Decision:**
Server state is held in memory; no database is used.

**Reason:**
Assignment explicitly requires in-memory state and does not require persistence.

**Alternatives Considered:**
- SQLite database
- File-based persistence

**Decision Rationale:**
Database would add unnecessary complexity and violate assignment constraints.

**Status:** Accepted

## Operation Classification

| Operation | Client | Communication | Execution | Reason |
|---|---|---|---|---|
| SignIn | Both | RPC | Sync | Immediate authentication result required |
| SignOut | Both | RPC | Sync | Immediate cleanup confirmation |
| GetChannels | Polling | RPC | Async from UI | Prevent UI blocking |
| CreateChannel | Both | RPC | Sync/Async | Validation result required |
| JoinChannel | Both | RPC | Sync/Async | Membership confirmation required |
| LeaveChannel | Both | RPC | Sync/Async | Leave confirmation required |
| GetChannelMembers | Polling | RPC | Async from UI | Prevent UI blocking |
| SendMessage | Both | RPC | Async from UI | Network operation |
| SendPrivateMessage | Both | RPC | Async from UI | Network operation |
| ShareFile | Both | RPC | Async | File transfer |
| GetFile | Both | RPC | Async | File transfer |
| GetChannelFiles | Polling | RPC | Async from UI | Prevent UI blocking |
| GetPendingMessages | Polling | RPC | Async from UI | Prevent UI blocking |
| GetPendingPrivateMessages | Polling | RPC | Async from UI | Prevent UI blocking |
| RegisterCallback | Duplex | One-way RPC | One-way | Callback registration |
| UnregisterCallback | Duplex | One-way RPC | One-way | Callback cleanup |
| ChannelListUpdated | Duplex | Callback | Callback | Server notification |
| MemberListUpdated | Duplex | Callback | Callback | Server notification |
| MessageReceived | Duplex | Callback | Callback | Server notification |
| PrivateMessageReceived | Duplex | Callback | Callback | Server notification |
| FileShared | Duplex | Callback | Callback | Server notification |
| UserDisconnected | Duplex | Callback | Callback | Server notification |

## Test Results

### Server and Polling Client Tests

#### Concurrent Duplicate Sign-In

**Result:** PASS

**Clients:** 5

**Scenario:** All clients attempted to sign in using the same user ID.

**Observed:** Exactly one client was accepted.

**Server State:** Consistent.

**Notes:** Server correctly enforces unique user ID constraint.

#### Channel Creation and Join

**Result:** PASS

**Clients:** 3

**Scenario:** Clients created channels and joined simultaneously.

**Observed:** All channels created successfully, membership correctly tracked.

**Server State:** Consistent.

#### Public Messaging

**Result:** PASS

**Clients:** 3

**Scenario:** Multiple clients sent messages to the same channel.

**Observed:** All current members received messages.

**Server State:** Consistent.

**Notes:** Message history not retained as required.

#### Private Messaging

**Result:** PASS

**Clients:** 2

**Scenario:** Users sent private messages within same channel.

**Observed:** Only intended recipient received messages.

**Server State:** Consistent.

**Notes:** Server validates same-channel membership.

#### File Sharing

**Result:** PASS

**Clients:** 2

**Scenario:** User uploaded valid image file.

**Observed:** File appeared to all channel members.

**Server State:** Consistent.

**Notes:** Server enforces 2 MB limit and extension whitelist.

#### File Validation

**Result:** PASS

**Scenario:** Attempted upload of unsupported file type.

**Observed:** Server rejected with appropriate error message.

**Server State:** Consistent.

#### Client Disconnect

**Result:** PASS

**Scenario:** Client terminated using X button.

**Observed:** Server detected disconnection, removed user from channel, released user ID.

**Server State:** Consistent.

**Notes:** Server continues serving other clients.

### Duplex Client Tests

**Status:** Functional — UI parity complete

The duplex client is implemented and functionally correct (callbacks fire, UI updates via Dispatcher). Formal UI parity tests have been completed, matching the visual structure of the polling client completely.

### Integration Tests

**Status:** Pending

Integration testing with both polling and duplex clients simultaneously is planned after duplex UI polish is complete. Scenarios to cover:
- Sign in from both clients, same channel
- Message sent by polling client → received by duplex client via callback
- Message sent by duplex client → received by polling client via next poll
- Private messages between polling and duplex users
- File share visible on both clients
- Kill duplex client with X → server cleanup observed by polling client


### Sign Out Flow & Empty States

**Sign Out:** The global footer now features a clickable username which reveals a 'Sign Out' popup. Clicking this button redirects the user back to the login screen and cleans up session state.

**Empty States:** Channel views now feature an 'empty state' message ('No shared files yet') in the Shared Files sidebar when no files have been uploaded.

### Private Messaging Follow-up (2026-08-23)

The private-message enhancement identified in `implementation_plan.md` is now implemented in both the polling and duplex clients.

- PM history is retained in memory for each recipient while the current user remains signed in. Closing and reopening a PM window restores that conversation history.
- A PM window closes when its recipient is no longer present in the current channel. The polling client determines this from its member refresh; the duplex client responds to both member-list updates and the user-disconnected callback.
- Leaving a channel closes all open PM windows because private messages are only valid for members of the same channel. History remains available if the user rejoins during the same signed-in session.
- Signing out closes all PM windows and clears all locally retained PM history so it cannot be displayed to the next signed-in account.
- PM windows are closed from a snapshot of the open-window collection, preventing the `Closing` event from modifying that collection during enumeration.
- `SendPrivateMessage` now returns an acknowledgement from the server. A client adds its own message to local history and clears the input only after the server accepts it; rejected sends display a concise warning instead of a phantom local message.

Manual verification remains pending and will be performed through the WPF clients. Test the following scenarios for both client types: close/reopen a PM window, have the other user leave the channel while a PM is open, leave and rejoin the channel, and sign out then sign in as a different user.

### UI Enhancement Follow-up (2026-08-23)

- Channel file-message cards now carry the server file ID and can be clicked to download/open the file through the existing authorized file path.
- Polling and duplex PM windows use grouped message metadata: consecutive messages from the same sender in the same local minute hide repeated sender/time metadata, including the current user.
- PM windows include a right-side shared-files panel and local file-selection preview. The UI intentionally labels these entries as pending server support; a private-file WCF operation and routing are not implemented yet.
