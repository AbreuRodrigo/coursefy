@echo off
setlocal
cd /d "%~dp0"

dotnet publish Coursefy.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -o .\publish\win-x64 ^
  /p:PublishSingleFile=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true

if errorlevel 1 (
  echo.
  echo Publish failed.
  pause
  exit /b 1
)

echo.
echo Publish complete.
echo EXE: %cd%\publish\win-x64\Coursefy.exe
start "" explorer "%cd%\publish\win-x64"

if /I "%~1"=="run" (
  start "" "%cd%\publish\win-x64\Coursefy.exe"
)

echo.
echo Tip: use "publish_csharp.bat run" to publish and launch immediately.
pause
endlocal
