# Coursefy

Coursefy is a Windows app I use to make local courses easier to watch.

If you have a bunch of downloaded tutorials spread across folders, this helps turn that mess into something you can actually browse and play without hunting through File Explorer every time.

## What problem this solves (in plain English)

Watching local courses usually gets annoying fast:

- Videos are buried in random folders
- You forget where you stopped
- Opening files one-by-one kills momentum

Coursefy gives you one place to scan your course folders, browse them, and play content quickly.
It is meant to feel like "my own local course library", not a giant media server setup.

## How this is used

Typical flow:

1. Point Coursefy at the folder(s) where your courses/videos live.
2. Let it scan and build an index.
3. Open the app and pick what you want to watch.
4. Continue learning without digging through directories.

## Tech stack

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
