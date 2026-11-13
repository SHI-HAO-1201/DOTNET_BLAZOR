# SQL Server and Entity Framework Core

The running application uses `Features/Shared/AppDbContext` with SQL Server.
Its migrations are stored in `Features/Shared/Migrations`; schema changes are
applied at startup. The legacy `Data/AppDbContext` and its migrations are not
used by the running application.

## Connection string

The connection string is configured in `appsettings.json` under
`ConnectionStrings:DefaultConnection`. The application does not fall back to a
local SQLite database. For deployment, use the `ConnectionStrings__DefaultConnection`
environment variable to supply the server's connection string.

## Code First

Install the matching EF Core CLI tool once:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
```

From the project directory, create and apply a migration for the running
application context:

```powershell
dotnet ef migrations add MigrationName --context BlazorXe_Xemay.Features.Shared.AppDbContext --output-dir Features/Shared/Migrations
dotnet ef database update
```

The initial migration creates the schema in `MotoSpeedDb`. Do not apply it to a
database that already contains application tables unless its schema and
migration history have been checked.
