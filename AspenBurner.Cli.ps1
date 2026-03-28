param(
    [Parameter(Position = 0)]
    [ValidateSet('start', 'configure', 'preview', 'stop')]
    [string]$Command = 'start',

    [string]$ConfigPath = '',

    [int]$PreviewSeconds = 8
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'AspenBurner.Common.ps1')

$resolvedConfigInputPath = if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    Join-Path $PSScriptRoot 'config\crosshair.json'
}
else {
    $ConfigPath
}

$resolvedConfigPath = [System.IO.Path]::GetFullPath($resolvedConfigInputPath)
$executablePath = Get-AspenBurnerExecutablePath -RootPath $PSScriptRoot

$arguments = switch ($Command) {
    'start' {
        @('--config-path', $resolvedConfigPath, '--resume')
    }
    'configure' {
        @('--config-path', $resolvedConfigPath, '--show-settings')
    }
    'preview' {
        @('--config-path', $resolvedConfigPath, '--preview', '--preview-seconds', $PreviewSeconds)
    }
    'stop' {
        @('--stop')
    }
}

if ($Command -eq 'stop') {
    Start-Process -FilePath $executablePath -ArgumentList $arguments -Wait | Out-Null
}
else {
    Start-Process -FilePath $executablePath -ArgumentList $arguments | Out-Null
}
