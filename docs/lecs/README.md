# Distributed Computing — Lecture Guide

This is a **conceptual study guide** to the COMP3008 Distributed Computing lecture sequence. It connects the lecture concepts with the laboratory sequence, example guides, and the main chat assignment. It is a revision aid, not a replacement for the official course materials.

## Source material notice

The original lecture PDFs are Curtin University course teaching materials and are intentionally omitted/redacted from this repository to avoid redistributing protected course content.

This repository retains conceptual summaries and navigation only. The descriptions below do not reproduce the original lecture documents, slide text, exercises, or substantial portions of the teaching material. Locally retained copies may be used for study where appropriate, but they are not part of the Git-tracked documentation.

For the practical side of the course, see the [Laboratory Guide](../labs/README.md). The two guides cross-reference each other so that the relationship between **lecture theory → lab practice → assignment implementation** is explicit.

---

## Course progression

The lectures move from local programming foundations to distributed boundaries, asynchronous communication, web protocols, data persistence, and web presentation:

```text
C# and distributed-systems foundations
        |
        v
RPC, components, services, and WCF
        |
        v
Multi-tier architecture and asynchronous execution
        |
        v
Delegates, callbacks, duplex channels, and synchronization
        |
        v
HTTP, XML/JSON, SOAP, REST, and web APIs
        |
        v
Database connectivity and persistence
        |
        v
Web platforms, MVC, Razor, and stateful/stateless interaction
```

The labs turn selected parts of this progression into working patterns: desktop clients, WCF services, HTTP APIs, application-to-application communication, and asynchronous clients.

---

## Lecture 1 — Distributed Systems and RPC

### Topic

An introduction to distributed systems, C# and WPF, inter-process communication, and remote procedure calls.

### Core concepts

- Distributed computing splits useful application work across multiple processes or machines; it is more than simply having a client and a server.
- Client-server, peer-to-peer, and distributed applications describe different ways work and responsibility are arranged.
- Inter-process communication is required when components no longer share one address space.
- RPC makes a remote operation look like a function call, using client stubs, server implementations, requests, responses, and serialization.
- Serialization converts objects into a transferable representation; deserialization reconstructs them. Marshaling is the related conversion between representations in a managed environment.
- Remote calls are slower and less reliable than local calls. Network failure, server failure, configuration errors, security, and scaling must be treated as part of the design.
- C# access modifiers, properties, classes, inheritance, and WPF/XAML provide the local programming foundation for the later service examples.

### What to understand

A remote method is not a local method with a longer path. It can time out, fail, serialize data differently, and leave the caller uncertain about what happened. A useful distributed interface makes the boundary explicit and handles failures deliberately.

### Lab connection

