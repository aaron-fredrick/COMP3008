# Real-Time Chat Application - Project Plan

## Project Overview
A real-time chat application built with .NET Framework, WCF, and WPF supporting both polling and duplex communication patterns. This is a distributed application demonstrating client-server architecture with RPC-based communication.

## Distributed Computing Architecture Goals

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

## COMP3008 Distributed Systems Context

This project is a distributed application implemented using a client-server architecture with two RPC communication mechanisms.

### Distributed Application Characteristics

The application is distributed across multiple processes (server, polling client, duplex client) that communicate via WCF RPC over network endpoints. This demonstrates distributed systems concepts of coordinating computation across independent processes using IPC mechanisms (Lecture 1). The server and clients can run on the same physical machine during development but remain separate processes communicating through network endpoints.

- `Chat.Server` runs as a separate server process.
- `Chat.Client.Polling` runs as a separate client process.
- `Chat.Client.Duplex` runs as a separate client process.
- Communication occurs over network endpoints rather than direct method calls within the same process.

The application performs useful work across multiple processes:
- Clients provide the user interface and initiate operations.
- The server maintains shared application state and performs business logic.
- The server routes messages, manages users/channels, validates files, and maintains sessions.
- The duplex server additionally invokes client callbacks for real-time events.

### Client-Server vs Peer-to-Peer

The project is not peer-to-peer - clients never communicate directly. All communication flows through the central server, which maintains authoritative shared state.

```text
Correct:

Client A ─────► Server ◄───── Client B
```

The duplex callback from server to client does not make the system peer-to-peer because the communication still originates from the server.

### Inter-Process Communication (IPC)

Because the client and server execute as separate processes, they cannot directly access each other's memory or invoke internal methods directly. Communication occurs through WCF service calls. The client does not directly access UserManager, ChannelManager, MessageRouter, or FileHandler - it communicates through the WCF service contract.

### Component Decomposition

The architectural components are:

**Client Components**
```text
Chat.Client.Polling
Chat.Client.Duplex
```
Responsibilities:
- Presentation
- User interaction
- Client-side validation
- Client-side file operations
- WCF communication

**Server Service Component**
```text
Chat.Server
    └── ChatService
```
Responsibilities:
- Expose service operations
- Coordinate server-side application logic
- Maintain authoritative state
- Communicate with clients

**Internal Server Objects**
```text
UserManager
ChannelManager
MessageRouter
CallbackManager
FileHandler
```
These are internal implementation objects rather than independently exposed distributed components.

### Component Interfaces

The distributed component boundary is defined by WCF service contracts:
```text
IChatService
IDuplexChatService
IChatCallback
```

Service contracts provide the public interface of the distributed service. Internal interfaces/classes are implementation details and are not directly exposed to clients.

### WCF Endpoint Design

| Component Access | Contract             | Binding            | Address                                       | Purpose                   |
| ---------------- | -------------------- | ------------------ | --------------------------------------------- | ------------------------- |
| Polling client   | `IChatService`       | `BasicHttpBinding` | `http://localhost:8080/ChatService/Polling`   | Request/response RPC      |
| Duplex client    | `IDuplexChatService` | `NetTcpBinding`    | `net.tcp://localhost:8081/ChatService/Duplex` | Duplex RPC                |
| Server callback  | `IChatCallback`      | Duplex channel     | Client callback channel                       | Server-to-client callback |

```text
Endpoint = Address + Binding + Contract
```

### RPC Architecture

The application uses Windows Communication Foundation (WCF) as its RPC framework. From the client's perspective, operations like `service.SignIn()` appear similar to local method calls, but they cross process/network boundaries with different failure characteristics (Lecture 1). The architecture has two layers: RPC calls from client to server via WCF, then local procedure calls within the server (e.g., `_userManager.SignIn()`).

WCF provides the RPC abstraction, handling serialization, marshaling, and transport. Remote calls have multiple possible failure points (network, server availability, serialization) that local calls do not. A communication exception does not necessarily prove the server-side operation did not occur.

