# COMP3008 Chat Application - Working Notes

## Project Status

**Current Phase**: Phase 5 - Duplex Client Implementation (In Progress)

**Last Updated**: August 20, 2026

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
Address: http://localhost:8080/ChatService/Polling
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

The Chat Application follows a simplified multi-tier architecture based on the distributed computing concepts introduced in COMP3008. The system does not require all four conceptual tiers. Instead, it combines the presentation and display responsibilities within the WPF clients while keeping the server-side business logic separate.

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

```csharp
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

```csharp
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

```csharp
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

```csharp
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
 | await
 v
Presentation/API
 |
 | await
 v
Business Tier
 |
 | await
 v
Data Tier
 |
 | await
 v
Database / External Service
```

This prevents a situation where an asynchronous operation is immediately converted back into a blocking operation at another layer. Avoid patterns such as `var result = SomeAsyncMethod().Result;` or `SomeAsyncMethod().Wait();` in GUI/application code unless there is a specific architectural reason. Prefer `var result = await SomeAsyncMethod();`.

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

```csharp
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

```csharp
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

## COMP3008 Lecture 3 Success Criteria

### Distributed Architecture

- [x] Clear separation between client presentation and server business logic
- [x] Distributed service boundary exposed through WCF contracts
- [x] No direct client access to server implementation objects
- [x] Server maintains authoritative shared state
- [x] Architecture avoids unnecessary additional tiers

### Asynchronous Communication

- [ ] WPF UI remains responsive during remote operations
- [ ] Suitable network operations use Task-based asynchronous execution
- [ ] `await` is used rather than blocking the UI thread where appropriate
- [ ] Async communication exceptions are handled correctly
- [ ] Duplex callbacks are safely marshalled to the WPF Dispatcher

### Concurrency

- [x] Multiple clients can communicate concurrently
- [x] Server shared state remains thread-safe
- [ ] Polling and duplex clients can operate simultaneously
- [x] Client disconnections do not corrupt server state
- [x] Server shutdown/disconnection is handled gracefully

