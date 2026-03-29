Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# @brief Test whether the current PowerShell host is elevated.
# @return True when the current token is in the Administrators group.
function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# @brief Locate the preferred AspenBurner executable.
# @param RootPath Repository or install root that contains dist/ and src/.
# @return Absolute path to AspenBurner.exe.
function Get-AspenBurnerExecutablePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $candidates = @(
        (Join-Path $RootPath 'AspenBurner.exe')
        (Join-Path $RootPath 'src\AspenBurner.App\bin\Release\net8.0-windows\AspenBurner.exe')
        (Join-Path $RootPath 'dist\AspenBurner\AspenBurner.exe')
        (Join-Path $RootPath 'src\AspenBurner.App\bin\Debug\net8.0-windows\AspenBurner.exe')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw 'AspenBurner.exe was not found. Build or publish the desktop app first.'
}

# @brief Relaunch the current script with administrator rights.
# @param ScriptPath Current script path.
# @param ForwardArguments Argument list to pass through after elevation.
function Start-ElevatedPowerShellScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,

        [Parameter(Mandatory = $true)]
        [string[]]$ForwardArguments
    )

    $argumentList = @(
        '-NoProfile'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        ('"{0}"' -f $ScriptPath)
    ) + $ForwardArguments

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argumentList | Out-Null
}