### Communication Models

The project demonstrates two RPC communication models:

**Polling (Request/Response RPC)**: The client repeatedly asks the server for updates using operations like `GetMessages()`, `GetPrivateMessages()`, `GetChannelMembers()`, and `GetChannels()`.

**Duplex (Bidirectional RPC)**: The client establishes a callback channel and the server can notify the client when events occur, eliminating the need for periodic polling (Section C requirement). The callback is still communication across the process/network boundary, not a direct local event.

### RPC Failure Considerations

Because operations are remote, they have properties that local method calls do not: network connectivity can fail, the server may be unavailable, remote calls have higher latency, and requests can fail after being sent. The implementation includes disconnect handling and service connection management.

### Serialization and Service Contracts

The WCF contracts in `Chat.Contracts` define the interface and data exchanged between client and server. When the client calls `service.SendMessage(message)`, the `Message` object is serialized for transmission across the network and deserialized on the server side. The client and server therefore have independent object instances and do not share memory. WCF handles the required argument/message marshaling and serialization infrastructure.

### Why Use RPC Instead of Raw TCP/UDP?

TCP provides reliable byte-stream transport but does not define application-level operations. A custom TCP implementation would need to define message boundaries, request/response format, serialization, operation identification, error handling, connection management, and application protocol rules. WCF provides a higher-level RPC abstraction for application/service-level remote operations (Lecture 1).

### Runtime Communication Model

```text
Client Application
      │
      │ Chat.Contracts
      ▼
ChannelFactory
      │
      ▼
WCF Channel
      │
      │ Network
      ▼
WCF Endpoint
      │
      ▼
ChatService
      │
      ▼
Internal Server Objects
```

The client does not link directly to the server implementation. The client references the service contract and creates a WCF communication channel. The actual communication occurs at runtime over the configured endpoint.

### Distributed Failure Model

Remote calls must be treated differently from local calls. Potential failures include:
- Server unavailable
- Endpoint unavailable
- Network disconnected
- Timeout
- Communication channel fault
- Callback channel failure
- Server-side exception

Implementation requirements:
- Handle CommunicationException
- Handle TimeoutException
- Detect faulted WCF channels
- Close/abort channels correctly
- Prevent callback failures from crashing the client
- Provide user-visible connection error messages

### Data Serialization

Data crossing the distributed boundary must be serializable. Relevant objects: `User`, `Channel`, `Message`, `SharedFile`.

```text
Client Object
      ↓
WCF Serialization
      ↓
Network
      ↓
WCF Deserialization
      ↓
Server Object
```

Object references are not shared across the process/network boundary.

### State Placement Rationale

State is maintained by the server because:
- Multiple clients require a consistent view of users/channels
- Message routing requires a central authority
- Channel membership must be coordinated
- Private-message authorization depends on server-side membership
- Files must pass through the server
- Clients cannot safely coordinate authoritative shared state independently

```text
             Server
       Authoritative State
              │
       ┌──────┴──────┐
       │             │
    Client A      Client B
       │             │
       └──────┬──────┘
              │
          Client C
```

### Distributed Architecture Principles

**1. Service Boundary**: Clients interact with server functionality through WCF contracts.

**2. Encapsulation**: Server implementation objects remain inside the server process.

**3. No Shared Memory**: Clients and server exchange serialized data rather than memory references.

**4. Explicit Communication**: Remote operations occur through WCF rather than direct object calls.

**5. Failure Awareness**: Network operations are assumed to be fallible.

**6. Component Cohesion**: Each distributed component has a clear responsibility.

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

## Technology Stack
- **Framework**: .NET Framework 4.8
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Communication / RPC**: WCF (Windows Communication Foundation)
- **Language**: C#
- **Transport bindings**:
  - BasicHttpBinding for polling
  - NetTcpBinding for duplex communication

## Solution Architecture

