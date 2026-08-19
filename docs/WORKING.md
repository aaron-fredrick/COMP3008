# COMP3008 Chat Application - Working Notes

## Project Status

**Current Phase**: Phase 2 - Server Implementation (Complete)

**Last Updated**: August 19, 2026

## Completed Work

### Phase 1: Foundation ✅
- Created solution structure with 5 projects targeting .NET Framework 4.8
- Defined all WCF service contracts in Chat.Contracts
- Defined data contracts (User, Channel, Message, SharedFile)
- Defined shared types (MessageType, FileType)
- Set up basic WPF shell for both client applications
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

### Thread Safety
- Used ReaderWriterLockSlim for all shared state
- Write locks for modifications (sign-in/out, join/leave, message routing)
- Read locks for queries (get channels, get members, get callbacks)
- Callbacks invoked with try-catch to prevent failures from crashing server

### Message Routing
- Public messages distributed to all channel members at send time
- Sender also receives their own message for consistency
- Private messages only between users in same channel
- Pending queues emptied after successful retrieval (polling)

## Pending Work

### Phase 3: Polling Client
- [ ] Implement sign-in functionality
- [ ] Implement channel list view with polling
- [ ] Implement channel creation
- [ ] Implement conversation view with message polling
- [ ] Implement member list polling
- [ ] Add background polling thread
- [ ] Implement private messaging UI
- [ ] Implement file sharing UI
- [ ] Implement file download and opening
- [ ] Add sign-out functionality

### Phase 4: Advanced Features
- [ ] Implement color theme system
- [ ] Implement settings feature
- [ ] Implement WhatsApp-style date segmentation
- [ ] Implement active DM list sidebar

### Phase 5: Client-Side SQLite (Optional)
- [ ] Design SQLite schema for local message history
- [ ] Implement SQLite persistence layer
- [ ] Add message caching on client side
- [ ] Implement history loading for conversations

### Phase 6: Duplex Client
- [ ] Implement IDuplexChatService on server (already done)
- [ ] Implement IChatCallback interface (already defined)
- [ ] Create DuplexServiceClient
- [ ] Implement callback registration
- [ ] Implement thread-safe UI updates via Dispatcher
- [ ] Add disconnection handling

### Phase 7: Testing & Polish
- [ ] Test with 3+ concurrent clients
- [ ] Test both clients against same server
- [ ] Test file sharing between clients
- [ ] Test private messaging between channel members
- [ ] Performance optimization
- [ ] Code review and cleanup

## Known Issues

None currently.

## Technical Notes

### WCF Service Hosting
- ServiceBehavior set to InstanceContextMode.Single for singleton service
- BasicHttpBinding for polling (request/response)
- NetTcpBinding for duplex (callbacks)
- Endpoints configurable via App.config or command-line

### File Storage
- Files stored in Chat.Server/StoredFiles directory
- File key format: `{channelName}_{fileName}`
- Files do not survive server restart (per assignment)
- File data included in SharedFile for transfer

### Message Timestamps
- All messages use DateTime.UtcNow
- Server generates timestamp, not client
- No message IDs currently (could add for debugging)

### User Sessions
- UserSession contains: UserId, CurrentChannel, PendingChannelMessages, PendingPrivateMessages, Callback
- Pending queues are Queue<Message>
- Callback is IChatCallback for duplex clients

## Git Commits

- **55e8638**: Add .gitattributes for proper line ending handling
- **c3a6d40**: Phase 2: Server implementation
- **5181cd4**: Phase 1: Initial solution setup

## Next Steps

1. Begin Phase 3: Polling Client implementation
2. Start with sign-in functionality
3. Implement channel list view
4. Add background polling thread
5. Test against running server

## Resources

- [README.md](../README.md) - Project overview and getting started
- [PROJECT_PLAN.md](PROJECT_PLAN.md) - Detailed project plan
- [Part A.md](Part%20A.md) - Assignment requirements
