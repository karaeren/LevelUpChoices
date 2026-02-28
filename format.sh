#!/bin/bash
# Runs the .NET built-in code formatter and linter

echo "Running dotnet format..."
dotnet format LevelUpChoices.sln
