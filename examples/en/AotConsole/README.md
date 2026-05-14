# AOT console (EN)

Native AOT smoke example.

```bash
# Regular run
dotnet run --project examples/en/AotConsole

# Native AOT publish (produces some Inso.Els trim/AOT warnings — see roadmap)
dotnet publish examples/en/AotConsole -c Release -p:PublishAot=true -r osx-arm64 --self-contained
```
