param(
    [string]$Version = "",
    [string]$Changelog = "Neue Version des ZODIAC SIX Launchers.",
    [switch]$Mandatory,
    [switch]$SelfContained = $true,
    [string]$WebRepoPath = "C:\Users\xBeiros\Herd\zodiac",
    [string]$BaseUrl = "https://zodiac.duran.li/launcher",
    [switch]$GitPush
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$CsprojPath = Join-Path $ProjectDir "WpfApp1.csproj"
$PublishDir = Join-Path $ProjectDir "bin\Release\publish-win-x64"

$ManifestPath = Join-Path $WebRepoPath "public\launcher\version.json"
$ExistingManifest = $null
if (Test-Path $ManifestPath) {
    try { $ExistingManifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json } catch {}
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ($ExistingManifest -and -not [string]::IsNullOrWhiteSpace($ExistingManifest.version)) {
        $Version = $ExistingManifest.version
    } else {
        $Version = "1.0.0"
    }
}

if (-not $PSBoundParameters.ContainsKey('Changelog') -and $ExistingManifest -and -not [string]::IsNullOrWhiteSpace($ExistingManifest.changelog)) {
    $Changelog = $ExistingManifest.changelog
}

Write-Host "Building ZodiacSix-Launcher v$Version (SelfContained=$SelfContained)..." -ForegroundColor Cyan

$scArg = if ($SelfContained) { "true" } else { "false" }
dotnet publish $CsprojPath `
    -c Release `
    -r win-x64 `
    --self-contained $scArg `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$ExePath = Join-Path $PublishDir "ZodiacSix-Launcher.exe"
if (-not (Test-Path $ExePath)) { throw "Published executable not found at $ExePath" }

$FileInfo = Get-Item $ExePath
$SizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
$Hash = (Get-FileHash -Path $ExePath -Algorithm SHA256).Hash

Write-Host "Built: $ExePath ($SizeMB MB)" -ForegroundColor Green
Write-Host "SHA-256: $Hash" -ForegroundColor Gray

$TargetDir = Join-Path $WebRepoPath "public\launcher"
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

$TargetExe = Join-Path $TargetDir "ZodiacSix-Launcher.exe"
Copy-Item -Path $ExePath -Destination $TargetExe -Force

$Manifest = [ordered]@{
    version     = $Version
    downloadUrl = "$BaseUrl/ZodiacSix-Launcher.exe"
    changelog   = $Changelog
    sha256      = $Hash
    mandatory   = [bool]$Mandatory
}
$ManifestPath = Join-Path $TargetDir "version.json"
$Manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $ManifestPath -Encoding UTF8

Write-Host "Deployed to $TargetDir" -ForegroundColor Green

if ($GitPush) {
    Push-Location $WebRepoPath
    try {
        git add public/launcher/ZodiacSix-Launcher.exe public/launcher/version.json
        git commit -m "chore(launcher): release ZodiacSix-Launcher v$Version"
        git push origin main
    } finally {
        Pop-Location
    }
}
