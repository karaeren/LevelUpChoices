for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -ceq '--check') {
        $args[$i] = '--verify-no-changes'
    }
}

$solution = Join-Path $PSScriptRoot 'LevelUpChoices.sln'
dotnet format $solution @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet format style $solution --no-restore --severity info --diagnostics IDE0001 IDE0002 IDE0004 IDE0005 IDE0044 @args
exit $LASTEXITCODE
