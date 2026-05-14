# Фильтрация (RU)

Комбинирует `MinLevel`, `SampleRate`, синхронный `BeforeSend` (маскирует
email) и асинхронный `BeforeSendAsync` (отбрасывает заблокированных
tenant'ов). Показывает, что critical-уровень всегда проходит.

```bash
dotnet run --project examples/ru/Filtering
```
