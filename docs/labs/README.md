# COMP3008 Laboratory Guide

This directory contains a conceptual guide to the COMP3008 laboratory sequence.

## Source material notice

The original laboratory and teaching documents have been redacted from this repository. They are based on teaching materials provided as part of the COMP3008 coursework and laboratory material at Curtin University.

This repository retains a conceptual overview of the material rather than redistributing the original teaching documents. The descriptions below focus on the programming concepts, architectures, technologies, and implementation patterns demonstrated by the labs.

The summaries are intentionally not a replacement for the original laboratory instructions and do not reproduce the original exercises, source code, or substantial text from the teaching material.

---

## Lab 1 — C# and Desktop Application Foundations

Lab 1 introduces the basic structure of a C# application and the transition from ordinary object-oriented code to a desktop user interface.

### Core concepts

- C# classes, properties, inheritance, and object-oriented modelling.
- Collections such as `List<T>` and simple in-memory data.
- Reusing code across projects through project references.
- Console applications and console-based program flow.
- WPF desktop applications and event-driven UI programming.
- Reading user input, validating/parsing values, and displaying results.
- Separating application/model logic from presentation logic.

### Conceptual structure

```text
C# model / data classes
        |
        +----> Console application
        |
        +----> WPF desktop client
                    |
                    +----> UI events
                    +----> Display / search results
```

The important idea is that the underlying data model does not need to know about the UI. A WPF application can reference ordinary C# classes and use them in response to user events.

### What the lab establishes

This is the foundation for the later distributed examples: define responsibilities in classes and projects first, then introduce communication between those components. The later WCF and Web API material builds on this separation of model, application logic, and presentation.

---

## Labs 2–3 — Multi-Tier Client/Server and WCF

The Labs 2–3 material moves from a local desktop program to a distributed application. The example material associated with these labs uses WCF to expose a service boundary between a client and server-side components.

### Core concepts

- Multi-project and multi-tier architecture.
- Client/server separation.
- Shared service contracts and interfaces.
- WCF service contracts and implementations.
- Service hosting and endpoints.
- `NetTcpBinding` and TCP-based service communication.
- Channel factories and remote method calls.
- Serializable/data-contract types.
- Service faults and structured error handling.
- Separating data access from the service boundary and UI.

### Conceptual structure

```text
WPF Client
    |
    | WCF channel
    v
Service Contract / Interface
    |
    v
Service Implementation / Host
    |
    v
Data / Database Layer
```

The client depends on the contract rather than directly depending on the server's implementation or data storage. This is a central distributed-systems concept: a process communicates through a defined boundary rather than sharing its internal implementation.

### What the labs establish

The material introduces the architectural pattern used again in Lab 6. A database/data layer, service layer, and presentation/client layer can be developed independently, provided their contracts remain compatible.

The repository's `examples/Tutorial-2-3-Solution/` guide documents the corresponding conceptual example.

---

## Lab 4 — ASP.NET Core Web API

Lab 4 changes the communication model from WCF/TCP services to HTTP-based web APIs.

### Core concepts

- ASP.NET Core application hosting.
- Web API controllers.
- HTTP request/response communication.
- Attribute routing and route parameters.
- GET and POST operations.
- JSON request and response data.
- Model binding and serialization/deserialization.
- HTTP status codes such as successful responses and `404 Not Found`.
- A desktop client consuming an HTTP API.
- Process-local/in-memory application state.

### Conceptual structure

```text
WPF / HTTP Client
        |
        | HTTP + JSON
        v
ASP.NET Core Web API
        |
        v
Controller
        |
        v
Application / in-memory data
```

The key change from the WCF material is the communication boundary. Instead of a .NET client invoking a WCF contract through a WCF channel, the client sends HTTP requests to resource-oriented endpoints and interprets HTTP responses.

### What the lab establishes

The lab introduces the basic web-service model used by modern distributed applications: an independently hosted application exposes HTTP endpoints, and clients communicate through a protocol and data format that are not tied to the server's internal implementation.

The repository's `examples/SimpleWebAPI/` guide documents the corresponding conceptual example.

---

## Lab 5 — Application-to-Application Web Communication

The Lab 4 and 5 material continues the web application model by demonstrating communication between separate ASP.NET applications.

### Core concepts

- Multiple independently hosted web applications.
- Separate processes and ports.
- HTTP communication between applications.
- Controllers acting as both web endpoints and HTTP clients.
- Attribute/conventional routing.
- Outbound HTTP requests from server-side application code.
- JSON/text response handling.
- Deserialization and forwarding of results.
- Treating another application as an external dependency.
- Startup order and service availability.

