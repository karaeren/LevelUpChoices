# Runs the .NET built-in code formatter and linter

./format.ps1
./lint.ps1

Write-Host "Building project..."
dotnet build -c Release
