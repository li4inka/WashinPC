param([ValidateSet('x86','x64')][string]$Architecture = 'x64')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    $env:DOTNET_CLI_HOME = Join-Path $repo '.dotnet-home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
    dotnet restore WashinPC.sln --configfile NuGet.config --locked-mode "-p:PlatformTarget=$Architecture"
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build WashinPC.sln -t:Rebuild -c Release --no-restore "-p:PlatformTarget=$Architecture"
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $results = "artifacts/test-results/$Architecture"
    dotnet test tests/WashinPC.Tests/WashinPC.Tests.csproj -c Release --no-build "-p:PlatformTarget=$Architecture" --results-directory $results --logger 'trx;LogFileName=tests.trx' -- "RunConfiguration.TargetPlatform=$Architecture"
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed; no archive created.' }
    $version = (Get-Content version.json -Raw | ConvertFrom-Json).version
    $name = "WashinPC-$version-win-$Architecture"
    $stage = Join-Path $repo ("artifacts/staging/" + [Guid]::NewGuid().ToString('N') + '/' + $name)
    New-Item -ItemType Directory -Force $stage | Out-Null
    $binary = Join-Path $repo 'src/bin/Release/net48/WashinPC.exe'
    $bytes = [IO.File]::ReadAllBytes($binary)
    $pe = [BitConverter]::ToInt32($bytes,0x3c)
    $machine = [BitConverter]::ToUInt16($bytes,$pe+4)
    $expected = if ($Architecture -eq 'x64') { 0x8664 } else { 0x14c }
    if ($machine -ne $expected) { throw 'Wrong PE architecture.' }
    $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($binary).ProductVersion
    if (!$productVersion.StartsWith($version + '+')) { throw 'Executable version does not match version.json.' }
    Copy-Item -LiteralPath $binary -Destination $stage
    $runtimeDirectory = Split-Path -Parent $binary
    Get-ChildItem -LiteralPath $runtimeDirectory -Filter '*.dll' -File | Copy-Item -Destination $stage
    foreach ($nativeArchitecture in @('x86','x64')) {
        $nativeDirectory = Join-Path $runtimeDirectory $nativeArchitecture
        $nativeDll = Join-Path $nativeDirectory 'SQLite.Interop.dll'
        if (!(Test-Path -LiteralPath $nativeDll)) { throw "Missing SQLite native runtime: $nativeArchitecture" }
        Copy-Item -LiteralPath $nativeDirectory -Destination $stage -Recurse
    }
    if (!(Test-Path -LiteralPath (Join-Path $stage 'System.Data.SQLite.dll'))) { throw 'Missing managed SQLite runtime.' }
    $config = $binary + '.config'
    if (Test-Path $config) { Copy-Item -LiteralPath $config -Destination $stage }
    $assets = Join-Path $repo 'src/bin/Release/net48/Assets'
    if (Test-Path $assets) { Copy-Item -LiteralPath $assets -Destination $stage -Recurse }
    foreach ($file in @('LICENSE','THIRD_PARTY_NOTICES.md','README.md','README.ru.md','CHANGELOG.md')) { Copy-Item -LiteralPath $file -Destination $stage }
    Copy-Item -LiteralPath docs -Destination $stage -Recurse
    New-Item -ItemType Directory -Force (Join-Path $stage 'licenses') | Out-Null
    Copy-Item -LiteralPath src/Assets/Fonts/Inter/OFL.txt -Destination (Join-Path $stage 'licenses/Inter-OFL.txt')
    Copy-Item -LiteralPath docs/licenses/lucide-ISC.txt -Destination (Join-Path $stage 'licenses/lucide-ISC.txt')
    Copy-Item -LiteralPath docs/licenses/SQLite.txt -Destination (Join-Path $stage 'licenses/SQLite.txt')
    $out = Join-Path $repo 'artifacts/portable'
    New-Item -ItemType Directory -Force $out | Out-Null
    $zip = Join-Path $out ($name + '.zip')
    Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
    $hash = Get-FileHash -LiteralPath $zip -Algorithm SHA256
    [IO.File]::WriteAllText(($zip + '.sha256'), $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($zip) + "`n", [Text.Encoding]::ASCII)
    Write-Host "Created $zip"
    Write-Host "SHA256: $($hash.Hash)"
} finally { Pop-Location }
