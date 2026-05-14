# AOT-консоль (RU)

Smoke-пример под Native AOT.

```bash
# Обычный запуск
dotnet run --project examples/ru/AotConsole

# Native AOT publish (будут warnings от Inso.Els — см. roadmap)
dotnet publish examples/ru/AotConsole -c Release -p:PublishAot=true -r osx-arm64 --self-contained
```
