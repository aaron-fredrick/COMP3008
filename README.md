# Distributed Real-Time Chat Application

> Educational distributed chat application built with **C#**, **.NET Framework 4.8**, **WCF**, and **WPF**, implementing both HTTP polling and `NetTcpBinding` duplex push communication.

## Context

This repository is an educational project for studying distributed systems, concurrent server state, WCF communication patterns, asynchronous clients and WPF UI threading. The `docs/` directory contains conceptual assignment, laboratory, lecture and architecture documentation; original protected course teaching materials are not reproduced beyond the retained assignment reference material.

---

## Assignment at a glance

The application combines one authoritative WCF server with two independent WPF clients:

```text
                         ┌─────────────────────────┐
                         │       Chat.Server        │
                         │  Self-hosted WCF server  │
                         │  authoritative state     │
                         └────────────┬────────────┘
                                      │
                   ┌──────────────────┴──────────────────┐
                   │                                     │
          BasicHttpBinding                         NetTcpBinding
             HTTP / 9000                             TCP / 8081
                   │                                     │
          ┌────────▼────────┐                   ┌────────▼────────┐
          │ Polling Client  │                   │  Duplex Client   │
          │      WPF        │                   │       WPF        │
          └─────────────────┘                   └─────────────────┘
```

Both clients use the same server and therefore share the same users, channels, messages and files. The Polling client periodically asks for changes; the Duplex client registers a callback and receives core updates pushed by the server.

### Assessment coverage

The Part A requirements are grouped in the repository as follows:

| Assignment area | Requirements | Where implemented | Primary course relationship |
|---|---|---|---|
| Client functionality | A-F01–A-F21 | `Chat.Client.Polling`, shared WPF code | Lab 1; Labs 2–3; Lab 6; Lectures 1–4 |
| Architecture | A-ARC01–A-ARC16 | `Chat.Contracts`, `Chat.Server`, both clients | Labs 1–3, 6; Lectures 1–5 |
| Polling | A-POL01–A-POL08 | `Chat.Client.Polling` | Lab 6; Lecture 4 |
| Duplex / callbacks | A-DPX01–A-DPX12 | `Chat.Contracts`, `Chat.Server`, `Chat.Client.Duplex` | Lab 6; Lectures 4–5 |
| Chat / channels | A-CHAT01–A-CHAT22 | `ChatService`, `UserManager`, `ChannelManager`, `MessageRouter`, clients | Labs 2–3, 6; Lectures 2–5 |
| File sharing | A-FILE01–A-FILE11 | `FileHandler`, WCF operations, client file handling | Labs 2–3 concepts; Lectures 2–3, 5 |
| Concurrency / synchronization | A-CON* | WCF service behaviour, state managers, callback isolation, WPF Dispatcher | Lab 6; Lectures 3–5 |

For the detailed **requirement → implementation → lab → lecture** mapping, see [`docs/assignment-mapping.md`](docs/assignment-mapping.md).

### Feature overview

| Feature | Polling | Duplex | Shared/server |
|---|:---:|:---:|:---:|
| Sign in / sign out | ✓ | ✓ | ✓ |
| Channel list / creation / membership | ✓ | ✓ | ✓ |
| Public channel messages | ✓ | ✓ | ✓ |
| Private conversations | ✓ | ✓ | ✓ |
| File sharing / retrieval | ✓ | ✓ | ✓ |
| Automatic membership updates | ✓ | ✓ | ✓ |
| User joined / left system messages | ✓ | ✓ | ✓ |
| Chat export | ✓ | ✓ | ✓ |
| Concurrent server state | — | — | ✓ |
| Disconnect cleanup | — | ✓ | ✓ |

---

## Architecture

The solution is split by responsibility rather than by duplicating the entire application for each client:

```text
COMP3008.slnx
│
├── src/Chat.Contracts
│     WCF service contracts, callback contracts and data contracts
│
├── src/Chat.Server
│     Self-hosted WCF service + authoritative state + file storage
│
├── src/Chat.Client.Shared
│     Shared client models, UI helpers, themes and export functionality
│
├── src/Chat.Client.Polling
│     WPF client using periodic request/response polling
│
└── src/Chat.Client.Duplex
      WPF client using server-pushed duplex callbacks
```

### Contracts and transport

| Client | Contract | Binding | Endpoint | Update model |
|---|---|---|---|---|
| Polling | `IChatService` | `BasicHttpBinding` | `http://localhost:9000/ChatService/Polling` | Periodic pull |
| Duplex | `IDuplexChatService` + `IChatCallback` | `NetTcpBinding` | `net.tcp://localhost:8081/ChatService/Duplex` | Server push |

