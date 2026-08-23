# Distributed Chat Application

> An independent educational project exploring distributed computing,
> WCF, WPF, polling, duplex communication, and client-server architecture.

## About

This project was developed independently as a self-directed learning
exercise to explore concepts covered in the COMP3008 Distributed
Computing module.

I was not enrolled in this module and this project was not developed
for, submitted to, or assessed as part of a Curtin University
assignment.

The project was created as an additional technical challenge to
develop a practical understanding of distributed computing concepts.

## Educational Reference

The project was developed with reference to educational material
provided by a friend, including module documentation, lecture
material, laboratory documentation, and assignment specifications.

Those materials are **not included in this repository** and remain
the property of their respective copyright holders.

## Overview

This chat application supports both polling and duplex communication patterns:
- **Polling Client**: Periodically requests updates from the server
- **Duplex Client**: Receives real-time updates via WCF callbacks
- **Server**: Self-hosted WCF service managing all application state in memory

## Technology Stack

- **Framework**: .NET Framework 4.8
- **Language**: C#
- **Communication**: WCF (Windows Communication Foundation)
- **UI**: WPF (Windows Presentation Foundation)
- **Architecture**: Client-Server with shared contracts

## Project Structure

```
COMP3008/
├── Chat.Contracts/          # WCF service contracts, data contracts, shared types
├── Chat.Client.Shared/      # Shared client logic (Services, Models, UI Resources)
├── Chat.Server/             # WCF server console application
├── Chat.Client.Polling/     # WPF polling client (✅ Complete)
├── Chat.Client.Duplex/      # WPF duplex client (🔄 In Progress)
└── docs/                    # Documentation
```

## Features

### Server
- User authentication (username-based, no passwords)
- Channel management (create, join, leave)
- Public messaging within channels
- Private messaging between channel members
- File sharing (2MB limit, specific extensions)
- In-memory state management (no database)
- Thread-safe operations
- Configurable endpoints via App.config or command-line arguments

### Polling Client
- Request/response communication pattern
- Periodic polling for messages and updates
- Channel management UI
- Private conversation windows
- File upload/download

### Duplex Client
- Real-time event delivery via WCF callbacks
- Instant message notifications
- Channel member change notifications
- File sharing notifications

## File Restrictions

Allowed file types:
- Images: `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`
- Text: `.txt`

Maximum file size: **2 MB**

## Getting Started

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- Windows OS

### Building the Solution

**Using build scripts (recommended):**
```powershell
# Build Chat.Server (default)
.\build.ps1

# Build specific project
.\build.ps1 Chat.Contracts

# Build with Release configuration
.\build.ps1 Chat.Server Release

# Or use batch file
build.bat
build.bat Chat.Contracts
build.bat Chat.Server Release
```

**Using Visual Studio:**
1. Open `COMP3008.slnx` in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Ensure all projects compile successfully

**Using MSBuild directly:**
```powershell
msbuild Chat.Server\Chat.Server.csproj /p:Configuration=Debug
```

### Running the Server

**Using defaults (from App.config):**
```bash
Chat.Server.exe
```

**Using command-line arguments:**
```bash
Chat.Server.exe --host localhost --polling-port 8080 --duplex-port 8081
```

**View help:**
```bash
Chat.Server.exe --help
```

**Default endpoints:**
- Polling: `http://localhost:8080/ChatService/Polling`
- Duplex: `net.tcp://localhost:8081/ChatService/Duplex`

### Configuration

Server endpoints can be configured in `Chat.Server/App.config`:

```xml
<appSettings>
  <add key="Host" value="localhost" />
  <add key="PollingPort" value="8080" />
  <add key="DuplexPort" value="8081" />
</appSettings>
```

Command-line arguments override App.config values.

## Architecture

### Shared Contracts (`Chat.Contracts`)

- **ServiceContracts**: `IChatService`, `IDuplexChatService`
- **CallbackContracts**: `IChatCallback`
- **DataContracts**: `User`, `Channel`, `Message`, `SharedFile`
- **SharedTypes**: `MessageType`, `FileType`

### Server Components

- **UserManager**: User sessions, authentication, pending message queues
- **ChannelManager**: Channel creation, membership management
- **MessageRouter**: Public and private message routing
- **CallbackManager**: Duplex callback registration and invocation
- **FileHandler**: File validation, storage, retrieval
- **ChatService**: WCF service implementation
- **ChatServiceHost**: WCF service hosting

