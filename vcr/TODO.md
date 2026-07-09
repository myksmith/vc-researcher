# TODO

## 1. Refactor HTTP Client Initialization

The singleton pattern in `NotionHelper` and `AttioHelper` works but .NET's `IHttpClientFactory` is the preferred approach — it handles connection pooling and lifetime management correctly.

- Replace manual singleton clients in `vcrutils` with `IHttpClientFactory`
- Register clients with DI in the application entry point

## 2. Improve Error Handling

Current try/catch + `Console.WriteLine` is fine for a CLI tool but a proper logging framework would give structured output and log levels.

- Integrate Serilog or NLog
- Replace `Console.WriteLine` error output with structured log calls

## 3. Parallelise Independent API Calls

In several flows (e.g. validating Notion + finding the Attio record before the Perplexity call), the two lookups are sequential but independent. `Task.WhenAll` would reduce latency.

- Audit flows where Notion and Attio calls are made back-to-back
- Refactor to run independent calls in parallel with `Task.WhenAll`
