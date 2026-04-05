param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$dotnetHome = Join-Path $PSScriptRoot "..\.dotnet-home"
$env:DOTNET_CLI_HOME = [IO.Path]::GetFullPath($dotnetHome)
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"

Write-Host "[smoke] restore app (offline feed)"
dotnet restore .\washinpc.csproj --configfile .\NuGet.config
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] restore tests (offline feed)"
dotnet restore .\tests\WashinPC.Tests\WashinPC.Tests.csproj --configfile .\NuGet.config
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] build app"
dotnet build .\washinpc.csproj -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] build tests"
dotnet build .\tests\WashinPC.Tests\WashinPC.Tests.csproj -c $Configuration --no-restore /p:BuildProjectReferences=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] test"
dotnet test .\tests\WashinPC.Tests\WashinPC.Tests.csproj -c $Configuration --no-build
exit $LASTEXITCODE
