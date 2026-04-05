param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$dotnetHome = Join-Path $PSScriptRoot "..\.dotnet-home"
$env:DOTNET_CLI_HOME = [IO.Path]::GetFullPath($dotnetHome)
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"

Write-Host "[smoke] dotnet build WashinPC.sln -c $Configuration"
dotnet build WashinPC.sln -c $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] dotnet test WashinPC.sln -c $Configuration --no-build"
dotnet test WashinPC.sln -c $Configuration --no-build
exit $LASTEXITCODE
