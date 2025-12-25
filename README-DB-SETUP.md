# Database setup for IMS with PostgreSQL

This project uses EF Core with Npgsql for PostgreSQL. The repository includes a helper PowerShell script `scripts/setup-postgres-migrate.ps1` to start a local Docker PostgreSQL instance, optionally add a migration, and apply migrations to create the database.

Prerequisites
- Docker (desktop or daemon) running locally
- .NET SDK compatible with project (net10.0)
- `dotnet-ef` tool (install via `dotnet tool install --global dotnet-ef`) or use the CLI shipped in the SDK

Quick start
1. Start and migrate using the helper script (PowerShell):

   powershell -File scripts/setup-postgres-migrate.ps1 -DbName imsdb -DbUser imsuser -DbPassword ims_pass -HostPort 5433

   The script will expose Postgres on localhost:5433 using the provided credentials and apply migrations.

2. Alternatively run migrations manually:

   - Start a PostgreSQL container:
     docker run -d --name ims-postgres -e POSTGRES_DB=imsdb -e POSTGRES_USER=imsuser -e POSTGRES_PASSWORD=ims_pass -p 5433:5432 postgres:15-alpine

   - Export connection string (PowerShell example):
     $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=imsdb;Username=imsuser;Password=ims_pass"

   - Add migration (optional):
     dotnet ef migrations add InitialCreate --project src/IMS.Api.Infrastructure --startup-project src/IMS.Api.Presentation

   - Apply migrations:
     dotnet ef database update --project src/IMS.Api.Infrastructure --startup-project src/IMS.Api.Presentation

Notes
- Program.cs reads connection string from configuration key `ConnectionStrings:DefaultConnection` or from the environment variable `ConnectionStrings__DefaultConnection`.
- Integration tests already use Testcontainers and start a PostgreSQL container for the test database.
