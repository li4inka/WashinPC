param([switch]$Screenshots)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    $env:DOTNET_CLI_HOME = Join-Path $repo '.dotnet-home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    if ($Screenshots) { $env:WASHINPC_CAPTURE_PREVIEW = '1' }
    dotnet test tests/WashinPC.Tests/WashinPC.Tests.csproj -c Release --filter 'FullyQualifiedName~PagesRenderAndRetainSelectionAcrossNavigationAndLanguage' --logger 'trx;LogFileName=render.trx' --results-directory artifacts/test-results/render
    if ($LASTEXITCODE -ne 0) { throw 'WPF validation failed.' }
    if ($Screenshots) {
        Write-Host 'Captured actual WPF views in docs/assets and navigation samples in artifacts/measurements/final-navigation.csv.'
    }
} finally {
    Remove-Item Env:WASHINPC_CAPTURE_PREVIEW -ErrorAction SilentlyContinue
    Pop-Location
}
