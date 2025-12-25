param(
    [string]$ContainerName = "ims-postgres",
    [string]$Image = "postgres:15-alpine",
    [int]$HostPort = 5433,
    [string]$DbName = "imsdb",
    [string]$DbUser = "ims",
    [string]$DbPassword = "ims_pass",
    [string]$MigrationName = "",
    [string]$ProjectPath = "src/IMS.Api.Infrastructure",
    [string]$StartupProjectPath = "src/IMS.Api.Presentation"
)

Write-Host "This script will start a PostgreSQL container and run EF Core migrations against it."

# Check Docker is available
try {
    docker version > $null 2>&1
} catch {
    Write-Error "Docker is required to run this script. Start Docker Desktop or install Docker CLI and daemon."
    exit 1
}

# If container already running, reuse it
$existing = docker ps -a --filter "name=$ContainerName" --format "{{.Names}}"
if ($existing -eq $ContainerName) {
    Write-Host "Removing existing container '$ContainerName'..."
    docker rm -f $ContainerName | Out-Null
}

Write-Host "Starting PostgreSQL container '$ContainerName' (host port $HostPort)..."
docker run -d --name $ContainerName -e POSTGRES_DB=$DbName -e POSTGRES_USER=$DbUser -e POSTGRES_PASSWORD=$DbPassword -p ${HostPort}:5432 $Image | Out-Null

# Wait for Postgres to be ready
Write-Host "Waiting for PostgreSQL to be ready..."
$tries = 0
$maxTries = 30
$ready = $false
while ($tries -lt $maxTries -and -not $ready) {
    Start-Sleep -Seconds 1
    try {
        docker exec $ContainerName pg_isready -U $DbUser > $null 2>&1
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    } catch { }
    $tries++
}

if (-not $ready) {
    Write-Error "PostgreSQL did not become ready in time. Check Docker logs: docker logs $ContainerName"
    exit 1
}

$connectionString = "Host=localhost;Port=$HostPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"
Write-Host "Using connection string: $connectionString"

# Set environment variable for dotnet ef
$env:ConnectionStrings__DefaultConnection = $connectionString

# Optional: add a new migration
if (![string]::IsNullOrWhiteSpace($MigrationName)) {
    Write-Host "Adding migration '$MigrationName'..."
    dotnet ef migrations add $MigrationName --project $ProjectPath --startup-project $StartupProjectPath
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to add migration"; exit 1 }
}

# Run database update
Write-Host "Applying migrations (database update)..."
dotnet ef database update --project $ProjectPath --startup-project $StartupProjectPath
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to apply migrations"; exit 1 }

Write-Host "Database created and migrations applied successfully. Container: $ContainerName"
Write-Host "To stop and remove the container: docker rm -f $ContainerName"
