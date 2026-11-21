#!/bin/sh

for argument do
    shift
    if [ "$argument" = --check ]; then
        argument=--verify-no-changes
    fi
    set -- "$@" "$argument"
done

solution="$(dirname -- "$0")/LevelUpChoices.sln"
dotnet format "$solution" "$@" || exit $?

exec dotnet format style "$solution" --no-restore --severity info --diagnostics IDE0001 IDE0002 IDE0004 IDE0005 IDE0044 "$@"
