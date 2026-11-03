param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$dotnetHome = Join-Path $repoRoot ".dotnet-home"
$env:DOTNET_CLI_HOME = [IO.Path]::GetFullPath($dotnetHome)
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"

$appProject = Join-Path $repoRoot "src/washinpc.csproj"
$testProject = Join-Path $repoRoot "tests/WashinPC.Tests/WashinPC.Tests.csproj"
$nugetConfig = Join-Path $repoRoot "NuGet.config"

Write-Host "[smoke] restore app (offline feed)"
dotnet restore $appProject --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] restore tests (offline feed)"
dotnet restore $testProject --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] build app"
dotnet build $appProject -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] build tests"
dotnet build $testProject -c $Configuration --no-restore /p:BuildProjectReferences=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[smoke] test"
dotnet test $testProject -c $Configuration --no-build
exit $LASTEXITCODE