### Communication Patterns

**Polling:**
```
Client → Request → Server → Response → Client
```

**Duplex:**
```
Client → Register Callback → Server
Server → Push Event → Client (via callback)
```

## Important Notes

- All server state is held in memory (no database persistence)
- Users only see messages from the time they join a channel
- Private messages only allowed between users in the same channel
- Files travel through the server (Client A → Server → Client B)
- Server restart clears all state and files
- Username uniqueness applies only to currently signed-in users

## Testing Requirements

- Support 1-5 concurrent clients
- Demonstration requires at least 3 clients
- Test both polling and duplex clients simultaneously
- Verify file sharing between clients
- Verify private messaging between channel members

## Assignment Part A Feature & Marks Alignment

| Feature | Section | Marks | Status | Implementation Location |
|---------|---------|-------|--------|-------------------------|
| **Section A: Client Functionality (14 Marks)** | | | | |
| Sign-in | A | 2m | ✅ Complete | `Chat.Client.Polling/Views/SignInView.xaml.cs`, `ChatService.SignIn()` |
| Channel list | A | 2m | ✅ Complete | `Chat.Client.Polling/Views/ChannelListView.xaml.cs`, `ChatService.GetChannels()` |
| Channel creation | A | 2m | ✅ Complete | `ChatService.CreateChannel()`, ChannelListView UI |
| Conversation | A | 2m | ✅ Complete | `Chat.Client.Polling/Views/ConversationView.xaml.cs`, `ChatService.SendMessage()` |
| Private messages | A | 2m | ✅ Complete | `ChatService.SendPrivateMessage()`, PrivateMessageView |
| File sharing | A | 3m | ✅ Complete | `ChatService.ShareFile()`, `FileHandler.StoreFile()` |
| Sign-out | A | 1m | ✅ Complete | `ChatService.SignOut()`, UserManager cleanup |
| **Section B: Server Functionality (10 Marks)** | | | | |
| User management | B | 2m | ✅ Complete | `UserManager.cs`, session tracking, authentication |
| Channel management | B | 2m | ✅ Complete | `ChannelManager.cs`, create/join/leave channels |
| Message distribution | B | 2m | ✅ Complete | `MessageRouter.cs`, public message routing |
| Private messaging | B | 2m | ✅ Complete | `MessageRouter.cs`, private message routing |
| File handling | B | 2m | ✅ Complete | `FileHandler.cs`, validation, storage, retrieval |
| **Section C: Duplex Client (8 Marks)** | | | | |
| Duplex contract | C | 2m | ✅ Complete | `IDuplexChatService.cs`, `IChatCallback.cs` |
| Push-based updates | C | 2m | ✅ Complete | `CallbackManager.cs`, callback registration |
| Real-time lists | C | 2m | ✅ Complete | Duplex callback notifications for members/channels |
| Real-time messaging/files | C | 2m | ✅ Complete | Duplex callbacks for messages and file shares |
| **Additional / Bonus Features (Beyond Part A)** | | | | |
| Configurable bounded message history queue | - | - | ✅ Complete | `ChannelManager.cs`, maxMessages parameter, eviction logging |
| Dark mode visual theme | - | - | ✅ Complete | `Colors.xaml`, `CustomTitleBar.xaml`, theme resources |
| User account popup menu in footer | - | - | ✅ Complete | `ApplicationFooter.xaml`, user menu |
| Shared files empty state placeholder | - | - | ✅ Complete | ConversationView "No shared files yet" UI |
| Unit test suite | - | - | ✅ Complete | `Chat.Server.Tests` project |

**Total Part A Marks: 32/32 (100%)**

## Documentation

- [Project Plan](docs/PROJECT_PLAN.md) - Detailed project plan and architecture
- [Part A Requirements](docs/Part%20A.pdf) - Assignment requirements
- [Working Notes](docs/WORKING.md) - Development notes and progress

## License

This project is licensed under the
**Personal Educational Project License**.
See [LICENSE](LICENSE) for the full terms.

The source is available for educational inspection and learning.
It is not intended to be an open-source project or a codebase for
direct reuse or submission as another person's work.
