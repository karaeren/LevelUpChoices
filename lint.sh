#!/bin/bash
# Checks the code format without modifying files. Useful for CI.

echo "Verifying code format..."
dotnet format LevelUpChoices.sln --verify-no-changes
