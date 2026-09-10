# Integration Tests

These tests exercise the running WCF endpoints rather than server classes in isolation.

The integration suite is intentionally separated by protocol/feature so failures identify the affected P0 area. Tests must use deterministic assertions and clean up sessions/channels in `finally` blocks.

Current coverage:

- `PollingMessageTests`: public-message delivery and polling boundary semantics.
- `DuplexMessageTests`: callback registration and public-message callback delivery.

The CI runner should start `Chat.Server.exe` first and execute these tests only after both configured endpoints are listening.
