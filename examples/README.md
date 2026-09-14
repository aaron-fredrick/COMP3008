# Example Code ↔ Lab Index Map

This directory contains conceptual guides for example projects from the COMP3008 practical material. The original example implementations have been redacted; use the per-example README after following the lab mapping below.

The mapping is based on the current example contents, directory names, and the available lab documents in [`../docs/labs/`](../docs/labs/).

## Lab mapping

| Example folder | Lab index | What the example covers |
|---|---:|---|
| [`Tutorial-1-Solution/`](Tutorial-1-Solution/README.md) | **Lab 1** | C# classes, inheritance, an in-memory collection, console output, and a WPF client. |
| [`Tutorial-2-3-Solution/`](Tutorial-2-3-Solution/README.md) | **Labs 2-3** | WCF multi-tier client, contracts, business/service host, and in-memory database. |
| [`SimpleWebAPI/`](SimpleWebAPI/README.md) | **Lab 4** | ASP.NET Core Web API controllers, routing, JSON, GET/POST, and a WPF HTTP client. |
| [`WebApplication1/`](WebApplication1/README.md) | **Lab 5** | Two ASP.NET Core applications with an HTTP call from one controller to the other. |
| [`DelegateExample/`](DelegateExample/README.md) | **Lab 6** | WCF layers plus delegate `BeginInvoke`/callback asynchronous programming. |
| [`AsyncHandle/`](AsyncHandle/README.md) | **Lab 6** | The same WCF layers with `Task`/`async`/`await` client handling. |

## How the folders relate to the lab documents

The corresponding lab documents are stored in [`../docs/labs/`](../docs/labs/):

- [`Lab 1.pdf`](../docs/labs/Lab%201.pdf) → `Tutorial-1-Solution/`
- [`Lab 2.pdf`](../docs/labs/Lab%202.pdf) → `Tutorial-2-3-Solution/` (grouped with the Lab 3 tutorial material)
- [`Lab 4 and 5.pdf`](../docs/labs/Lab%204%20and%205.pdf) → `SimpleWebAPI/` and `WebApplication1/`
- [`Lab 6.pdf`](../docs/labs/Lab%206.pdf) → `DelegateExample/` and `AsyncHandle/`

There is no separate `Lab 3.pdf` in the repository. The directory is explicitly named `Tutorial-2-3-Solution`, so its guide treats the material as one Labs 2-3 multi-tier example.

## Folder outline

```text
examples/
├── Tutorial-1-Solution/       # Lab 1
├── Tutorial-2-3-Solution/     # Labs 2–3
├── SimpleWebAPI/              # Lab 4
├── WebApplication1/           # Lab 5
├── DelegateExample/           # Lab 6
├── AsyncHandle/               # Lab 6
└── README.md                  # This mapping
```

## Notes

These folders are **conceptual reconstruction guides**, not part of the main `src/` application. Read each guide alongside the relevant lab PDF rather than treating it as production architecture for the main chat system.

The main assignment implementation remains under `src/`; these examples exist to illustrate individual technologies and programming patterns used in the course material.
