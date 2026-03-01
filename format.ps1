# Runs the .NET built-in code formatter and linter

Write-Host "Running dotnet format..."
dotnet format LevelUpChoices.sln --severity info
