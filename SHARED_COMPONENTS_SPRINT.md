# Chat.Client.Shared — Refined Implementation Plan

## Objective

The polling client is essentially complete and currently uses WPF code-behind successfully.

The duplex client is currently only a placeholder.

The purpose of `Chat.Client.Shared` is therefore **not** to completely redesign the application or introduce a large MVVM architecture.

Instead:

1. Extract genuinely reusable, communication-independent functionality.
2. Keep the existing polling client working.
3. Use the shared components to reduce duplication when implementing the duplex client.
4. Keep WPF code-behind for UI-specific behaviour.
5. Avoid over-engineering anything that does not directly support the assignment.

---

# Current Architecture

## Chat.Contracts

Already implemented and shared by:

- `Chat.Server`
- `Chat.Client.Polling`
- `Chat.Client.Duplex`

Contains:

- Service contracts
- Duplex callback contracts
- Data contracts
- Shared enums/types

Do not duplicate these types in `Chat.Client.Shared`.

---

## Chat.Server

Complete and functional.

Uses:

- WCF
- In-memory state
- Polling endpoint
- Duplex endpoint
- Channel management
- User management
- Message routing
- Callback management
- File validation/storage

Do not modify server behaviour unless required to support the duplex client or fix an actual defect.

---

## Chat.Client.Polling

Essentially complete.

Current functionality:

- Sign in
- Duplicate username handling
- Channel list
- Create channel
- Join channel
- Leave channel
- Automatic polling
- Public messages
- Member list
- Private messaging
- Multiple private-chat windows
- File upload
- File validation
- File download/open
- Sign out
- Disconnect handling

Architecture:

- WPF
- XAML + code-behind
- No MVVM
- Direct WCF service calls
- `DispatcherTimer` handles polling

Do NOT perform a large architectural rewrite.

---

## Chat.Client.Duplex

Currently mostly empty.

Needs to implement:

- Sign in
- Duplex WCF connection
- Callback handler
- Callback → WPF Dispatcher handling
- Channel list
- Create channel
- Join/leave channel
- Public messaging
- Private messaging
- File sharing
- Sign out
- Disconnect handling

---

# Refined Sprint Structure

## Sprint 0 — Baseline and Safety

Before refactoring anything:

### Tasks

1. Build the entire solution.
2. Start the server.
3. Run the existing server integration tests.
4. Run the polling client manually.
5. Verify:
   - Sign in
   - Channel creation
   - Join/leave
   - Public messages
   - Private messages
   - File upload/download
   - Sign out
6. Confirm the current polling client is the baseline.

### Requirement

Do not start refactoring until the baseline passes.

Every subsequent sprint must preserve this behaviour.

---

# Sprint 1 — Shared Foundation

## Goal

Move only communication-independent logic into `Chat.Client.Shared`.

Do NOT create UI controls yet.

Do NOT introduce MVVM yet.

Do NOT rewrite the polling client UI.

---

## 1. ConfigurationService

Create:

`Chat.Client.Shared/Services/ConfigurationService.cs`

Responsibilities:

- Read server configuration
- Store polling endpoint
- Store duplex endpoint
- Store polling interval
- Provide defaults
- Optionally parse command-line arguments if useful

Example conceptual API:

```csharp
public class AppSettings
{
    public string ServerHost { get; set; }
    public int PollingPort { get; set; }
    public int DuplexPort { get; set; }
    public int PollingIntervalMs { get; set; }
}
```

However, do not duplicate configuration models if an existing configuration mechanism already exists.

### Important

The service must NOT:

* Create WCF channels
* Know about WPF
* Know about `Dispatcher`
* Contain polling logic

It should only provide configuration.

---

# 2. ValidationService

Create:

`Chat.Client.Shared/Services/ValidationService.cs`

Move common validation rules here.

At minimum:

### Username

Validate:

* Not null/empty
* Allowed characters
* Maximum length
* Any rules required by the server

### Channel name

Validate:

* Not null/empty
* Maximum length
* Invalid characters

### Message

Validate:

* Not null/empty
* Maximum length if the contract/server imposes one

