# Simple Web API

> Lab 4

## What this demonstrates

This example exposes an in-memory student collection through ASP.NET Core HTTP endpoints and consumes those endpoints from a small WPF client. It introduces controller routing, JSON model binding, HTTP verbs, status codes, and the boundary between a desktop client and a web API.

## Main concepts

- ASP.NET Core application startup and middleware.
- MVC/API controllers and attribute routes.
- GET for reads and POST for creating a record.
- JSON request and response models.
- `200 OK`, `404 Not Found`, and client-side deserialization.
- Process-local application state.

## Architecture

```text
WPF SimpleWebClient
    | HTTP + JSON
    v
ASP.NET Core SimpleWebAPI
    |
    +--> StudentController --> in-memory StudentList
    +--> CalculatorController (routing/parameter example)
```

`Program` configures the web host, routes, and initial sample students. Controllers translate HTTP requests into model operations and return HTTP responses. The WPF client is deliberately separate and knows only the URL and JSON shape.

## How it works

1. The server seeds two students and listens at `http://localhost:5076`.
2. A client GET requests either the full collection or `/student/detail/{id}`.
3. The controller returns JSON for a match or `404` when the ID is absent.
4. A POST sends a student JSON body; model binding creates the controller parameter and the new record is added to memory.
5. The WPF client deserializes the response and refreshes its controls.

The calculator controller provides additional examples of route and query parameters. Data disappears when the server process stops; this is not a database-backed API.

## Key implementation ideas

Use models to define the JSON shape, controllers to define HTTP behavior, and a separate list/store helper for state. Route templates should make the resource and identifier explicit. Validate input and choose response status codes deliberately. The client should handle connection errors, non-success status codes, and JSON conversion instead of assuming every request succeeds.

## Rebuilding the example

Create an ASP.NET Core web project with a student model, an in-memory store seeded at startup, and a controller with collection/detail GET actions and a detail POST action. Add a second controller for parameter-routing exercises. Create a WPF client using an HTTP library, serialize a student request, deserialize responses, and point it at the local API URL.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.