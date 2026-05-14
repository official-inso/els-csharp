# Multi-tenant (EN)

Demonstrates how to enrich captures with a per-request `tenantId`. The SDK
client is a singleton; tenant context is supplied per call via meta because
`client.User` is process-wide.

```bash
dotnet run --project examples/en/MultiTenant
curl -X POST http://localhost:5000/tenants/acme/orders
```
