# COMP3008 Laboratory Guide

This directory contains a **conceptual guide** to the COMP3008 laboratory sequence. It explains the programming, distributed-systems, networking, and asynchronous-programming ideas demonstrated by the labs and connects them to the lecture material.

## Source material notice

The original laboratory documents and teaching examples are Curtin University teaching materials and are intentionally omitted from this repository. They have been removed/redacted to avoid redistributing protected course content.

This repository retains conceptual summaries and navigation only. The descriptions below do not reproduce the original exercises, source code, slide text, or substantial portions of the teaching material, and should not be treated as a replacement for the official course materials.

For the corresponding lecture concepts, see the [Lecture Guide](../lecs/README.md). The lecture guide also explains how these topics lead into the assignment architecture.

---

## Lab 1 — C# and Desktop Application Foundations

Lab 1 establishes the local programming foundation used by the later distributed examples: C# object-oriented programming, collections, project structure, and event-driven WPF applications.

### Core concepts

- C# classes, properties, inheritance, and object-oriented modelling.
- Collections such as `List<T>` and simple in-memory data.
- Reusing code across projects through project references.
- Console applications and ordinary program flow.
- WPF desktop applications and event-driven UI programming.
- Reading user input, validating and parsing values, and displaying results.
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

The underlying data model does not need to know about the UI. A WPF application can reference ordinary C# classes and use them in response to user events.

### Lecture connection

This lab is primarily the practical foundation for **Lecture 1**, where C#, WPF, inter-process communication, and the distinction between local and distributed execution are introduced. The lab is deliberately local; the lectures then explain what changes when the same responsibilities cross a process or network boundary.

