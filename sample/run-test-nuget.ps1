# Test Pgvector.EntityFrameworkCore.Scaffolding as a NuGet package (not project reference)
# Prerequisites: Docker Desktop, dotnet ef tool

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "=== Pgvector Scaffolding Test (as NuGet Package) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Pack the package
Write-Host "1. Packing Pgvector.EntityFrameworkCore.Scaffolding..." -ForegroundColor Yellow
$packOutput = Join-Path $ProjectRoot "bin\Release"
New-Item -ItemType Directory -Path $packOutput -Force | Out-Null
Push-Location $ProjectRoot
dotnet pack Pgvector.EntityFrameworkCore.Scaffolding.csproj -c Release -o $packOutput
$packResult = $LASTEXITCODE
Pop-Location
if ($packResult -ne 0) { exit 1 }
if ($LASTEXITCODE -ne 0) { exit 1 }
$nupkg = Get-ChildItem "bin\Release\*.nupkg" | Select-Object -First 1
Write-Host "   Created: $($nupkg.Name)" -ForegroundColor Green
Write-Host ""

# 2. Switch SampleApp to use package instead of project reference
$csprojPath = Join-Path $ProjectRoot "sample\SampleApp\SampleApp.csproj"
$csproj = Get-Content $csprojPath -Raw
$backupPath = "$csprojPath.bak"

# Replace ProjectReference with PackageReference
$restoreProjectRef = $false
$csprojPackage = $csproj -replace '<ProjectReference Include="\.\.\\\.\.\\Pgvector\.EntityFrameworkCore\.Scaffolding\.csproj" />', '<PackageReference Include="Pgvector.EntityFrameworkCore.Scaffolding" Version="1.0.0" />'
if ($csproj -eq $csprojPackage) {
    Write-Host "   SampleApp already uses PackageReference (or pattern changed)" -ForegroundColor Gray
} else {
    $csproj | Set-Content $backupPath -NoNewline
    $csprojPackage | Set-Content $csprojPath -NoNewline
    $restoreProjectRef = $true
}
Write-Host ""

try {
# 3. Start Docker
Write-Host "2. Starting PostgreSQL (pgvector) in Docker..." -ForegroundColor Yellow
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") up -d 2>$null
Write-Host "   Waiting for database..."
$maxAttempts = 30
$attempt = 0
do {
    Start-Sleep -Seconds 2
    $result = docker exec pgvector-scaffold-test pg_isready -U testuser -d pgvector_test 2>$null
    if ($LASTEXITCODE -eq 0) { break }
    $attempt++
    if ($attempt -ge $maxAttempts) {
        Write-Host "   Database failed to start." -ForegroundColor Red
        throw "Database failed to start"
    }
} while ($true)
Write-Host "   Database ready." -ForegroundColor Green
Write-Host ""

# 4. Restore and scaffold (package is used via nuget.config local source)
Write-Host "3. Restoring and running EF Core scaffolding..." -ForegroundColor Yellow
Push-Location sample\SampleApp
try {
    dotnet restore --configfile nuget.config
    $connStr = (Get-Content appsettings.json | ConvertFrom-Json).ConnectionStrings.DefaultConnection
    dotnet ef dbcontext scaffold $connStr Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Scaffolded --force --no-onconfiguring
    if ($LASTEXITCODE -ne 0) { throw "Scaffold failed" }
} finally {
    Pop-Location
}

# 5. Verify scaffolded output
$scaffoldedProduct = Get-Content "sample\SampleApp\Models\Scaffolded\Product.cs" -Raw
if ($scaffoldedProduct -match "Vector\?") {
    Write-Host "   Scaffolding SUCCESS: Product.Embedding is Vector? (correct)" -ForegroundColor Green
} elseif ($scaffoldedProduct -match "byte\[\]") {
    Write-Host "   Scaffolding FAILED: Product.Embedding is byte[] (wrong)" -ForegroundColor Red
    throw "Scaffolding failed"
}
Write-Host ""

# 6. Run sample app
Write-Host "4. Running sample application..." -ForegroundColor Yellow
Push-Location sample\SampleApp
try {
    dotnet run
    if ($LASTEXITCODE -ne 0) { throw "Run failed" }
} finally {
    Pop-Location
}

} finally {
    # Always restore ProjectReference on exit (success or failure)
    if ($restoreProjectRef) {
        Write-Host ""
        Write-Host "   Restoring ProjectReference in SampleApp..." -ForegroundColor Gray
        $csproj | Set-Content $csprojPath -NoNewline
        Remove-Item $backupPath -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "=== All tests passed! (Package tested successfully) ===" -ForegroundColor Green
Write-Host ""
Write-Host "To stop Docker: docker compose down" -ForegroundColor Gray