### Project Structure
```
COMP3008.sln
├── Chat.Contracts/              # Shared class library for WCF contracts (server + clients)
│   ├── ServiceContracts/       # WCF service interfaces
│   ├── DataContracts/          # Data models
│   ├── CallbackContracts/      # Duplex callback interfaces
│   └── SharedTypes/            # Enums, constants, helpers
│
├── Chat.Client.Shared/         # Shared class library for client logic
│   ├── Services/               # Shared client services (Validation, FileHelper, Configuration)
│   ├── Models/                 # Client-side models (UI-specific)
│   ├── Styles/                 # Resource dictionaries (colors, sizing)
│   ├── Resources/              # Resource dictionaries (converters, master resources)
│   └── Converters/             # Value converters
│
├── Chat.Server/                # Console application
│   ├── Services/               # WCF service implementations (implements Chat.Contracts)
│   ├── StateManagement/        # In-memory state management
│   ├── FileStorage/            # File handling and validation
│   └── Hosting/                # Service host configuration
│
├── Chat.Client.Polling/        # WPF application (✅ Complete)
│   ├── Views/                  # XAML views (MainWindow, ChannelListView, ConversationView, PrivateMessageView)
│   └── Services/               # Polling-specific WCF client + polling logic
│
└── Chat.Client.Duplex/         # WPF application (🔄 In Progress)
    ├── Views/                  # XAML views (to be implemented)
    └── Services/               # Duplex-specific WCF client + callback handling
```

## Core Components

### 1. Chat.Contracts (Shared Library)
**Purpose**: Central repository for WCF service contracts, data contracts, and shared types.

**Service Contracts**:
- `IChatService` - Main service contract for polling client
- `IDuplexChatService` - Duplex service contract with callbacks
- `IChatCallback` - Callback interface for duplex client

**Data Contracts**:
- `User` - User information (ID, current channel)
- `Channel` - Channel information (name, members)
- `Message` - Message data (sender, content, timestamp, type)
  - **Timestamp**: DateTime (UTC) - Required for all messages
- `FileInfo` - Shared file metadata (name, type, size, uploader, timestamp)
- `PrivateConversation` - Private message tracking (other user ID, started at)

**Enums**:
- `MessageType` (Public, Private, System)
- `FileType` (Png, Jpg, Jpeg, Gif, Bmp, Txt, Unsupported)
  - **Allowed types**: .png, .jpg, .jpeg, .gif, .bmp, .txt (per assignment)
  - **Size limit**: 2 MB per file (per assignment)

### 2. Chat.Client.Shared (Shared Library)
**Purpose**: Shared client-side logic including services and UI resources.

**Shared Services** (communication-agnostic):
- `ValidationService` - Validates usernames, channel names, messages, and files
- `FileHelperService` - File operations (size, extension, reading, saving)
- `ConfigurationService` - Server configuration management

**Shared Models**:
- `MessageDisplayModel` - UI-specific message presentation model

**Shared UI Resources**:
- `Colors.xaml` - Dark theme color brushes
- `Sizing.xaml` - Font sizes, padding, margins, corner radius
- `Converters.xaml` - FileSizeConverter
- `SharedResources.xaml` - Master resource dictionary

### 3. Chat.Server
**Purpose**: Self-hosted WCF service managing all shared state.

**Key Components**:
- `ChatService` - Implements IChatService and IDuplexChatService
- `UserManager` - Handles user sign-in/out, ID uniqueness
- `ChannelManager` - Channel creation, membership management
- `MessageRouter` - Message distribution (public and private)
- `FileHandler` - File validation, storage, retrieval
- `CallbackManager` - Tracks and invokes duplex callbacks

**State Management**:
- All state held in server memory (per assignment requirement)
- In-memory dictionaries for fast access
- Thread-safe operations with appropriate locking
- No database required for server
- Nothing survives server restart

### 4. Chat.Client.Polling
**Purpose**: WPF client using polling mechanism for updates (✅ Complete).

**Key Components**:
- `MainWindow` - Sign-in view with validation
- `ChannelListView` - Channel list and creation UI
- `ConversationView` - Channel conversation with message display
- `PrivateMessageView` - Private messaging UI
- `ChatServiceClient` - WCF client wrapper
- `DispatcherTimer` - Background polling for updates