### Conceptual structure

```text
Browser / Client
       |
       v
Web Application A
       |
       | HTTP request
       v
Web Application B
       |
       v
API response
       |
       v
Web Application A
       |
       v
Original client
```

The important distinction is that Application A does not call a controller method in Application B directly. The applications communicate across an HTTP boundary, just as two independently deployed services would.

### What the lab establishes

This extends the Web API concepts from Lab 4 into service-to-service communication. It demonstrates why HTTP endpoints can form useful boundaries between independently hosted applications and why the caller must deal with transport errors, response formats, and service availability.

The repository's `examples/WebApplication1/` guide documents the corresponding conceptual example.

---

## Lab 6 — Delegates, Callbacks, and Asynchronous Programming

Lab 6 returns to the layered service architecture while focusing on asynchronous execution. The example material presents both the older delegate-based asynchronous pattern and the task-based `async`/`await` approach.

### Core concepts

- Reuse of a multi-tier WCF architecture.
- Delegates as references to methods with a defined signature.
- Delegate-based asynchronous invocation.
- `BeginInvoke` / `EndInvoke` and completion callbacks.
- `IAsyncResult` and wait handles.
- `Task<T>` as a representation of asynchronous work.
- `async` and `await`.
- Keeping a WPF UI responsive while work is pending.
- Marshaling completed work back to the UI thread/context.
- Comparing blocking and non-blocking client behaviour.
- Observing exceptions from asynchronous operations.

### Delegate-based flow

```text
WPF Client
    |
    | start delegate asynchronously
    v
Remote / slow operation
    |
    | completion callback
    v
IAsyncResult
    |
    v
EndInvoke()
    |
    v
WPF Dispatcher
    |
    v
UI update
```

The delegate pattern makes the completion mechanism explicit: the caller starts work, supplies a callback, receives an asynchronous result, obtains the return value, and then transfers the result back to the UI thread.

### Task-based flow

```text
WPF event handler
        |
        v
Task<T>
        |
      await
        |
        v
remote / slow operation
        |
        v
continuation after await
        |
        v
UI update
```

The task-based model represents the same general problem with a higher-level abstraction. `await` allows the UI thread to continue processing while the operation is pending and resumes the method when the task completes.

### What the lab establishes

The important distinction is not simply syntax. The lab demonstrates different models for representing and completing asynchronous work. It also establishes a critical desktop-programming rule: long-running or blocking work must not unnecessarily occupy the UI thread, and completed background work must update UI state through the appropriate UI-thread context.

The repository's `examples/DelegateExample/` and `examples/AsyncHandle/` guides document the two corresponding approaches.

---

## Overall progression

The laboratory sequence develops the distributed-programming concepts incrementally:

```text
Lab 1
  C# / OOP / desktop UI
        |
        v
Labs 2–3
  Multi-tier architecture + WCF + client/server communication
        |
        v
Lab 4
  HTTP + ASP.NET Core Web API
        |
        v
Lab 5
  Application-to-application HTTP communication
        |
        v
Lab 6
  Delegates + callbacks + asynchronous client programming
```

The progression moves from **local object-oriented programming**, to **distributed service boundaries**, to **HTTP-based services**, and finally to **asynchronous execution and responsive client applications**.

These concepts are directly relevant to understanding the architecture of the main COMP3008 assignment, although the laboratory examples should be treated as teaching examples rather than as the assignment's production architecture.

## Corresponding example guides

The original example implementations have also been redacted from the repository. Their conceptual guides are available under [`../../examples/`](../../examples/):

| Lab | Example guide |
|---|---|
| Lab 1 | [`Tutorial-1-Solution`](../../examples/Tutorial-1-Solution/README.md) |
| Labs 2–3 | [`Tutorial-2-3-Solution`](../../examples/Tutorial-2-3-Solution/README.md) |
| Lab 4 | [`SimpleWebAPI`](../../examples/SimpleWebAPI/README.md) |
| Lab 5 | [`WebApplication1`](../../examples/WebApplication1/README.md) |
| Lab 6 | [`DelegateExample`](../../examples/DelegateExample/README.md) and [`AsyncHandle`](../../examples/AsyncHandle/README.md) |

## Repository note

The actual teaching documents are intentionally not reproduced in this public repository. Keep any locally retained copies separate from the Git-tracked documentation. This README is intended to preserve the conceptual map of the laboratory material without redistributing the original teaching content.
