# Resilience (EN)

Polly retry / timeout pipeline in front of the ELS `HttpClient`. The SDK
already retries; Polly here covers transport-level edge cases.

```bash
dotnet run --project examples/en/Resilience
```