### File

Validate:

* File extension
* Maximum size: 2 MB
* Supported types:

```text
.png
.jpg
.jpeg
.gif
.bmp
.txt
```

The validation rules should match the server's rules.

Do not duplicate contradictory validation rules between client and server.

The server remains authoritative.

---

# 3. FileHelperService

Create:

`Chat.Client.Shared/Services/FileHelperService.cs`

Only put generic client-side file operations here.

Responsibilities:

* Get file size
* Determine file extension
* Validate supported file
* Read file into `byte[]`
* Save downloaded `byte[]`
* Open downloaded file if appropriate

Do not put WCF calls inside this class.

For example:

```text
FileHelperService
    ↓
reads local file
    ↓
byte[]
    ↓
client service layer sends it to WCF
```

NOT:

```text
FileHelperService
    ↓
WCF
    ↓
Server
```

---

# Sprint 2 — Shared Models

## Goal

Create only models that are actually useful to both clients.

Do NOT create artificial models simply to increase abstraction.

Potential models:

```text
Models/
    AppSettings.cs
    MessageDisplayModel.cs
    ChannelDisplayModel.cs
    UserDisplayModel.cs
    FileDisplayModel.cs
```

Only create a model if it solves a real duplication problem.

---

## Important distinction

Do not duplicate contract objects unnecessarily.

For example, if:

```csharp
Chat.Contracts.DataContracts.Message
```

already contains everything needed by the UI, use it directly.

A `MessageDisplayModel` should only exist if the UI needs additional presentation-specific information.

Example:

```csharp
public class MessageDisplayModel
{
    public Message Message { get; set; }

    public bool IsOwnMessage { get; set; }

    public bool IsPrivateMessage { get; set; }
}
```

Do not wrap every contract type just for architectural purity.

---

# Sprint 3 — Polling Client Refactor

## Goal

Use the new shared services in the existing polling client.

Do NOT change the UI.

Do NOT introduce MVVM.

Do NOT change the user experience.

---

## Refactor only:

### Replace duplicated validation

Before:

```text
Polling MainWindow
    ↓
own validation code
```

After:

```text
Polling MainWindow
    ↓
ValidationService
```

---

### Replace duplicated file handling

Before:

```text
Polling client
    ↓
local file logic
```

After:

```text
Polling client
    ↓
FileHelperService
```

---

### Replace configuration logic

Before:

```text
Polling client
    ↓
hardcoded/config-specific server settings
```

After:

```text
Polling client
    ↓
ConfigurationService
```

---

## Do NOT move:

* XAML event handlers
* `DispatcherTimer`
* Window management
* WPF `Dispatcher`
* UI state
* Window-specific logic

These remain in the polling client.

---

# Sprint 4 — Build Duplex Client

This is now the primary development task.

## Step 1 — Duplex ChannelFactory

Create a duplex WCF connection using the existing contracts.

Conceptually:

```text
InstanceContext
      ↓
CallbackHandler
      ↓
DuplexChannelFactory
      ↓
IDuplexChatService
```

Use the existing:

```text
IDuplexChatService
IChatCallback
```

contracts.

Do not create duplicate contracts.

---

# Step 2 — Callback Handler

Create something similar to:

```text
Chat.Client.Duplex/
    Services/
        DuplexChatClientService.cs
        ChatCallbackHandler.cs
```

The callback handler should:

1. Receive the WCF callback.
2. Convert it into a client event/message.
3. Safely marshal the event onto the WPF Dispatcher.
4. Update the UI.

Important:

WCF callback execution is NOT guaranteed to happen on the WPF UI thread.

Therefore do NOT directly modify WPF controls from the callback.

Use:

```text
WCF callback thread
        ↓
Dispatcher
        ↓
WPF UI
```

---

# Step 3 — Reuse Shared UI-independent functionality

The duplex client should reuse:

* `ValidationService`
* `FileHelperService`
* `ConfigurationService`
* Shared models where actually useful
* Shared styles
* Shared converters

---

# Step 4 — Build Duplex UI

