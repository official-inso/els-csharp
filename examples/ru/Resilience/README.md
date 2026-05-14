# Resilience (RU)

Polly retry / timeout пайплайн перед `HttpClient` ELS. SDK уже ретраит;
Polly здесь покрывает transport-level edge cases.

```bash
dotnet run --project examples/ru/Resilience
```
