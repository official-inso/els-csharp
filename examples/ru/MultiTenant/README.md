# Multi-tenant (RU)

Показывает, как обогащать каждый capture per-request полем `tenantId`.
SDK-клиент singleton; контекст tenant'а передаётся per-call через meta,
потому что `client.User` процесс-глобальный.

```bash
dotnet run --project examples/ru/MultiTenant
curl -X POST http://localhost:5000/tenants/acme/orders
```