Reuse the same conceptual UI structure as polling:

```text
MainWindow
    ↓
ChannelListView
    ↓
ConversationView
    ↓
PrivateMessageView
```

The UI can remain code-behind based.

Do not create a massive UserControl hierarchy unless there is actual duplication worth eliminating.

---

# Sprint 5 — Shared UI Improvements

Only do this AFTER the duplex client is working.

Evaluate actual duplication between:

```text
Polling UI
Duplex UI
```

Then identify the UI elements that are genuinely identical.

Potential candidates:

* Message bubble
* File item
* Channel list
* Sign-in panel

Only extract a `UserControl` if both clients genuinely need the same control.

Do not create controls simply because the project plan says so.

---

# Sprint 6 — Optional MVVM

This sprint is OPTIONAL.

Do not perform it unless there is enough time after the assignment requirements are completely satisfied.

Possible future structure:

```text
View
 ↓
ViewModel
 ↓
Shared/client service
 ↓
WCF
```

However, the current code-behind implementation is acceptable for this project.

A full MVVM migration is not currently a priority.

---

# What NOT To Do

Do NOT:

* Rewrite the polling client from scratch
* Convert everything to MVVM
* Create unnecessary abstraction layers
* Duplicate WCF contracts
* Duplicate data contracts
* Create repositories
* Add a database
* Add SQLite
* Add local message persistence
* Add dependency injection frameworks
* Add event buses
* Add a generic networking framework
* Add unnecessary design patterns
* Change working server behaviour without a reason
* Build a large generic UI framework

The assignment is a distributed WCF application, not a production enterprise architecture exercise.

---

# Priority Order

The actual priority should now be:

| Priority    | Work                                      |
| ----------- | ----------------------------------------- |
| 🔴 Critical | Duplex WCF client                         |
| 🔴 Critical | Duplex callback handling                  |
| 🔴 Critical | Duplex channel/conversation functionality |
| 🔴 Critical | Duplex private messaging                  |
| 🔴 Critical | Duplex file sharing                       |
| 🟠 High     | Shared validation                         |
| 🟠 High     | Shared file helpers                       |
| 🟠 High     | Shared configuration                      |
| 🟡 Medium   | Shared display models                     |
| 🟢 Low      | Shared WPF controls                       |
| 🟢 Optional | MVVM                                      |

---

# Assignment Safety Requirements

Every refactor must preserve:

* Polling functionality
* Duplex functionality
* WCF contracts
* In-memory server state
* 2 MB file limit
* Supported file extensions
* Channel membership rules
* Private messaging
* Sign-in/sign-out behaviour

---

# Definition of Done

The refactoring is successful when:

## Polling

* Existing polling client still works.
* No regression in existing functionality.
* Shared validation is used.
* Shared file utilities are used where appropriate.
* Shared configuration is used where appropriate.

## Duplex

* User can sign in.
* User can sign out.
* User can create channels.
* User can join channels.
* User can leave channels.
* User can see members.
* User can send public messages.
* User receives public messages through WCF callbacks.
* User can send private messages.
* User receives private messages through callbacks.
* User can share supported files.
* User can download/open supported files.
* Callback events safely update the WPF UI.
* Disconnects are handled gracefully.

## Architecture

```text
                    Chat.Contracts
                         ↑
              ┌──────────┴──────────┐
              │                     │
       Chat.Client.Polling   Chat.Client.Duplex
              │                     │
       Polling Service        Duplex Service
              │                     │
              └────────────┬────────────┘
                           ↓
                 Chat.Client.Shared
                 ┌────────┼─────────┐
                 │        │         │
            Validation   Files   Configuration
```

The shared project should contain **only functionality that is genuinely shared**.

---

# Final Principle

The polling client is the working reference implementation.

Do not destabilize it.

Extract reusable pieces from it incrementally and use those pieces while building the duplex client.

The main objective of this phase is:

**"Reuse the working polling client's communication-independent functionality while implementing the duplex client with the correct WCF callback architecture."**

Not:

**"Rewrite the entire application using MVVM and a large shared UI framework."**
