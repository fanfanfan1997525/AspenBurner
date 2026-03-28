Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'AspenBurner.Common.ps1')

$currentScript = [System.IO.Path]::GetFullPath($PSCommandPath)

if (-not (Test-IsAdministrator)) {
    Start-ElevatedPowerShellScript -ScriptPath $currentScript -ForwardArguments @()
    exit
}

$executablePath = Get-AspenBurnerExecutablePath -RootPath $PSScriptRoot
Start-Process -FilePath $executablePath -ArgumentList @('--stop') -Wait | Out-Null
