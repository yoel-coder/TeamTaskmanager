# Team Task Manager

A shared team task manager built with an ASP.NET Core backend, a React JSX frontend, and SQL Server through Entity Framework Core.

## Features

- Secure username and password registration and login
- Passwords stored as salted PBKDF2 hashes
- Add shared tasks with descriptions and due dates
- Claim a task so nobody else can claim it
- Only the assigned user can complete a task
- Completed-task history with the user and completion date
- Seven-day login sessions

## One-click Windows start

Download and extract the repository, then double-click `run-windows.bat`. It installs the frontend packages, starts the C# API and React JSX frontend, and opens the app in your browser.

## Technology

- Backend: C#, ASP.NET Core 8 Minimal API
- Frontend: React, JavaScript, Vite
- Database: Microsoft SQL Server / SQL Server LocalDB through Entity Framework Core
- No TypeScript

## Project structure

```text
backend/
  Data/                 EF Core DbContext
  Models/               Tasks, users, sessions, and request models
  Repositories/         EF Core repository interfaces and implementations
  Services/             Authentication business logic
  Program.cs            API endpoints and dependency injection
frontend/
  src/App.jsx           React application
  src/styles.css        Application design
database/schema.sql     Optional SQL schema reference
run-windows.bat         One-click Windows launcher
```

## Requirements

- .NET 8 SDK
- Node.js and npm
- SQL Server LocalDB, normally installed with Visual Studio's ASP.NET workload

## Run the backend

Install the .NET 8 SDK, then run:

```bash
cd backend
dotnet run
```

The API runs at `http://localhost:5055`.

## Run the frontend

In another terminal, run:

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`.

## Database

The backend uses EF Core repositories with SQL Server LocalDB. EF Core automatically creates the `TeamTaskManager` database and its Users, Sessions, and Tasks tables when the API starts. You do not need to run SQL queries manually. The SQL file is retained only as an optional schema reference.

## Get the latest GitHub changes

```bat
cd /d "C:\Users\yoell\source\repos\TeamTaskmanager"
git pull
run-windows.bat
```
