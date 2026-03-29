Import-Module (Join-Path $PSScriptRoot '..\..\build\release\Release.Common.psm1') -Force

Describe 'Get-AspenBurnerVersion' {
    It 'reads semantic version from the app csproj' {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
        $csprojPath = Join-Path $repoRoot 'src\AspenBurner.App\AspenBurner.App.csproj'

        $version = Get-AspenBurnerVersion -CsprojPath $csprojPath

        $version | Should Be '0.6.0'
    }
}

Describe 'Get-ReleaseLayout' {
    It 'normalizes version tags and artifact names' {
        $layout = Get-ReleaseLayout -RepoRoot 'F:\repo' -Version '0.6.0'

        $layout.Version | Should Be '0.6.0'
        $layout.VersionTag | Should Be 'v0.6.0'
        $layout.ReleaseName | Should Be 'AspenBurner-v0.6.0-win-x64'
    }

    It 'roots portable and installer artifacts under versioned release directories' {
        $layout = Get-ReleaseLayout -RepoRoot 'F:\repo' -Version 'v0.6.0'

        $layout.PortableZipPath | Should Match 'artifacts\\releases\\v0\.6\.0\\AspenBurner-v0\.6\.0-win-x64-portable\.zip$'
        $layout.InstallerPath | Should Match 'artifacts\\releases\\v0\.6\.0\\AspenBurner-v0\.6\.0-win-x64-setup\.exe$'
    }
}
