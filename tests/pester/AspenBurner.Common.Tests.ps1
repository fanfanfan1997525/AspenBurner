$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'AspenBurner.Common.ps1')

Describe 'Get-AspenBurnerExecutablePath' {
    It 'prefers installed-root executable when present' {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $root | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'AspenBurner.exe') | Out-Null

        try {
            $resolved = Get-AspenBurnerExecutablePath -RootPath $root
            $resolved | Should Be ([System.IO.Path]::GetFullPath((Join-Path $root 'AspenBurner.exe')))
        }
        finally {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }

    It 'falls back to dist payload when installed-root executable is absent' {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
        $distRoot = Join-Path $root 'dist\AspenBurner'
        New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $distRoot 'AspenBurner.exe') | Out-Null

        try {
            $resolved = Get-AspenBurnerExecutablePath -RootPath $root
            $resolved | Should Be ([System.IO.Path]::GetFullPath((Join-Path $distRoot 'AspenBurner.exe')))
        }
        finally {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
}
