# Checks the code format without modifying files. Useful for CI.

Write-Host "Verifying code format..."
dotnet format LevelUpChoices.sln --verify-no-changes
