# Example Code ↔ Lab Index Map

This directory contains example/tutorial projects collected from the COMP3008 practical material. The table below maps each example folder to the lab material it belongs to.

The mapping is based on the subject matter demonstrated by each project and is intended to make the examples easier to navigate alongside `docs/labs/`.

## Lab mapping

| Example folder | Lab index | What the example covers |
|---|---:|---|
| `Tutorial-1-Solution/` | **Lab 1** | Basic desktop application development, including WPF and Windows Forms examples. |
| `Tutorial-2-3-Solution/` | **Labs 2–3** | Multi-tier/database example with separate client, interface, library, and server components. |
| `SimpleWebAPI/` | **Lab 4** | ASP.NET Core Web API fundamentals, including controllers and HTTP endpoints. |
| `WebApplication1/` | **Lab 5** | Web application example using separate web application projects. |
| `DelegateExample/` | **Lab 6** | Delegates/callback-style programming around the distributed client/server example. |
| `AsyncHandle/` | **Lab 6** | Asynchronous handling built around the same student/database/business-tier example, including a WPF client. |

## How the folders relate to the lab documents

The corresponding lab documents are stored in [`../docs/labs/`](../docs/labs/):

- [`Lab 1.pdf`](../docs/labs/Lab%201.pdf) → `Tutorial-1-Solution/`
- [`Lab 2.pdf`](../docs/labs/Lab%202.pdf) → `Tutorial-2-3-Solution/` (together with the following tutorial material)
- [`Lab 4 and 5.pdf`](../docs/labs/Lab%204%20and%205.pdf) → `SimpleWebAPI/` and `WebApplication1/`
- [`Lab 6.pdf`](../docs/labs/Lab%206.pdf) → `DelegateExample/` and `AsyncHandle/`

There is no separate `Lab 3.pdf` in the repository; the example directory is explicitly named `Tutorial-2-3-Solution`, so it is grouped with the Labs 2–3 material rather than being treated as a separate standalone example.

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

These projects are **examples/reference implementations**, not part of the main `src/` application. They should be read alongside the relevant lab PDF rather than treated as production architecture for the main chat system.

The main assignment implementation remains under `src/`; these examples exist to illustrate individual technologies and programming patterns used in the course material.