[Lab 1](../labs/README.md#lab-1--c-and-desktop-application-foundations) establishes the C# object model, project references, collections, console flow, and WPF event-driven presentation before a network boundary is introduced. The corresponding conceptual example is [`Tutorial-1-Solution`](../../examples/Tutorial-1-Solution/README.md).

---

## Lecture 2 — Components, Services, and WCF

### Topic

How to decide what to distribute: components, service-oriented architecture, interoperability, and a first WCF client/server design.

### Core concepts

- A component is a cohesive set of functions that can operate independently and provide a service through an external interface.
- Objects are usually implementation-level units linked within an application; components are architectural units exposed across an application or process boundary.
- Service-oriented design groups behaviour behind a stable boundary, reducing coupling and hiding implementation details.
- CORBA and Java RMI illustrate earlier approaches to language interoperability and remote components; WCF provides the .NET service model used by the examples.
- A WCF service contract declares the operations, an operation contract marks an exposed method, and an endpoint combines an address, binding, and contract.
- Bindings select transport and message behaviour. The lecture contrasts HTTP-oriented bindings with `NetTcpBinding` for TCP-based communication.
- `ChannelFactory` creates a client channel from the shared contract. Service behaviour controls instance lifetime, concurrency, synchronization context, and fault detail.
- A DLL or shared contract assembly allows client and server projects to agree on types and interfaces without sharing the implementation.

### What to understand

The contract is the architectural seam between caller and service. The client should depend on the contract and endpoint configuration, not on the server's internal data structures. The network boundary also means that service behaviour, serialization, faults, and concurrency are part of the public design.

### Lab connection

[Labs 2–3](../labs/README.md#labs-2–3--multi-tier-clientserver-and-wcf) put these ideas into practice with a WPF client, shared WCF interface, service host, and data library. The corresponding conceptual example is [`Tutorial-2-3-Solution`](../../examples/Tutorial-2-3-Solution/README.md).

---

## Lecture 3 — Multi-Tier Architecture and Task-Based Async

### Topic

Separating distributed responsibilities into tiers, then introducing non-blocking execution with one-way calls, threads, tasks, and `async`/`await`.

### Core concepts

- The three-tier model separates data, business, and presentation responsibilities. A four-tier view can distinguish a user-facing display tier from a broader presentation/API tier.
- Data access hides storage details; the business tier owns rules and processing; the presentation tier validates input and exposes an access interface; the display tier serves human interaction.
- Interfaces between tiers reduce coupling, support alternative implementations, improve security boundaries, and make load balancing or specialization possible.
- Tiers can be combined, split, or replicated when the application needs a cache, parallel work, load balancing, or fault tolerance. More tiers are not automatically better.
- Blocking remote calls can waste resources and make users think an application has stopped. One-way operations return no result and therefore require a separate way to observe progress or completion.
- A thread is an independently scheduled execution path sharing a process's memory. Shared state introduces races and data-corruption risks.
- `Task` represents asynchronous work, while `async` and `await` allow a method to suspend without blocking the current thread and resume when the task completes.

### What to understand

Architecture determines where a responsibility lives and what interface crosses the boundary. Asynchronous design determines how a caller behaves while that boundary is busy. These are related but separate decisions: a well-layered service can still be called synchronously, asynchronously, or through a one-way operation.

### Lab connection

[Labs 2–3](../labs/README.md#labs-2–3--multi-tier-clientserver-and-wcf) demonstrate tier separation and service boundaries. [Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming) applies the asynchronous side through the [`AsyncHandle`](../../examples/AsyncHandle/README.md) and [`DelegateExample`](../../examples/DelegateExample/README.md) examples.

---

## Lecture 4 — Delegates, Callbacks, and Thread Synchronization

### Topic

Detailed asynchronous communication using .NET delegates, polling, completion callbacks, worker threads, and synchronization.

### Core concepts

- A delegate is a typed reference to a method. It can represent an event handler, an operation to run asynchronously, or a completion callback.
- `BeginInvoke` starts delegate work asynchronously and returns an `IAsyncResult`; `EndInvoke` retrieves the result and may block if completion has not happened.
- Polling repeatedly checks completion and can waste CPU. Blocking at a chosen point is simpler but can freeze a UI.
- A completion callback avoids unnecessary polling, but it runs on a worker thread and must safely hand results back to the caller or UI thread.
- Threads share process memory, so simultaneous calls can corrupt shared state without synchronization. Thread safety means shared operations remain correct under concurrent access; re-entrancy describes safe repeated entry into code.
- Synchronization can use framework-provided contexts or synchronized methods, or explicit locks, mutexes, waits, and signals. The correct choice depends on ownership and contention.

### What to understand

The important distinction is between starting work, observing completion, retrieving a result, and updating a UI. `EndInvoke` must be called exactly once for a delegate result, wait handles need cleanup, and a callback must not update WPF controls directly from a worker thread.

### Lab connection

[Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming) demonstrates `BeginInvoke`/`EndInvoke`, completion callbacks, tasks, and WPF dispatcher handoff. The corresponding conceptual guides are [`DelegateExample`](../../examples/DelegateExample/README.md) and [`AsyncHandle`](../../examples/AsyncHandle/README.md).

The assignment applies the same concerns in a different form through timer callbacks, WCF callbacks, `ReaderWriterLockSlim`, and dispatcher calls.

---

## Lecture 5 — Duplex Communication and Web Services

### Topic

A WCF duplex service that reports progress, followed by service-oriented web communication and the distinction between SOAP/SOA and REST/ROA.

### Core concepts

- A duplex WCF contract has a service interface and a callback contract implemented by the client.
- A one-way operation can start a long task without waiting for a normal return value; the server sends progress through the callback channel instead.
- `DuplexChannelFactory` creates the client side of the callback relationship. The UI callback must marshal progress to the WPF dispatcher.
- Services group related functionality behind boundaries and expose only what other services need. Internal objects are not passed directly across the boundary.
- HTTP is a widely available service transport. XML and JSON are data representations; XML is verbose and schema-oriented, while JSON is compact and familiar to web clients but leaves more semantics to API documentation.
- WSDL describes SOAP-style services for programmatic consumers. SOAP uses XML messages, commonly over HTTP; REST treats addressable resources and HTTP methods as the primary interface.
- REST commonly uses GET, POST, PUT, PATCH, and DELETE with resource-oriented URIs. Method semantics should match whether data is read, created, replaced, partially updated, or deleted.

### What to understand

Duplex communication changes the usual request/response direction for notifications: the client provides a callback endpoint, and the service can later push progress or events. HTTP APIs are normally request-driven, so an application needs polling, a callback-capable protocol, or another update mechanism when the server must notify the client.

### Lab connection

The duplex pattern is conceptually related to [Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming) through callbacks and UI-thread coordination. The HTTP/web-service material leads directly into [Lab 4](../labs/README.md#lab-4--aspnet-core-web-api) and [Lab 5](../labs/README.md#lab-5--application-to-application-web-communication), which apply service boundaries using HTTP rather than WCF duplex callbacks.

The assignment's [duplex service contract](../../src/Chat.Contracts/ServiceContracts/IDuplexChatService.cs) and [callback contract](../../src/Chat.Contracts/CallbackContracts/IChatCallback.cs) use the same client-implemented callback idea for chat notifications.

---

## Lecture 6 — REST APIs and ASP.NET Core MVC/Web API

### Topic

Resource-oriented HTTP operations, an introductory Node.js REST service, and an ASP.NET Core MVC/Web API implementation consumed by a WPF client.

### Core concepts

- A REST resource has an identifier and is transferred as a representation of its current state.
- GET retrieves a representation, POST creates a resource, PUT updates or replaces one, PATCH partially updates one, and DELETE removes one. Status codes communicate outcomes such as `200` and `404`.
- MVC separates model/data and business rules, controller request coordination, and view presentation. Web API controllers return structured data rather than rendering a page.
- Convention-based routing derives routes from controller/action names; attribute routing makes API paths and HTTP verbs explicit.
- Model binding maps request data, including JSON request bodies with `[FromBody]`, to action parameters. Serialization and deserialization turn models into transport representations and back.
- A WPF client can use an HTTP library and a JSON library to call the API without referencing the server's implementation.

### What to understand

An HTTP API is a protocol boundary, not a direct method call. A client must know the URI, method, representation, and expected status codes, and it must handle unavailable services and unsuccessful responses.

### Lab connection

[Lab 4](../labs/README.md#lab-4--aspnet-core-web-api) directly demonstrates HTTP, JSON, routing, GET/POST operations, model binding, and an ASP.NET Core Web API. [Lab 5](../labs/README.md#lab-5--application-to-application-web-communication) extends those ideas to service-to-service HTTP communication.

The corresponding conceptual examples are [`SimpleWebAPI`](../../examples/SimpleWebAPI/README.md) and [`WebApplication1`](../../examples/WebApplication1/README.md).

---

## Lecture 7 — Databases and .NET Data Access

### Topic

Distributed database concerns, SQLite fundamentals, universal connectivity, ADO.NET, Entity Framework, and database-backed ASP.NET APIs. This summary reflects the current `DC-Lecture7.pdf` in the repository.

### Core concepts

- Distributed databases distribute data across nodes using techniques such as partitioning, sharding, and replication. The resulting design must address scalability, availability, consistency, conflict resolution, security, authorization, auditing, and load balancing.
- SQLite is a portable, serverless, file-based, cross-platform database suited to prototyping and development. Larger or highly concurrent systems may use SQL Server, PostgreSQL, MySQL, or managed database services instead.
- Tables contain rows and columns; primary keys identify rows and foreign keys express relationships between tables.
- SQL CRUD operations create tables, insert rows, select data, update rows, and delete rows. Parameterized commands help keep values separate from SQL text and reduce injection risk.
- ODBC and JDBC provide standardized database APIs; ADO.NET provides .NET data providers for direct database access.
- Entity Framework provides an object-relational mapping layer. `DbContext` represents a unit of database interaction, configuration selects a provider and connection string, and migrations evolve the schema from model changes.
- ASP.NET Core can register a `DbContext` with dependency injection, after which controllers can use it to implement database-backed API actions.

### What to understand

Lecture 7 distinguishes storage concerns from API concerns. A controller should expose a resource contract, while a data-access layer or ORM manages connections, queries, mapping, and schema evolution. Direct ADO.NET and Entity Framework provide different trade-offs between low-level control and higher-level abstraction.

### Lab connection

There is no retained database-backed laboratory example corresponding directly to Lecture 7. [Labs 2–3](../labs/README.md#labs-2–3--multi-tier-clientserver-and-wcf) provide a useful architectural contrast because their data layer is separated from the service layer, but the retained example uses in-memory data rather than SQLite or Entity Framework.

The current assignment likewise does **not** use a database or ORM: sessions, channels, queues, and metadata are process-local, while file content uses a sharded filesystem store. Lecture 7 therefore provides a persistence option and supporting theory rather than a description of the assignment implementation.

---

## Lecture 8 — Web Platforms and State

### Topic

HTTP and the Web as a distributed request/response platform, connection behaviour, statefulness, and ASP.NET Core MVC/Razor pages.

### Core concepts

- The Web is a request/response architecture built around URLs, HTTP requests, HTTP responses, MIME types, and navigable HTML.
- TCP is connection-oriented and reliable; UDP is connectionless and does not provide the same delivery guarantees. HTTP is stateless at the application level, while the underlying transport may use persistent connections.
- A stateless server does not retain client-specific application state between requests by default, which supports simple scaling and independent requests. A stateful service retains session context between calls, which can suit interactive applications but requires lifecycle management.
- State can be layered onto a stateless protocol with cookies, client-supplied identifiers, session tokens, or timeouts. The protocol itself does not define the complete application session model.
- ASP.NET Core MVC maps routes to controller actions, renders Razor views, and serves static CSS/JavaScript from `wwwroot`. Layouts provide shared page structure; `ViewBag` and `ViewData` pass values from controllers to views; forms send user input with GET or POST.
- The browser acts as a display tier while the server-side presentation/controller tier coordinates models, controllers, and views.

### What to understand

HTTP's request/response model explains why a web application needs an explicit strategy for identity, session state, and repeated updates. It also explains the difference between a REST API response and a rendered MVC page: both use HTTP, but their representations and consumers differ.

### Lab connection

[Lab 5](../labs/README.md#lab-5--application-to-application-web-communication) is the closest practical connection because it uses independently hosted web applications communicating through HTTP. Its [`WebApplication1`](../../examples/WebApplication1/README.md) example also provides the clearest repository connection to ASP.NET Core MVC and controller-based web applications.

The assignment provides another contrast: the polling client repeatedly requests updates, while the duplex client maintains a callback relationship over TCP. Both provide ongoing interaction, but they use different communication and state models.

---

## How the lectures connect to the labs

| Lecture themes | Lab connection | Example / project connection |
|---|---|---|
| C# objects, properties, inheritance, WPF events | [Lab 1](../labs/README.md#lab-1--c-and-desktop-application-foundations) | [Tutorial 1](../../examples/Tutorial-1-Solution/README.md) |
| RPC, contracts, endpoints, bindings, service behaviour | [Labs 2–3](../labs/README.md#labs-2–3--multi-tier-clientserver-and-wcf) | [Tutorial 2–3](../../examples/Tutorial-2-3-Solution/README.md) |
| Multi-tier separation and task-based async | [Labs 2–3](../labs/README.md#labs-2–3--multi-tier-clientserver-and-wcf) and [Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming) | [AsyncHandle](../../examples/AsyncHandle/README.md) |
| Delegates, callbacks, `BeginInvoke`/`EndInvoke`, UI synchronization | [Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming) | [DelegateExample](../../examples/DelegateExample/README.md) |
| Duplex WCF callbacks and one-way progress | [Lab 6](../labs/README.md#lab-6--delegates-callbacks-and-asynchronous-programming); assignment | [Duplex contracts](../../src/Chat.Contracts/ServiceContracts/IDuplexChatService.cs) |
| HTTP, JSON, routing, GET/POST, controller model binding | [Lab 4](../labs/README.md#lab-4--aspnet-core-web-api) | [SimpleWebAPI](../../examples/SimpleWebAPI/README.md) |
| Separate web applications and outbound HTTP | [Lab 5](../labs/README.md#lab-5--application-to-application-web-communication) | [WebApplication1](../../examples/WebApplication1/README.md) |
| SQLite, ADO.NET, EF, migrations, database APIs | No direct retained lab implementation; architectural contrast in Labs 2–3 | Current Lecture 7 only |
| MVC views, Razor, HTTP state, and web platforms | [Lab 5](../labs/README.md#lab-5--application-to-application-web-communication) | [WebApplication1](../../examples/WebApplication1/README.md) |

This table is intentionally explicit: the lectures do not all map one-to-one to a laboratory. Some concepts are taught theoretically, some are demonstrated in a lab, and some are used later in the assignment.

## How the lectures connect to the assignment

The assignment is a concrete distributed application rather than a direct copy of any one teaching example:

```text
WPF polling client -- BasicHttpBinding/HTTP --> ChatService
WPF duplex client -- NetTcpBinding/TCP -----> ChatService
                                                   |
                         contracts, routing, state, locks, storage
```

- **Contract-first boundaries:** `IChatService`, `IDuplexChatService`, and `IChatCallback` apply the contract, endpoint, serialization, and callback ideas from Lectures 1, 2, and 5.
- **Multiple communication models:** the polling client periodically retrieves newer messages and files; the duplex client receives one-way callback notifications. This connects Lecture 5's push callbacks with Lecture 8's request/response and state discussion.
- **Concurrency and synchronization:** the shared service allows concurrent WCF calls. `ReaderWriterLockSlim`, membership-transition locking, callback identity checks, and non-overlapping polling protect shared state, applying the thread-safety material from Lectures 3 and 4.
- **Responsive clients:** timer callbacks, task-returning duplex coordination, background file work, and WPF dispatcher handoff keep network and file work away from UI event handling, applying the asynchronous material from Lectures 3 and 4.
- **Layered service design:** the server separates user/session management, channels, message routing, callbacks, and file handling. This follows the separation-of-concerns and component ideas from Lectures 2 and 3.
- **Persistence boundary:** file content is written to a sharded filesystem store, while sessions, channels, message history, and metadata remain in memory. Lecture 7's database designs are relevant alternatives, but they are not currently part of this assignment.

## Key concepts to know

- A distributed call crosses a failure-prone boundary and must be designed differently from a local call.
- Contracts define what clients may depend on; bindings and endpoints define how they connect.
- Components and tiers reduce coupling by assigning each responsibility to a coherent interface.
- Synchronous calls block; asynchronous calls require an explicit completion model.
- Delegates, callbacks, tasks, and `await` are different mechanisms for representing or observing work.
- Callback code may run on a worker thread; UI updates must return through the UI dispatcher.
- Shared service state requires synchronization and clear session/membership invariants.
- HTTP is application-level request/response and is commonly used in a stateless manner; polling and callbacks provide different update strategies.
- JSON and XML are representations, not business rules; status codes and HTTP method semantics are part of an API contract.
- Database APIs, ORMs, keys, transactions, migrations, and distributed consistency solve storage problems that are distinct from controller routing.
- A useful architecture makes the theory visible in code: contract, transport, state, concurrency, storage, and presentation should each have a clear responsibility.

## Navigation

- [Laboratory Guide](../labs/README.md) — practical concepts, lab progression, and example mappings.
- [Example Guides](../../examples/README.md) — conceptual documentation for the retained example projects.
