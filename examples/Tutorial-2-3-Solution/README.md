# Tutorial 2-3: WCF Data Service

> Labs 2-3

## What this demonstrates

This example builds a small distributed data service. A WPF client communicates with a server through a shared WCF contract. The server delegates data work to a reusable library that owns an in-memory collection of account-like records.

## Main concepts

- Multi-project and multi-tier design.
- WCF service contracts, implementations, and self-hosting.
- `NetTcpBinding`, channel factories, and remote method calls.
- Data contracts, output values, and typed service faults.
- Keeping client, contract, service, and data responsibilities separate.

## Architecture

```text
DBClient (WPF)
    | ChannelFactory over net.tcp
    v
DBInterface (contract and fault)
    v
DBServer (service host and implementation)
    v
DBLib (records, generated data, in-memory database)
```

The client knows the contract, not the database implementation. `DBInterface` is the shared boundary. `DBServer` hosts the endpoint and translates calls into `DBLib` operations. `DBLib` contains the data representation and storage policy.

## How it works

1. The server creates sample records and opens a WCF endpoint on `net.tcp://0.0.0.0:8100/DataService`.
2. The client creates a channel to `net.tcp://localhost:8100/DataService`.
3. It asks for the number of records, then requests a selected record.
4. The service reads the in-memory database and returns typed values, including a bitmap field.
5. An invalid index is returned as a typed WCF fault and displayed by the client rather than treated as an unstructured exception.

## Key implementation ideas

Define the service interface in a project that both sides reference. Mark the data and fault types for serialization. Host the concrete implementation in a server process and keep connection details in client configuration or setup code. The client should use the contract through a channel factory, close or abort the channel appropriately, and convert transport data into UI-friendly values.

## Rebuilding the example

Create four projects: a data library, a contract library, a server executable, and a WPF client. Define methods for record count and indexed retrieval plus an out-of-range fault. Implement an in-memory repository, host it with a TCP WCF endpoint, and have the client bind to that contract and render the returned record. Run the server before the client.

## Source material notice

The original source code for this example has been intentionally redacted from this repository. The example is based on teaching materials provided as part of the COMP3008 coursework/laboratory material at Curtin University. This repository retains a conceptual description rather than redistributing the original teaching source.

This README documents the architecture, programming concepts, component responsibilities, and implementation approach so the material remains useful for study and reconstruction without reproducing the source code.