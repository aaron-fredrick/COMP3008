# Delegate-Based Asynchronous Client

> Lab 6

## What this demonstrates

This example combines a layered WCF student service with the older .NET delegate asynchronous pattern. The synchronous WPF client provides a baseline; the asynchronous client performs the same search without keeping the UI blocked while the business tier simulates slow work.

## Main concepts

- Database, service, business, and presentation layers.
- WCF contracts and TCP service boundaries.
- Delegate declaration, registration, `BeginInvoke`, and `EndInvoke`.
- Completion callbacks and `IAsyncResult`/wait handles.
- WPF dispatcher-based UI updates.

## Architecture

```text
AsyncClient / Client (WPF)
    |
    v
StudentBusinessTier :8200
    |
    v
DatabaseServer :8100
    |
    v
StudentDatabase (in-memory students)
```

The client talks to the business tier, not directly to the database service. The business tier applies the search operation and forwards data calls. Each server has a contract and implementation, keeping transport details separate from the data model.

## How it works

1. Start the database service at `net.tcp://localhost:8100/StudentService`.
2. Start the business service at `net.tcp://localhost:8200/StudentBusinessService`.
3. The client submits a search value to the business service.
4. The asynchronous client assigns its search method to a delegate and calls `BeginInvoke`.
5. The UI remains responsive while the service performs the delayed search.
6. The callback receives the `IAsyncResult`, calls `EndInvoke` for the return value, closes the wait handle, and uses the WPF dispatcher to update controls.

The synchronous client performs the same logical search directly and makes the blocking behavior easier to compare.

## Key implementation ideas

A delegate represents a method signature that can be invoked later. `BeginInvoke` starts the operation and accepts a callback; `EndInvoke` retrieves the result and surfaces exceptions. UI controls must be changed on the UI thread, so completion code marshals back through `Dispatcher.Invoke`. The artificial delay in the business tier makes the difference between blocking and asynchronous interaction visible.

## Rebuilding the example

Create a shared student model, a database WCF service, and a business WCF service that calls it. Add synchronous and asynchronous WPF clients. In the asynchronous client, define a delegate matching the search method, start it with `BeginInvoke`, handle completion with a callback, call `EndInvoke`, and marshal the result to the UI dispatcher. Add status text so start, waiting, and completion are observable.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.