**Polling Strategy**:
- Poll interval: 2 seconds (configurable)
- Efficient timestamp-based polling for delta updates
- Sequential polling: channels → messages → files → members

**Features Implemented**:
- Sign-in with duplicate username detection
- Channel creation, joining, and leaving
- Public messaging with timestamp-based history
- Private messaging with multiple conversation windows
- File sharing (2MB limit, specific extensions)
- File download and opening
- Sign-out functionality
- Shared UI resources (colors, sizing, converters)
- Shared services (ValidationService, FileHelperService)

### 5. Chat.Client.Duplex
**Purpose**: WPF client using duplex callbacks for real-time updates (🔄 In Progress).

**Key Components** (to be implemented):
- `DuplexServiceClient` - WCF duplex client wrapper
- `ChatCallbackHandler` - Implements IChatCallback
- `Dispatcher marshaling` - Thread-safe UI updates from callbacks
- Views (MainWindow, ChannelListView, ConversationView, PrivateMessageView)

**Callback Handling** (to be implemented):
- Server pushes updates immediately via WCF callbacks
- Thread-safe UI updates via Dispatcher
- Graceful disconnection detection

**Shared Components** (to be integrated):
- Shared services (ValidationService, FileHelperService, ConfigurationService)
- Shared UI resources (colors, sizing, converters)

## Enhanced Features

### Color Theme System
**Design**: System-aware theming with user override capability.

**Implementation**:
```csharp
public enum ThemeMode
{
    System,      // Follow Windows setting
    Light,       // Force light theme
    Dark         // Force dark theme
}

public class AppTheme
{
    public ThemeMode Mode { get; set; }
    public Color AccentColor { get; set; }
    // Additional theme properties
}
```

**Theme Resources**:
- Resource dictionaries for Light/Dark themes
- Dynamic resource loading based on theme mode
- System theme detection via `SystemParameters.WindowsGlassBrush`

**Supported Themes**:
1. **Light Theme** (default)
   - Background: #FFFFFF
   - Foreground: #000000
   - Accent: #0078D4 (Windows blue)

2. **Dark Theme**
   - Background: #1E1E1E
   - Foreground: #FFFFFF
   - Accent: #60CDFF (lighter blue)

3. **System Default**
   - Detects Windows setting at startup
   - Listens for system theme changes

### Settings Feature
**Purpose**: Allow users to customize application behavior.

**Settings Categories**:

1. **Appearance**
   - Theme mode (System/Light/Dark)
   - Accent color picker
   - Font size (Small/Medium/Large)

2. **Connection**
   - Server address (default: localhost)
   - Server port (default: 8080)
   - Polling interval (for polling client)

3. **Notifications**
   - Sound on new message
   - Flash window on private message
   - Show notifications when minimized

4. **Chat**
   - Timestamp format
   - Show join/leave messages
   - Auto-scroll to newest message

**Implementation**:
- Settings stored in `user.config` (standard .NET settings)
- Settings window accessible from main view
- Real-time theme updates without restart

### Client-Side Local Storage (Optional Enhancement)
**Purpose**: Store message history (both channel and private) locally for each client.

**Note**: This is an optional enhancement not required by the assignment. The assignment requires no message history on the server, but client-side caching can improve user experience.

**Schema** (if implemented):
```sql
CREATE TABLE ChannelConversations (
    channel_name TEXT PRIMARY KEY,
    last_visited TIMESTAMP NOT NULL
);

CREATE TABLE ChannelMessages (
    channel_name TEXT NOT NULL,
    sender_id TEXT NOT NULL,
    sent_at TIMESTAMP NOT NULL,
    message_content TEXT NOT NULL,
    PRIMARY KEY (channel_name, sender_id, sent_at),
    FOREIGN KEY (channel_name) REFERENCES ChannelConversations(channel_name)
);

CREATE INDEX idx_channel_messages_time ON ChannelMessages(sent_at);

CREATE TABLE PrivateConversations (
    other_user_id TEXT PRIMARY KEY,
    started_at TIMESTAMP NOT NULL,
    last_message_at TIMESTAMP
);

CREATE TABLE PrivateMessages (
    conversation_id TEXT NOT NULL,
    sender_id TEXT NOT NULL,
    sent_at TIMESTAMP NOT NULL,
    message_content TEXT NOT NULL,
    is_outgoing BOOLEAN NOT NULL,
    PRIMARY KEY (conversation_id, sender_id, sent_at),
    FOREIGN KEY (conversation_id) REFERENCES PrivateConversations(other_user_id)
);

CREATE INDEX idx_private_messages_time ON PrivateMessages(sent_at);
```

