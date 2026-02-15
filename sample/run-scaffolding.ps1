# Run EF Core scaffolding with pgvector support

# Change to SampleApp directory
Push-Location SampleApp

# Connection string for the sample database
$connectionString = "Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass"

# Run scaffolding
dotnet ef dbcontext scaffold $connectionString Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context SampleDbContext --force

Write-Host "Scaffolding completed. Check the Models directory for generated files."

# Return to original directory
Pop-Location