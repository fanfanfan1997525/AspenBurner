param(
    [string]$Version = '',
    [switch]$SkipInstaller,
    [switch]$SkipPortableZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Release.Common.psm1') -Force

# @brief Ensure a directory exists and is empty before staging release assets.
# @param Path Target directory path.
function Reset-Directory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

# @brief Copy repository-managed runtime assets into the staged portable directory.
# @param RepoRoot Repository root.
# @param PortableRoot Staged portable app directory.
function Copy-ReleaseAssets {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$PortableRoot
    )

    $filesToCopy = @(
        'AspenBurner.Common.ps1'
        'AspenBurner.Cli.cmd'
        'AspenBurner.Cli.ps1'
        'Start-Crosshair.cmd'
        'Start-Crosshair.ps1'
        'Configure-Crosshair.cmd'
        'Configure-Crosshair.ps1'
        'Stop-Crosshair.cmd'
        'Stop-Crosshair.ps1'
        'README.md'
    )

    foreach ($relativePath in $filesToCopy) {
        Copy-Item -LiteralPath (Join-Path $RepoRoot $relativePath) -Destination (Join-Path $PortableRoot $relativePath) -Force
    }

    Copy-Item -LiteralPath (Join-Path $RepoRoot 'config') -Destination (Join-Path $PortableRoot 'config') -Recurse -Force
    New-Item -ItemType Directory -Path (Join-Path $PortableRoot 'logs') -Force | Out-Null
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$csprojPath = Join-Path $repoRoot 'src\AspenBurner.App\AspenBurner.App.csproj'
$resolvedVersion = if ([string]::IsNullOrWhiteSpace($Version)) {
    Get-AspenBurnerVersion -CsprojPath $csprojPath
}
else {
    $Version.TrimStart('v')
}

$layout = Get-ReleaseLayout -RepoRoot $repoRoot -Version $resolvedVersion
Reset-Directory -Path $layout.ReleaseRoot
New-Item -ItemType Directory -Path $layout.StageRoot -Force | Out-Null

$publishArguments = @(
    'publish'
    $csprojPath
    '-c'
    'Release'
    '-r'
    'win-x64'
    '--self-contained'
    'true'
    '-o'
    $layout.PortableRoot
    "-p:Version=$resolvedVersion"
    "-p:AssemblyVersion=$resolvedVersion.0"
    "-p:FileVersion=$resolvedVersion.0"
    '-p:PublishSingleFile=true'
    '-p:IncludeNativeLibrariesForSelfExtract=true'
    '-p:DebugType=None'
    '-p:DebugSymbols=false'
)

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-ReleaseAssets -RepoRoot $repoRoot -PortableRoot $layout.PortableRoot

if (-not $SkipPortableZip) {
    if (Test-Path -LiteralPath $layout.PortableZipPath) {
        Remove-Item -LiteralPath $layout.PortableZipPath -Force
    }

    Compress-Archive -Path (Join-Path $layout.PortableRoot '*') -DestinationPath $layout.PortableZipPath -CompressionLevel Optimal
}

if (-not $SkipInstaller) {
    $isccPath = Get-InnoSetupCompilerPath
    & $isccPath `
        "/DAppVersion=$($layout.Version)" `
        "/DReleaseName=$($layout.ReleaseName)" `
        "/DSourceDir=$($layout.PortableRoot)" `
        "/DOutputDir=$($layout.ReleaseRoot)" `
        $layout.InnoScriptPath

    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed with exit code $LASTEXITCODE."
    }

    $expectedInstallerPath = Join-Path $layout.ReleaseRoot "$($layout.ReleaseName)-setup.exe"
    if (-not (Test-Path -LiteralPath $expectedInstallerPath)) {
        throw "Installer output was not found at $expectedInstallerPath."
    }
}

Write-Host "Release root: $($layout.ReleaseRoot)"
Write-Host "Portable:    $($layout.PortableZipPath)"
if (-not $SkipInstaller) {
    Write-Host "Installer:   $($layout.InstallerPath)"
}