**Storage Strategy** (if implemented):
- Each client maintains its own SQLite database
- Channel messages cached locally for offline viewing
- Private conversations stored per-user basis (no channel association)
- Messages persisted immediately upon receipt/sending
- History loaded when opening conversation window
- Optional: Clear history on sign-out (user setting)
- Optional: Limit history to last N days (configurable)

### Server Configuration
**Address and Port**:
- Configurable via App.config or command-line arguments
- Default polling endpoint: `http://localhost:8080/ChatService/Polling`
- Default duplex endpoint: `net.tcp://localhost:8081/ChatService/Duplex`
- Binding: `netTcpBinding` for duplex client
- Binding: `basicHttpBinding` for polling client

**Configuration Options**:
- App.config: Set `PollingHost`, `PollingPort`, `DuplexHost`, `DuplexPort` in appSettings
- Command-line: `--polling-host`, `--polling-port`, `--duplex-host`, `--duplex-port`
- Command-line arguments override App.config values

**State Management**:
- All state held in server memory (per assignment requirement)
- No database required for server
- Nothing survives server restart
- In-memory dictionaries for fast access
- Thread-safe operations with appropriate locking

### Message Display Features

**Timestamp Display**:
- All messages display timestamps (both channel and private)
- Format: "HH:MM" for messages from today
- Format: "Yesterday HH:MM" for messages from yesterday
- Format: "DD/MM/YYYY HH:MM" for older messages
- User-configurable timestamp format in settings

**WhatsApp-Style Date Segmentation**:
- Automatic date separators between messages from different days
- Date headers displayed as: "Today", "Yesterday", or "DD/MM/YYYY"
- Grouped by day with visual separation (colored background or divider)
- Implemented via message grouping logic in ViewModels
- Applied to both channel conversations and private chats

**Active DM List**:
- Sidebar or panel showing all active private conversations
- Displays: other user's name, last message preview, last message time
- Sorted by most recent activity
- Click to open or switch to that conversation
- Unread indicator for conversations with new messages
- Close button to end conversation (removes from list, history preserved)
- Collapsible panel to save screen space

### Server-Side SQLite Schema
**NOTE**: Server-side SQLite is NOT required per assignment. The assignment explicitly states:
> All state is held in server memory. No database is required, and nothing needs to survive a server restart.

Server implementation uses in-memory state only. Any SQLite implementation would be client-side only for optional local message history caching.

## Shared Contract Implementation

### Interface Segregation
Following the "I" prefix convention for interfaces:

```csharp
// Service Contracts
public interface IChatService { }
public interface IDuplexChatService { }
public interface IChatCallback { }

// Data Contract Interfaces (if needed)
public interface IUser { }
public interface IChannel { }

// Service Internal Interfaces
public interface IUserManager { }
public interface IChannelManager { }
public interface IMessageRouter { }
```

### Implementation Classes
```csharp
public class ChatService : IChatService, IDuplexChatService { }
public class UserManager : IUserManager { }
public class ChannelManager : IChannelManager { }
public class MessageRouter : IMessageRouter { }
```

## Implementation Roadmap

### Phase 1: Foundation ✅ COMPLETED
- [x] Create solution structure with 5 projects
- [x] Define all service contracts in Chat.Contracts
- [x] Define data contracts and shared types
- [x] Set up basic WCF hosting in Chat.Server
- [x] Add configurable endpoints via App.config and command-line

