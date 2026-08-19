# Real-Time Chat Application - Project Plan

## Project Overview
A real-time chat application built with .NET Framework, WCF, and WPF supporting both polling and duplex communication patterns.

## Technology Stack
- **Framework**: .NET Framework 4.8
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Communication**: WCF (Windows Communication Foundation)
- **Language**: C#

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
│   ├── ViewModels/             # Shared ViewModels (UI logic only)
│   ├── Services/               # Shared client services (storage, grouping, theme)
│   ├── Models/                 # Client-side models (UI-specific)
│   ├── Styles/                 # Resource dictionaries (themes, colors)
│   ├── Controls/               # Custom user controls
│   └── Converters/             # Value converters
│
├── Chat.Server/                # Console application
│   ├── Services/               # WCF service implementations (implements Chat.Contracts)
│   ├── StateManagement/        # In-memory state management
│   ├── FileStorage/            # File handling and validation
│   └── Hosting/                # Service host configuration
│
├── Chat.Client.Polling/        # WPF application
│   ├── Views/                  # XAML views
│   └── Services/               # Polling-specific WCF client + polling logic
│
└── Chat.Client.Duplex/         # WPF application
    ├── Views/                  # XAML views (shared with polling)
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
**Purpose**: Shared client-side logic including ViewModels and services.

**Shared ViewModels** (UI logic, communication-agnostic):
- `MainViewModel` - Coordinates all view models
- `ChannelListViewModel` - Channel list and creation
- `ConversationViewModel` - Channel conversation with date grouping
- `PrivateChatViewModel` - Private message windows with date grouping
- `ActiveDMListViewModel` - Manages active private conversations sidebar
- `SettingsViewModel` - User settings

**Shared Client Services**:
- `LocalMessageStorage` - SQLite for message history (channel + private)
- `MessageGroupingService` - WhatsApp-style date segmentation logic
- `ThemeService` - Color theme management

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
**Purpose**: WPF client using polling mechanism for updates.

**Key Components**:
- `PollingServiceClient` - WCF client wrapper
- `UpdatePoller` - Background thread for periodic updates
- **ViewModels**: All ViewModels imported from Chat.Client.Shared (shared)
- **Services**: LocalMessageStorage, MessageGroupingService, ThemeService from Chat.Client.Shared (shared)

**Polling Strategy**:
- Poll interval: 2-3 seconds (configurable)
- Sequential polling: channels → messages → files → members
- Efficient delta checking to minimize unnecessary updates

**ViewModel Communication**:
- ViewModels accept data via dependency injection or events
- PollingServiceClient fetches data and pushes to ViewModels
- ViewModels are communication-agnostic (don't know about polling vs duplex)

### 5. Chat.Client.Duplex
**Purpose**: WPF client using duplex callbacks for real-time updates.

**Key Components**:
- `DuplexServiceClient` - WCF duplex client wrapper
- `ChatCallbackHandler` - Implements IChatCallback
- `DispatcherService` - Marshals callbacks to UI thread
- **ViewModels**: All ViewModels imported from Chat.Client.Shared (shared)
- **Services**: LocalMessageStorage, MessageGroupingService, ThemeService from Chat.Client.Shared (shared)

**Callback Handling**:
- Server pushes updates immediately
- Thread-safe UI updates via Dispatcher
- Graceful disconnection detection

**ViewModel Communication**:
- ViewModels accept data via dependency injection or events
- ChatCallbackHandler receives callbacks and pushes to ViewModels
- ViewModels are communication-agnostic (don't know about polling vs duplex)

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

### Phase 1: Foundation (Week 1)
- [x] Create solution structure with 5 projects
- [x] Define all service contracts in Chat.Contracts
- [x] Define data contracts and shared types
- [x] Set up basic WCF hosting in Chat.Server
- [x] Create basic WPF shell for both clients
- [x] Add configurable endpoints via App.config and command-line

### Phase 2: Server Core (Week 2)
- [x] Implement UserManager with ID uniqueness
- [x] Implement ChannelManager with CRUD operations
- [x] Implement MessageRouter for public messages
- [x] Implement FileHandler with validation
- [x] Implement CallbackManager for duplex callbacks
- [x] Implement ChatService WCF service
- [x] Add thread-safety with ReaderWriterLockSlim
- [ ] Add basic unit tests for server components

### Phase 3: Polling Client (Week 3)
- [ ] Implement sign-in functionality
- [ ] Implement channel list view with polling
- [ ] Implement channel creation
- [ ] Implement conversation view with message polling
- [ ] Implement member list polling
- [ ] Add background polling thread

### Phase 4: Advanced Features (Week 4)
- [ ] Implement private messaging
- [ ] Implement file sharing UI
- [ ] Implement file download and opening
- [ ] Add sign-out functionality
- [ ] Implement color theme system
- [ ] Implement settings feature

### Phase 5: Session Recovery (Week 5)
- [ ] Design SQLite schema (optional - client-side only)
- [ ] Implement SQLite persistence layer (optional - client-side only)
- [ ] Add periodic checkpointing (optional - client-side only)
- [ ] Implement recovery on server startup (NOT REQUIRED - assignment states no server persistence)
- [ ] Test crash recovery scenarios (optional - client-side only)

### Phase 6: Duplex Client (Week 6)
- [ ] Implement IDuplexChatService on server
- [ ] Implement IChatCallback interface
- [ ] Create DuplexServiceClient
- [ ] Implement callback registration
- [ ] Implement thread-safe UI updates
- [ ] Add disconnection handling

### Phase 7: Testing & Polish (Week 7)
- [ ] Test with 3+ concurrent clients
- [ ] Test both clients against same server
- [ ] Test crash recovery
- [ ] Test theme switching
- [ ] Test all settings
- [ ] Performance optimization
- [ ] Code review and cleanup

### Phase 8: Documentation & Submission (Week 8)
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
- Polling client: Background thread with proper synchronization

### Error Handling
- Custom fault exceptions for WCF errors
- User-friendly error messages in UI
- Graceful degradation on network issues

### Performance
- Efficient polling with delta checking
- File streaming for large files
- Connection pooling for WCF clients

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

## Success Criteria
- All 32 marks achievable from assignment requirements
- Both clients work simultaneously against same server
- Clean, maintainable code architecture
- Enhanced features (themes, settings, recovery) functional
- Successful demonstration with 3+ concurrent clients
