# Pgvector Scaffolding Docker Test
# Prerequisites: Docker Desktop running, dotnet ef tool (dotnet tool install -g dotnet-ef)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "=== Pgvector Scaffolding Test ===" -ForegroundColor Cyan
Write-Host ""

# 1. Start PostgreSQL with pgvector
Write-Host "1. Starting PostgreSQL (pgvector) in Docker..." -ForegroundColor Yellow
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") up -d

# Wait for DB to be ready
Write-Host "   Waiting for database to be ready..."
$maxAttempts = 30
$attempt = 0
do {
    Start-Sleep -Seconds 2
    $result = docker exec pgvector-scaffold-test pg_isready -U testuser -d pgvector_test 2>$null
    if ($LASTEXITCODE -eq 0) { break }
    $attempt++
    if ($attempt -ge $maxAttempts) {
        Write-Host "   Database failed to start." -ForegroundColor Red
        exit 1
    }
} while ($true)
Write-Host "   Database ready." -ForegroundColor Green
Write-Host ""

# 2. Run scaffolding (this tests our package!)
# Connection string from appsettings.json (same source as the sample app uses at runtime)
Write-Host "2. Running EF Core scaffolding (tests our package)..." -ForegroundColor Yellow
$appsettingsPath = Join-Path $ProjectRoot "sample\SampleApp\appsettings.json"
$connStr = (Get-Content $appsettingsPath | ConvertFrom-Json).ConnectionStrings.DefaultConnection
Push-Location sample\SampleApp
try {
    dotnet ef dbcontext scaffold $connStr Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Scaffolded --force --no-onconfiguring
    if ($LASTEXITCODE -ne 0) { exit 1 }
} finally {
    Pop-Location
}

# 3. Verify scaffolded output has Vector (not byte[])
$scaffoldedProduct = Get-Content "sample\SampleApp\Models\Scaffolded\Product.cs" -Raw
if ($scaffoldedProduct -match "Vector\?") {
    Write-Host "   Scaffolding SUCCESS: Product.Embedding is Vector? (correct)" -ForegroundColor Green
} elseif ($scaffoldedProduct -match "byte\[\]") {
    Write-Host "   Scaffolding FAILED: Product.Embedding is byte[] (wrong - package not working)" -ForegroundColor Red
    exit 1
} else {
    Write-Host "   Could not verify scaffolded output." -ForegroundColor Yellow
}
Write-Host ""

# 4. Run sample app (uses connection string from appsettings.json)
Write-Host "3. Running sample application..." -ForegroundColor Yellow
Push-Location sample\SampleApp
try {
    dotnet run
    if ($LASTEXITCODE -ne 0) { exit 1 }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=== All tests passed! ===" -ForegroundColor Green
Write-Host ""
Write-Host "To stop Docker: docker compose down" -ForegroundColor Gray