### Phase 2: Server Core ✅ COMPLETED
- [x] Implement UserManager with ID uniqueness
- [x] Implement ChannelManager with CRUD operations
- [x] Implement MessageRouter for public messages
- [x] Implement FileHandler with validation
- [x] Implement CallbackManager for duplex callbacks
- [x] Implement ChatService WCF service
- [x] Add thread-safety with ReaderWriterLockSlim
- [x] Add server integration tests

### Phase 3: Polling Client ✅ COMPLETED
- [x] Implement sign-in functionality with validation
- [x] Implement channel list view
- [x] Implement channel creation
- [x] Implement conversation view
- [x] Implement private messaging
- [x] Implement file sharing
- [x] Implement sign-out functionality
- [x] Integrate shared services
- [x] Integrate shared UI resources
- [ ] Review polling implementation for UI-thread blocking
- [ ] Convert suitable network operations to Task-based asynchronous calls
- [ ] Ensure polling does not block the WPF Dispatcher
- [ ] Add communication timeout handling
- [ ] Add communication failure handling

### Phase 4: Shared Components ✅ COMPLETED
- [x] Create ValidationService for common validation rules
- [x] Create FileHelperService for file operations
- [x] Create ConfigurationService for server settings
- [x] Create shared UI resources (Colors.xaml, Sizing.xaml, Converters.xaml)
- [x] Create SharedResources.xaml master dictionary
- [x] Refactor polling client to use shared components

### Phase 5: Duplex Client 🔄 IN PROGRESS
- [ ] Define duplex callback instance
- [ ] Create `InstanceContext` for callback handler
- [ ] Create `DuplexChannelFactory<IDuplexChatService>`
- [ ] Create duplex service channel
- [ ] Register callback with server
- [ ] Handle callback events
- [ ] Marshal callback events to WPF Dispatcher
- [ ] Build duplex client UI (MainWindow, ChannelListView, ConversationView, PrivateMessageView)
- [ ] Integrate shared services in duplex client
- [ ] Integrate shared UI resources in duplex client
- [ ] Test duplex client sign-in/sign-out
- [ ] Test duplex client channel management
- [ ] Test duplex client public messaging with callbacks
- [ ] Test duplex client private messaging with callbacks
- [ ] Test duplex client file sharing
- [ ] Test callback behaviour during client disconnection
- [ ] Test callback behaviour during server shutdown

### Phase 6: Concurrency, Asynchrony & Testing ⏳ PENDING
- [ ] Test with 3+ concurrent clients
- [ ] Test polling and duplex clients simultaneously
- [ ] Verify server thread safety
- [ ] Verify concurrent channel operations
- [ ] Verify concurrent message operations
- [ ] Verify concurrent private messaging
- [ ] Verify file operations under concurrent clients
- [ ] Verify polling does not freeze the UI
- [ ] Verify asynchronous operations correctly handle exceptions
- [ ] Verify WCF timeout behaviour
- [ ] Test server unavailable scenarios
- [ ] Test client disconnection scenarios
- [ ] Test duplex callback disconnection scenarios
- [ ] Measure polling overhead compared with duplex communication

### Phase 7: Documentation & Submission ⏳ PENDING
- [ ] Add XML documentation comments
- [ ] Create user guide
- [ ] Prepare demonstration script
- [ ] Complete declaration of originality
- [ ] Create submission zip
- [ ] Verify submission by re-downloading

## Technical Considerations

### Thread Safety
- Server: Use `ReaderWriterLockSlim` for shared state
- Duplex client: Use `Dispatcher.Invoke` for UI updates
- Polling client: DispatcherTimer runs on UI thread, no cross-thread issues

### Error Handling
- Custom fault exceptions for WCF errors
- User-friendly error messages in UI
- Graceful degradation on network issues

### Performance
- Efficient timestamp-based delta polling
- Configurable polling interval
- In-memory server state for fast access
- Callback-based updates for the duplex client
- File size limited to 2 MB by assignment requirements