See [Lecture 1 — Distributed Systems and RPC](../lecs/README.md#lecture-1---distributed-systems-and-rpc).

### Example connection

The corresponding conceptual example is documented in [`examples/Tutorial-1-Solution`](../../examples/Tutorial-1-Solution/README.md).

---

## Labs 2–3 — Multi-Tier Client/Server and WCF

Labs 2–3 move from a local desktop program to a distributed application. The associated example uses WCF to expose a service boundary between a client and server-side components.

### Core concepts

- Multi-project and multi-tier architecture.
- Client/server separation.
- Shared service contracts and interfaces.
- WCF service contracts and implementations.
- Service hosting and endpoints.
- `NetTcpBinding` and TCP-based service communication.
- `ChannelFactory` and remote method calls.
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

The client depends on the contract rather than directly on the server implementation or data storage. This makes the service boundary explicit and reduces coupling.

### Lecture connection

These labs directly implement the component, service, contract, endpoint, binding, and multi-tier ideas from **Lectures 2 and 3**. Lecture 2 explains why the contract is the boundary; Lecture 3 explains how responsibilities can be separated into tiers and why asynchronous execution matters when a boundary is slow or remote.

See [Lecture 2 — Components, Services, and WCF](../lecs/README.md#lecture-2---components-services-and-wcf) and [Lecture 3 — Multi-Tier Architecture and Task-Based Async](../lecs/README.md#lecture-3---multi-tier-architecture-and-task-based-async).

### Example connection

The corresponding conceptual example is documented in [`examples/Tutorial-2-3-Solution`](../../examples/Tutorial-2-3-Solution/README.md).

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

The key change from WCF is the communication boundary. Instead of a .NET client invoking a service through a WCF channel, the client sends HTTP requests to endpoints and interprets HTTP responses.

### Lecture connection

Lab 4 is the practical counterpart to **Lecture 6**, which covers REST, HTTP methods, resource representations, routing, model binding, and ASP.NET Core Web API. It also reinforces the service-boundary ideas from Lecture 5.

See [Lecture 5 — Duplex Communication and Web Services](../lecs/README.md#lecture-5---duplex-communication-and-web-services) and [Lecture 6 — REST APIs and ASP.NET Core MVC/Web API](../lecs/README.md#lecture-6---rest-apis-and-aspnet-core-mvcweb-api).

### Example connection

The corresponding conceptual example is documented in [`examples/SimpleWebAPI`](../../examples/SimpleWebAPI/README.md).

---

## Lab 5 — Application-to-Application Web Communication

Lab 5 extends the web-service model by demonstrating communication between separate ASP.NET applications.

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

Application A does not call a controller method in Application B directly. The applications communicate across an HTTP boundary, as independently deployed services would.

### Lecture connection

This lab applies the HTTP service and REST concepts from **Lectures 5, 6, and 8**. Lecture 6 provides the API and routing model; Lecture 5 explains service boundaries; Lecture 8 places those interactions in the broader stateless HTTP/web model.

See [Lecture 5](../lecs/README.md#lecture-5---duplex-communication-and-web-services), [Lecture 6](../lecs/README.md#lecture-6---rest-apis-and-aspnet-core-mvcweb-api), and [Lecture 8](../lecs/README.md#lecture-8---web-platforms-and-state).

### Example connection

The corresponding conceptual example is documented in [`examples/WebApplication1`](../../examples/WebApplication1/README.md).

---

## Lab 6 — Delegates, Callbacks, and Asynchronous Programming

Lab 6 returns to the layered service architecture while focusing on asynchronous execution. The examples demonstrate both the older delegate-based asynchronous pattern and the task-based `async`/`await` approach.

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

The important distinction is not just syntax. These are different abstractions for representing and completing asynchronous work. The lab also establishes the desktop-programming rule that long-running work should not unnecessarily occupy the UI thread and that completed background work must update UI state through the appropriate UI-thread context.

### Lecture connection

Lab 6 is directly connected to **Lectures 3 and 4**. Lecture 3 introduces threads, tasks, non-blocking execution, and `async`/`await`; Lecture 4 goes deeper into delegates, callbacks, completion observation, polling, and synchronization.

See [Lecture 3](../lecs/README.md#lecture-3---multi-tier-architecture-and-task-based-async) and [Lecture 4 — Delegates, Callbacks, and Thread Synchronization](../lecs/README.md#lecture-4---delegates-callbacks-and-thread-synchronization).

### Example connection

The two corresponding conceptual examples are documented in [`examples/DelegateExample`](../../examples/DelegateExample/README.md) and [`examples/AsyncHandle`](../../examples/AsyncHandle/README.md).

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

The sequence moves from **local object-oriented programming**, to **distributed service boundaries**, to **HTTP-based services**, and finally to **asynchronous execution and responsive client applications**.

## Lab-to-lecture map

| Lab | Main concepts | Primary lecture connections | Example guide |
|---|---|---|---|
| Lab 1 | C#, OOP, WPF, project structure | [Lecture 1](../lecs/README.md#lecture-1---distributed-systems-and-rpc) | [Tutorial 1](../../examples/Tutorial-1-Solution/README.md) |
| Labs 2–3 | WCF, contracts, endpoints, bindings, multi-tier design | [Lectures 2–3](../lecs/README.md#lecture-2---components-services-and-wcf) | [Tutorial 2–3](../../examples/Tutorial-2-3-Solution/README.md) |
| Lab 4 | HTTP, REST, JSON, routing, Web API | [Lectures 5–6](../lecs/README.md#lecture-6---rest-apis-and-aspnet-core-mvcweb-api) | [SimpleWebAPI](../../examples/SimpleWebAPI/README.md) |
| Lab 5 | Service-to-service HTTP communication | [Lectures 5, 6, and 8](../lecs/README.md#lecture-8---web-platforms-and-state) | [WebApplication1](../../examples/WebApplication1/README.md) |
| Lab 6 | Delegates, callbacks, tasks, `async`/`await`, UI synchronization | [Lectures 3–4](../lecs/README.md#lecture-4---delegates-callbacks-and-thread-synchronization) | [DelegateExample](../../examples/DelegateExample/README.md), [AsyncHandle](../../examples/AsyncHandle/README.md) |

## Relationship to the assignment

The labs provide individual building blocks rather than a complete implementation of the assignment. The assignment combines several of these ideas in a larger WPF/WCF system, including service contracts, polling, duplex callbacks, concurrency control, asynchronous client operations, and UI-thread coordination.

The [Lecture Guide](../lecs/README.md) provides the complementary theory-to-assignment mapping, while this document explains where those concepts first appear in the practical laboratory sequence.

## Corresponding example guides

The original example implementations have been removed/redacted from the repository. Their conceptual guides remain available under [`../../examples/`](../../examples/):

| Lab | Example guide |
|---|---|
| Lab 1 | [`Tutorial-1-Solution`](../../examples/Tutorial-1-Solution/README.md) |
| Labs 2–3 | [`Tutorial-2-3-Solution`](../../examples/Tutorial-2-3-Solution/README.md) |
| Lab 4 | [`SimpleWebAPI`](../../examples/SimpleWebAPI/README.md) |
| Lab 5 | [`WebApplication1`](../../examples/WebApplication1/README.md) |
| Lab 6 | [`DelegateExample`](../../examples/DelegateExample/README.md) and [`AsyncHandle`](../../examples/AsyncHandle/README.md) |

## Repository note

The actual teaching documents and example source are intentionally not reproduced in this repository. Keep any locally retained copies separate from the Git-tracked documentation. This README preserves the conceptual map of the laboratory sequence without redistributing the original course content.
