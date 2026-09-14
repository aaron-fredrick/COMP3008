# Web Application to Web Application

> Lab 5

## What this demonstrates

This example contains two independent ASP.NET Core applications. One exposes a small API endpoint; the other uses a controller to call that endpoint over HTTP and return the result through its own route. It shows application-to-application communication rather than a shared in-process method call.

## Main concepts

- Multiple web projects and independent hosts.
- MVC controllers versus an API-style controller.
- Conventional and attribute routing.
- Outbound HTTP calls from server-side application code.
- JSON deserialization and forwarding a result to a caller.
- Separate ports and process startup order.

## Architecture

```text
Browser/client
    |
    v
WebApplication1 :5254
    | RestSharp HTTP call
    v
WebApplication2 :5141
    | /api/Values
    v
JSON/text response
```

`WebApplication2` owns the values endpoint. `WebApplication1` owns the calling controller and acts as an HTTP client as well as a web server. The projects can be deployed or changed independently because their boundary is an HTTP contract.

## How it works

1. Start WebApplication2 so its API is available at `http://localhost:5141/api/Values`.
2. Start WebApplication1 at `http://localhost:5254`.
3. A request to WebApplication1's forwarding route enters its controller.
4. That controller sends a GET request to WebApplication2, receives the response, and deserializes or wraps it.
5. WebApplication1 returns an `Ok` response to the original caller.

The projects also contain ordinary Razor MVC pages, which provide the host applications' normal browser-facing routes. Those pages are separate from the service-to-service call.

## Key implementation ideas

Treat the other application as an external dependency: configure its base URL, send an HTTP request, check the response, and map the result into the local response contract. Do not call the other controller class directly. The two applications must be running on compatible ports, and failures in the downstream application should be represented as an appropriate local error.

## Rebuilding the example

Create two ASP.NET Core projects. Add a simple API controller to the first with a GET endpoint returning a small JSON-compatible value. Add a controller to the second that uses an HTTP client library to call the first endpoint and return the result. Add normal MVC views if desired, configure distinct local ports, and test the downstream endpoint before the forwarding route.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.