### Security
- File type validation (whitelist: .png, .jpg, .jpeg, .gif, .bmp, .txt)
- File size enforcement (2MB limit)
- Input sanitization for user IDs and channel names

### Testing Requirements
- Test with 1-5 concurrent clients (per assignment requirement)
- At least 3 clients must run during demonstration
- Test both polling and duplex clients simultaneously
- Test client-server communication (not peer-to-peer)
- All traffic passes through server, clients never connect directly

## Marking Alignment

### Section A (14 Marks) - Polling Client
- Sign-in with duplicate detection ✓
- Channel list with auto-update ✓
- Channel creation ✓
- Conversation with real-time messages ✓
- Private messaging ✓
- File sharing ✓
- Sign-out ✓

### Section B (10 Marks) - Server
- User management ✓
- Channel management ✓
- Message distribution ✓
- Private messaging ✓
- File handling ✓

### Section C (8 Marks) - Duplex Client
- Duplex contract and callbacks ✓
- No polling/refresh ✓
- Thread safety ✓
- Disconnection handling ✓

### Bonus Features (Not Marked)
- Color theme system
- Settings feature
- SQLite session recovery
- Enhanced UI/UX

## Development Guidelines

### Coding Standards
- Follow C# naming conventions (PascalCase for public members)
- Use XML documentation for public APIs
- Keep methods small and focused (single responsibility)
- Use meaningful variable and method names

### Architecture Principles
- DRY: Share code between clients via Chat.Contracts
- SOLID: Interface segregation, dependency injection where appropriate
- Separation of concerns: UI, business logic, data access

### Version Control
- Feature branches for each phase
- Frequent commits with descriptive messages
- Code review before merging to main

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| WCF duplex complexity | High | Start with polling, add duplex later |
| Thread safety issues | High | Extensive testing with concurrent clients |
| SQLite integration issues | Medium | Keep it optional, fallback to in-memory |
| Time constraints | High | Prioritize core features over enhancements |
| Theme system complexity | Low | Use existing WPF theming patterns |

## Communication Model Comparison

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

## Project Components Mapped to Multi-Tier Architecture

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

## Operation Classification

Every major service/API operation should be classified during implementation planning.

| Operation Type | Expected Behaviour | Example |
|---|---|---|
| Synchronous | Caller waits for result | GetChannels() |
| Asynchronous | Caller continues while operation executes | SendMessageAsync() |
| One-way | Caller sends command without requiring result | (potential future use) |
| Remote callback | Server sends notification to client | NotifyMessageReceived() |
| Local GUI dispatch | Worker thread updates GUI through UI thread | UpdateMessageList() |

## Asynchronous Communication Decision Rule

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

## Thread-Safety as Design Requirement

Distributed components must not assume that clients will serialize access to them. A service may receive multiple simultaneous client calls. Therefore: Components containing shared mutable state must be designed to be externally thread-safe. The design should minimise shared mutable state wherever possible. Prefer local variables, immutable data, stateless services, isolated state per request, database transactions where appropriate, and controlled synchronization around genuinely shared state. Avoid unnecessary global/member state.

## Architecture Decision Process

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

## Concurrency Testing

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

## Architecture Trade-Offs

The architecture explicitly recognises that distribution introduces costs.

**Benefits:** Modularity, lower coupling, scalability, load balancing, fault isolation, multiple clients, specialised services, better separation of concerns.

**Costs:** Network latency, network failure, serialization/deserialization, timeout handling, concurrency, thread synchronization, distributed state, more complex debugging, more complicated deployment, more difficult failure diagnosis.

The architecture distributes components only where the benefits justify these costs.

## Implementation Priority

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

## Design Principle Hierarchy

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

## Final Architecture Checklist

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

## Success Criteria
The implementation targets all assessed assignment requirements and is structured to support the full available mark allocation.

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
| Thread safety | ReaderWriterLockSlim | Protects shared mutable state |

## COMP3008 Lecture 4 Alignment

| Concept | Demonstrated By | Why Applied |
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