The server is configured as a single WCF service instance with concurrent request handling. Shared state is protected in the state-management layer and around membership transitions. Duplex callback failures are isolated so a disconnected client does not take down the service or prevent other clients from being notified.

### Server state

The server is authoritative for:

- signed-in users and their current channel
- channels and membership
- public/private message routing
- file metadata and authorisation
- callback registrations for duplex users

Message history is not replayed to late channel joiners. File metadata is held by the server while file bytes are stored by the server-side `ShardedFileContentStore` under `StoredFiles`; the server remains the only file-transfer boundary between clients.

---

## Polling vs Duplex

The two clients intentionally demonstrate different distributed communication strategies.

| Concern | Polling client | Duplex client |
|---|---|---|
| Core transport | HTTP / `BasicHttpBinding` | TCP / `NetTcpBinding` |
| Update direction | Client asks server | Server pushes to client callback |
| Background polling | Yes | No for core real-time updates |
| Member updates | Polling response | Callback notification |
| Public messages | Polling response | Callback |
| Private messages | Polling response | Callback |
| File notifications | Polling/state retrieval | Callback metadata + separate retrieval |
| Callback registration | No | Yes |
| Disconnect detection | Normal sign-out/session handling | WCF callback/channel lifecycle + server cleanup |

This distinction is central to the assignment: the Duplex client must not simulate duplex behaviour with a timer or refresh button.

---

## Core features

### Users and channels

Users sign in with a unique ID. The server owns the authoritative session and membership state, enforces at-most-one channel membership, and releases the ID on sign-out/disconnect.

### Public and private messaging

Public messages are routed to current channel members. Private messages are routed only to the named recipient and are permitted only when sender and recipient share a channel. Private conversations have separate WPF windows and can coexist.

### System messages

Membership events are represented as typed conversation items rather than raw strings:

```text
ConversationItemViewModel
    ├── MessageViewModel
    └── SystemMessageViewModel
```

System events such as `Alice joined the channel.` and `Bob left the channel.` use a dedicated WPF template: centered, subtle, timestamped and without sender/file controls. They are kept separate from `MessageType.File` rendering.

### File sharing

Permitted channel files are `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp` and `.txt`, up to 2 MB. `FileHandler` validates and stores the content; file metadata is distributed to authorised channel members. The actual bytes are retrieved using the file ID through the server. A metadata-only duplex notification is therefore intentional and is not itself the file transfer.

### Chat export

`ChatExportService` exports the conversation transcript and available attached files as a ZIP. Normal messages retain their sender; system messages use the normal timestamp format but deliberately have no sender prefix.

Example:

```text
[10:40 AM] Alice: Hello
[10:41 AM] Bob joined the channel.
[10:42 AM] Bob: Hi
[10:43 AM] Alice left the channel.
```

---

## Concurrency and WPF threading

The server is configured for concurrent WCF requests, so shared state cannot rely on sequential execution. The state managers use synchronization appropriate to their shared data, while membership transitions are coordinated in `ChatService`.

Duplex callbacks and background polling do not necessarily execute on the WPF UI thread. Client update paths therefore marshal UI-bound state through the WPF `Dispatcher`:

```text
WCF callback / polling worker
             ↓
       client coordinator
             ↓
        WPF Dispatcher
             ↓
 ObservableCollection / ViewModel
             ↓
            WPF UI
```

The conversation views maintain their item collections incrementally so new messages do not require replacing the entire `ItemsSource`. Bottom-following is conditional: users reading older messages are not forcibly moved to the newest item.

---

## Assignment requirement mapping

The official requirement extraction identifies the following major assessment groups:

### Section A — Client Functionality

Sign-in, channel discovery/creation, channel conversation, private conversations, file sharing, sign-out, automatic polling, required views and controls are implemented primarily in the Polling client and shared client code.

**Related teaching:** Lab 1 for C#/WPF foundations; Labs 2–3 for WCF client/server architecture; Lab 6 and Lecture 4 for background/asynchronous polling.

### Architecture

The solution uses a self-hosted WCF server, shared contracts, two independent WPF clients and a common server state. The Duplex client uses `NetTcpBinding` and a callback contract.

**Related teaching:** Labs 2–3 and Lectures 2–3 for services, contracts, endpoints and multi-tier architecture; Lecture 5 for duplex WCF.

### Polling

The Polling client periodically requests changes on a background execution path and updates the UI from the returned state.

**Related teaching:** Lecture 4 and Lab 6 for polling, asynchronous execution, callbacks and UI coordination.

### Duplex / callbacks

The Duplex client registers `IChatCallback`. `CallbackManager` tracks callback ownership and dispatches server events to the appropriate client. The client callback handler then marshals state changes to WPF.

**Related teaching:** Lecture 5 directly; Lecture 4 and Lab 6 for callbacks, asynchronous execution and thread synchronization.

