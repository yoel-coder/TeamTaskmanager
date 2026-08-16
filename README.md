# Team Task Manager

A shared team task manager with an ASP.NET Core backend and a React frontend written in plain JavaScript.

## One-click Windows start

Download and extract the repository, then double-click `run-windows.bat`. It installs the frontend packages, starts the C# API and React JSX frontend, and opens the app in your browser.

## Technology

- Backend: C#, ASP.NET Core 8 Minimal API
- Frontend: React, JavaScript, Vite
- Database: Microsoft SQL Server / SQL Server LocalDB through Entity Framework Core
- No TypeScript

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

## Features

- Secure username and password registration and login
- Add shared tasks with descriptions and due dates
- Claim a task so the start button is unavailable to everyone else
- Only the user working on a task can complete it
- Separate completed-task list with the user and completion date
