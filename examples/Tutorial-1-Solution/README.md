# Tutorial 1: Desktop C# Foundations

> Lab 1

## What this demonstrates

This example introduces basic object-oriented C# and two desktop application styles. A console project owns a small in-memory student model, while a WPF project references it and presents the data through a window. Despite the solution name, the desktop UI project is WPF; there is no Windows Forms project in this example.

## Main concepts

- Classes, properties, inheritance, and `ToString`.
- `List<T>` collections and simple in-memory data.
- Project references and reuse of a class library-like project.
- Console output versus event-driven WPF UI code.
- Input parsing and lookup by student ID.

## Architecture

```text
ConsoleApp1
  Person -> Student -> StudentList
             ^
             |
WpfApp1 ---- project reference ----> StudentList
   |
   +--> displays all students or one matching ID
```

`ConsoleApp1` contains the model and data creation code. `WpfApp1` is the presentation layer: button events read the controls, call the shared student list, and update the window.

## How it works

1. Construct several `Student` objects, each extending the shared `Person` data.
2. Store them in a list returned by a small `StudentList` helper.
3. The console entry point iterates over the list and prints each object.
4. The WPF window requests the list for a display operation or parses an ID for a search.
5. The event handler writes the result or an appropriate not-found/message state back to the UI.

## Key implementation ideas

Keep the model independent of the UI. Put student state and collection operations in ordinary C# classes, then reference that project from the WPF project. WPF controls raise events; handlers should validate text input before converting it to an integer and should not duplicate the model data.

## Rebuilding the example

Create a solution with a console/model project and a WPF project. Add `Person`, `Student`, and a list-producing helper to the first project. Add a project reference from the WPF project, design a window with list/search controls, and connect button events to the model. Add a console entry point that uses the same model to demonstrate non-UI execution.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.