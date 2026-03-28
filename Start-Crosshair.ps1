param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config\crosshair.json'),
    [switch]$Elevated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$launcherScript = [System.IO.Path]::GetFullPath($PSCommandPath)
$overlayScript = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'CrosshairOverlay.ps1'))
$resolvedConfigPath = [System.IO.Path]::GetFullPath($ConfigPath)

if (-not $Elevated -and -not (Test-IsAdministrator)) {
    $args = @(
        '-NoProfile'
        '-WindowStyle'
        'Hidden'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        ('"{0}"' -f $launcherScript)
        '-ConfigPath'
        ('"{0}"' -f $resolvedConfigPath)
        '-Elevated'
    )

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $args
    exit
}

if ($Elevated -and -not (Test-IsAdministrator)) {
    throw 'Elevation was requested but administrator rights were not acquired.'
}

Get-CimInstance Win32_Process -Filter "name='powershell.exe'" |
    Where-Object {
        $_.CommandLine -like '*CrosshairOverlay.ps1*' -and
        $_.CommandLine -like ('*' + $overlayScript + '*')
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force
    }

$overlayArgs = @(
    '-NoProfile'
    '-WindowStyle'
    'Hidden'
    '-ExecutionPolicy'
    'Bypass'
    '-File'
    ('"{0}"' -f $overlayScript)
    '-ConfigPath'
    ('"{0}"' -f $resolvedConfigPath)
)

Start-Process -FilePath 'powershell.exe' -ArgumentList $overlayArgs -WindowStyle Hidden
