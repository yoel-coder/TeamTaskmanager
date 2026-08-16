@echo off
setlocal
title Team Task Manager

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK is not installed.
  echo Opening the official download page...
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  pause
  exit /b 1
)

where npm >nul 2>nul
if errorlevel 1 (
  echo Node.js is not installed.
  echo Opening the official download page...
  start "" "https://nodejs.org/en/download"
  pause
  exit /b 1
)

echo Installing frontend packages...
pushd "%~dp0frontend"
call npm install
if errorlevel 1 (
  echo Frontend installation failed.
  pause
  exit /b 1
)
popd

echo Starting the C# backend...
start "Team Task Manager API" /D "%~dp0backend" cmd /k dotnet run

echo Starting the React JSX frontend...
start "Team Task Manager React" /D "%~dp0frontend" cmd /k npm run dev

echo Waiting for the app to start...
timeout /t 7 /nobreak >nul
start "" "http://localhost:5173"

echo Team Task Manager is starting in your browser.
exit /b 0
