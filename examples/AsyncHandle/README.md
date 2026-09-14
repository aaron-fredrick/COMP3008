# Task-Based Asynchronous Client

> Lab 6

## What this demonstrates

This example is the `Task`/`async`/`await` version of the layered student service used by the delegate example. It preserves the database, WCF database service, and business tier, but changes how the WPF client waits for a slow search result.

## Main concepts

- Reusable multi-tier WCF architecture.
- `Task<T>` as a representation of future work.
- `async` methods and `await` without blocking the UI thread.
- Completion ordering and UI state updates.
- Comparing modern task-based code with a synchronous baseline.

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

The service topology is the same as `DelegateExample`: the business tier is the client's boundary and delegates data access to the database server. The difference is the client-side completion mechanism.

## How it works

1. Start the database service at `net.tcp://localhost:8100/StudentService`.
2. Start the business service at `net.tcp://localhost:8200/StudentBusinessService`.
3. The WPF client captures the search text and starts a `Task<Student>` for the search operation.
4. It reports that the search has started, then awaits the task.
5. When the result arrives, execution resumes after `await` and updates the controls.
6. The client reports completion while the window has remained responsive during the delay.

The synchronous client is included as a comparison point. The two solution variants contain the same conceptual service arrangement; the teaching focus is the task-based client flow.

## Key implementation ideas

`Task<T>` separates starting work from receiving its result. An `async` event handler can await the task, allowing the UI message loop to continue processing while the remote operation is pending. Exceptions from the operation should be observed around the `await`, and UI updates should happen in the continuation on the UI context. This avoids manually managing callbacks and wait handles while retaining explicit completion behavior.

## Rebuilding the example

Reuse the layered services described in the delegate guide. In the WPF client, expose the search operation as a task, start it from an event handler, update a busy/status indicator, await the result, and render the student when it completes. Add error handling and restore the controls in a `finally` block. Keep a synchronous client or mode so the blocking difference can be observed.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.