### Chat and channels

`UserManager`, `ChannelManager`, `MessageRouter` and `ChatService` enforce user, channel, membership and message-routing rules.

**Related teaching:** Labs 2–3 and Lectures 2–3, with asynchronous delivery concepts from Lectures 4–5.

### File sharing

`FileHandler` validates extensions/size, stores content server-side, and controls retrieval by channel/recipient authorisation.

**Related teaching:** WCF service boundaries, data contracts and service-mediated transfer from Labs 2–3 / Lectures 2–3 and 5. There is no retained laboratory in this repository that directly reproduces the complete assignment file-sharing feature, so this README does not claim one.

### Concurrency / synchronization

The concurrent WCF service, synchronized state managers, membership-transition coordination, callback isolation and WPF Dispatcher handoff address the distributed/concurrent execution concerns of the assignment.

**Related teaching:** Lecture 3 for tasks/threads, Lecture 4 for synchronization/callbacks, Lecture 5 for duplex callbacks, and Lab 6 for practical asynchronous client patterns.

For the complete requirement identifiers and per-feature implementation locations, see [`docs/assignment-mapping.md`](docs/assignment-mapping.md).

---

## Documentation and walkthroughs

The repository's documentation is organised so that a marker or developer can move from requirements to theory to implementation:

| Document | Purpose |
|---|---|
| [`docs/assignment-mapping.md`](docs/assignment-mapping.md) | Detailed Part A requirement → implementation → lab/lecture map |
| [`docs/walkthrough.md`](docs/walkthrough.md) | End-to-end code/behaviour walkthrough and demonstration order |
| [`docs/labs/README.md`](docs/labs/README.md) | Conceptual laboratory guide and lab-to-lecture map |
| [`docs/lecs/README.md`](docs/lecs/README.md) | Conceptual lecture guide and assignment relationships |
| [`docs/architecture/c4/`](docs/architecture/c4/) | C4 system/container/component/code diagrams |
| [`docs/ass/`](docs/ass/) | Assignment reference/extraction material retained in the repository |

The walkthrough also includes a debugging table for tracing common failures to the responsible architectural layer.

---

## Project structure

```text
COMP3008/
├── COMP3008.slnx
├── src/
│   ├── Chat.Contracts/
│   ├── Chat.Server/
│   ├── Chat.Client.Shared/
│   ├── Chat.Client.Polling/
│   └── Chat.Client.Duplex/
├── tests/
│   ├── Chat.Server.Tests/
│   └── Chat.Client.Tests/
├── scripts/
├── docs/
│   ├── ass/
│   ├── labs/
│   ├── lecs/
│   ├── architecture/c4/
│   ├── assignment-mapping.md
│   └── walkthrough.md
└── README.md
```

---

## Constraints

- **Server state:** authoritative application state is in memory; restarting the server resets sessions/channels/message state. File bytes use the server's configured file-content store.
- **Message isolation:** late channel joiners do not receive earlier channel messages.
- **Private chat:** sender and recipient must currently share a channel.
- **File restrictions:** `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`, `.txt`; maximum 2 MB.
- **Clients:** Polling and Duplex are separate WPF applications and use the same server.

---

## Build and run

### Prerequisites

- Windows 10/11
- .NET Framework 4.8 Developer/Targeting Pack
- Visual Studio with WPF/.NET desktop tooling or compatible MSBuild tooling
- .NET SDK 8, 9 or 10 for the current CI workflow

### Build

```powershell
dotnet build COMP3008.slnx --configuration Debug
```

Visual Studio/MSBuild is also supported for the legacy .NET Framework/WPF projects.

### Run

Start the server first, then one or more clients:

```powershell
.\scripts\run_server_debug.ps1
.\scripts\run_client_polling_debug.ps1
.\scripts\run_client_duplex_debug.ps1
```

Multiple client instances can be used to demonstrate shared server state and concurrent communication.

---

## Testing

The repository uses MSTest.

```powershell
dotnet build COMP3008.slnx --configuration Debug
dotnet test COMP3008.slnx --configuration Debug
```

Individual projects can be tested with:

```powershell
dotnet test tests\Chat.Server.Tests\Chat.Server.Tests.csproj --configuration Debug
dotnet test tests\Chat.Client.Tests\Chat.Client.Tests.csproj --configuration Debug
```

The CI workflow runs the solution build and test commands on Windows for .NET SDK 8, 9 and 10.

---

## C4 architecture diagrams

Raw C4 diagrams are stored under [`docs/architecture/c4/`](docs/architecture/c4/):

- Level 1 — system context
- Level 2 — containers
- Level 3 — server, polling client, duplex client, shared/contracts components
- Level 4 — code/class-level architecture

---

## License

Distributed under the [Personal Educational Project License](LICENSE).
