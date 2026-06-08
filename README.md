# PRM Server

ASP.NET Core REST API for the **Project & Resource Management (PRM) Tool** — a console client–server system for IT services companies to manage employees, projects, allocations, and timesheets.

This repository contains the backend only. Pair it with the [PRM Client](../Client/) console app and the [AiService](../AiService/) Python microservice for AI skill matching and project risk summaries.

## Solution structure

| Project | Purpose |
|---------|---------|
| `Prm.Api` | REST API, controllers, business services, Hangfire jobs |
| `Prm.Data` | EF Core entities, repositories, migrations, seed data |
| `Prm.Common` | Shared DTOs, constants, enums |
| `UserManagement` | JWT authentication, login, refresh tokens, password change |

```
Server/
├── src/
│   ├── Prm.Api/           # Web API entry point
│   ├── Prm.Data/          # Database layer (see src/Prm.Data/README.md)
│   ├── Prm.Common/        # Shared models
│   └── UserManagement/    # Auth module
└── tests/
    ├── Prm.Api.Tests/
    └── UserManagement.Tests/
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or LocalDB
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (for migrations):

  ```powershell
  dotnet tool install --global dotnet-ef
  ```

## Configuration

Edit `src/Prm.Api/appsettings.json`:

| Section | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt` | Issuer, audience, secret, token lifetimes |
| `BootstrapAdmin` | First-run admin account (created on startup if missing) |
| `Hangfire` | Background job dashboard path and scheduler settings |

Do not commit production secrets. Use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables for overrides.

Default API URL: `http://localhost:5180` (see `Properties/launchSettings.json`).

## Database

On first run, the API applies EF Core migrations and seeds roles (Admin, Manager, Employee) plus the bootstrap admin user.

Manual migration commands (run from `Server/`):

```powershell
dotnet ef database update --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj
```

Full migration workflow: [src/Prm.Data/README.md](src/Prm.Data/README.md).

## Run

```powershell
cd Server/src/Prm.Api
dotnet run
```

Or open `Prm.slnx` in Visual Studio and run **Prm.Api**.

- API: `http://localhost:5180`
- Hangfire dashboard: `http://localhost:5180/hangfire` (credentials in `appsettings.json`)

## Features

- **Admin** — users, employees, projects, milestones, skills, allocations, system configuration
- **Manager** — resource dashboard, allocate resources, my projects, team timesheets, AI Assistant (via client → AiService)
- **Employee** — submit timesheets, view history and allocations
- **Background jobs** — employee bench status and project health / risk flag recalculation (Hangfire)

## Auth API

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/login` | Username + password → JWT + refresh token |
| POST | `/api/auth/refresh` | Refresh token rotation |
| POST | `/api/auth/change-password` | Change password (authenticated) |

All other routes require a valid JWT (`Authorization: Bearer …`).

## Tests

```powershell
cd Server
dotnet test Prm.slnx
```

## Related repos

| Repo | Role |
|------|------|
| **PRM Client** | .NET console UI for Admin, Manager, Employee |
| **PRM AiService** | Python FastAPI service for skill match and risk summary |
