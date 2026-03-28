Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# @brief Check whether the current process is running as administrator.
# @return True when elevated, otherwise false.
function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$currentScript = [System.IO.Path]::GetFullPath($PSCommandPath)
$targetScript = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'CrosshairOverlay.ps1'))

if (-not (Test-IsAdministrator)) {
    $args = @(
        '-NoProfile'
        '-WindowStyle'
        'Hidden'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        ('"{0}"' -f $currentScript)
    )

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $args
    exit
}

Get-CimInstance Win32_Process -Filter "name='powershell.exe'" |
    Where-Object {
        $_.CommandLine -like '*CrosshairOverlay.ps1*' -and
        $_.CommandLine -like ('*' + $targetScript + '*')
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force
    }
