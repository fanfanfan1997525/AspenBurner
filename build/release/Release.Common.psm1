Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# @brief Read the AspenBurner application version from the desktop csproj.
# @param CsprojPath Absolute or relative path to AspenBurner.App.csproj.
# @return Semantic version string without a leading v.
function Get-AspenBurnerVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath
    )

    [xml]$project = Get-Content -LiteralPath $CsprojPath
    $versionNode = $project.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionNode)) {
        throw "Version was not found in $CsprojPath."
    }

    return $versionNode.Trim()
}

# @brief Build the conventional release layout rooted under artifacts/releases.
# @param RepoRoot Repository root.
# @param Version Semantic version string without a leading v.
# @return Layout object with stage and final artifact paths.
function Get-ReleaseLayout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $normalizedVersion = $Version.TrimStart('v')
    $versionTag = "v$normalizedVersion"
    $releaseRoot = Join-Path $RepoRoot "artifacts\releases\$versionTag"
    $stageRoot = Join-Path $releaseRoot 'stage'
    $portableRoot = Join-Path $stageRoot 'AspenBurner'
    $releaseName = "AspenBurner-$versionTag-win-x64"

    return [pscustomobject]@{
        Version          = $normalizedVersion
        VersionTag       = $versionTag
        ReleaseName      = $releaseName
        ReleaseRoot      = $releaseRoot
        StageRoot        = $stageRoot
        PortableRoot     = $portableRoot
        PortableZipPath  = Join-Path $releaseRoot "$releaseName-portable.zip"
        InstallerPath    = Join-Path $releaseRoot "$releaseName-setup.exe"
        InnoScriptPath   = Join-Path $RepoRoot 'build\release\AspenBurner.iss'
    }
}

# @brief Locate the Inno Setup command-line compiler.
# @return Absolute path to ISCC.exe.
function Get-InnoSetupCompilerPath {
    [CmdletBinding()]
    param()

    $knownPaths = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
        'C:\Program Files\Inno Setup 6\ISCC.exe'
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )

    foreach ($knownPath in $knownPaths) {
        if (Test-Path -LiteralPath $knownPath) {
            return [System.IO.Path]::GetFullPath($knownPath)
        }
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return [System.IO.Path]::GetFullPath($command.Source)
    }

    throw 'ISCC.exe was not found. Install Inno Setup 6 first.'
}

Export-ModuleMember -Function Get-AspenBurnerVersion, Get-ReleaseLayout, Get-InnoSetupCompilerPath
