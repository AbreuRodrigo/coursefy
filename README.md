# Coursefy

Coursefy is a .NET 10 Windows desktop app for browsing and playing local course/video content.
It uses:

- WinForms for the desktop UI
- WebView2 for embedded web content
- ASP.NET Core for local API/endpoints

## Requirements

- Windows
- .NET SDK 10.0+

## Run

```bash
dotnet restore Coursefy.sln
dotnet build Coursefy.sln
dotnet run --project Coursefy/Coursefy.csproj
```

## Project Layout

- `Coursefy/` main application project
- `Coursefy/src/Desktop/` desktop host and form
- `Coursefy/src/Api/` API endpoints
- `Coursefy/src/Services/` app services and scanning logic
