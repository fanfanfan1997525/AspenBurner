param(
    [string]$ConfigPath = '',
    [int]$AutoCloseMs = 0,
    [switch]$Elevated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'AspenBurner.Common.ps1')

$launcherScript = [System.IO.Path]::GetFullPath($PSCommandPath)
$resolvedConfigInputPath = if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    Join-Path $PSScriptRoot 'config\crosshair.json'
}
else {
    $ConfigPath
}
$resolvedConfigPath = [System.IO.Path]::GetFullPath($resolvedConfigInputPath)

if (-not $Elevated -and -not (Test-IsAdministrator)) {
    Start-ElevatedPowerShellScript -ScriptPath $launcherScript -ForwardArguments @(
        '-ConfigPath'
        ('"{0}"' -f $resolvedConfigPath)
        '-AutoCloseMs'
        $AutoCloseMs
        '-Elevated'
    )
    exit
}

if ($Elevated -and -not (Test-IsAdministrator)) {
    throw 'Elevation was requested but administrator rights were not acquired.'
}

$executablePath = Get-AspenBurnerExecutablePath -RootPath $PSScriptRoot
$arguments = @('-ConfigPath', $resolvedConfigPath, '--show-settings')

if ($AutoCloseMs -gt 0) {
    Start-Process -FilePath $executablePath -ArgumentList $arguments -Wait | Out-Null
    Start-Sleep -Milliseconds $AutoCloseMs
    exit
}

Start-Process -FilePath $executablePath -ArgumentList $arguments | Out-Null
