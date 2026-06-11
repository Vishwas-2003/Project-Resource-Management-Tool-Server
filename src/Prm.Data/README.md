# Prm.Data

Entity Framework Core data access for the PRM API. Migration scripts are stored under `Persistence/Migration` (C# namespace: `Prm.Data.Persistence.Migrations`).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):

```powershell
dotnet tool install --global dotnet-ef
```

- SQL Server or LocalDB
- Connection string in `../Prm.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`)

Run all commands from the **Server** solution folder:

```powershell
cd Server
```

## Create a new migration

After changing entities or `Persistence/Configurations`, add a migration (files are written to `Persistence/Migrations`):

```powershell
dotnet ef migrations add <MigrationName> --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj --output-dir Persistence/Migrations
```

Example:

```powershell
dotnet ef migrations add AddResourceTable --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj --output-dir Persistence/Migrations
```

## Apply migrations to the database

```powershell
dotnet ef database update --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj
```

The API also runs `Database.Migrate()` on startup via `DatabaseInitializer`.

## Remove the last migration (if not applied)

```powershell
dotnet ef migrations remove --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj
```

## List migrations

```powershell
dotnet ef migrations list --project src/Prm.Data/Prm.Data.csproj --startup-project src/Prm.Api/Prm.Api.csproj
```

## Folder layout

| Path | Purpose |
|------|---------|
| `Entities/` | Domain entities |
| `ServiceCollectionExtensions.cs` | `AddDbContext` — SQL Server registration (`namespace Prm.Data`) |
| `Persistence/AppDbContext.cs` | `DbSet`s; applies configurations from `Persistence/Configurations` |
| `Persistence/Configurations/` | `IEntityTypeConfiguration<T>` per entity (table names, keys, indexes, relationships) |
| `Migrations/` | EF Core migration scripts and model snapshot |
| `Repositories/` | Data access repositories |
