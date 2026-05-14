# Inso.Els.Extensions.Logging

`Microsoft.Extensions.Logging` provider for [Inso.Els](https://www.nuget.org/packages/Inso.Els).
Routes `ILogger` calls to ELS so existing logging code automatically captures
errors without rewrites.

```csharp
using Inso.Els;
using Inso.Els.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEls(opts => { /* ... */ });
builder.Logging.AddEls(o => o.MinLevel = ElsLevel.Warning);
```

See the [project repository](https://github.com/official-inso/els-csharp) for full
documentation.
