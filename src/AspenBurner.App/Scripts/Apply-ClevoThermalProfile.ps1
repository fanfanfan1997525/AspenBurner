param(
    [Parameter(Mandatory = $true)]
    [string]$Cc40Path,

    [Parameter(Mandatory = $true)]
    [string]$PowerPlanGuid,

    [Parameter(Mandatory = $true)]
    [string]$PowerModeAutomationId,

    [Parameter(Mandatory = $true)]
    [string]$FanModeAutomationId,

    [Parameter(Mandatory = $true)]
    [string]$GpuSwitchAutomationId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient | Out-Null
Add-Type -AssemblyName UIAutomationTypes | Out-Null

function Get-OrStartCc40Process {
    $existing = Get-Process -Name 'CC40' -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } |
        Select-Object -First 1
    if ($existing) {
        return $existing
    }

    return Start-Process -FilePath $Cc40Path -PassThru
}

function Wait-Cc40Window {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,

        [int]$TimeoutSeconds = 20
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $Process.Refresh()
        if ($Process.MainWindowHandle -ne 0) {
            return [System.Windows.Automation.AutomationElement]::FromHandle($Process.MainWindowHandle)
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "CC40 window did not appear within $TimeoutSeconds seconds."
}

function Get-Cc40Control {
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Automation.AutomationElement]$Window,

        [Parameter(Mandatory = $true)]
        [string]$AutomationId
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId
    )

    $element = $Window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $element) {
        throw "CC40 control not found: $AutomationId"
    }

    return $element
}

function Select-Cc40Control {
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Automation.AutomationElement]$Window,

        [Parameter(Mandatory = $true)]
        [string]$AutomationId
    )

    $element = Get-Cc40Control -Window $Window -AutomationId $AutomationId
    try {
        $pattern = $element.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        if ($pattern) {
            $pattern.Select()
            return
        }
    } catch {
    }

    try {
        $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        if ($pattern) {
            $pattern.Invoke()
            return
        }
    } catch {
    }

    throw "CC40 control cannot be selected: $AutomationId"
}

function Invoke-Cc40ApplyButton {
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Automation.AutomationElement]$Window
    )

    $buttonCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button
    )
    $buttons = $Window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
    foreach ($button in $buttons) {
        if ($button.Current.Name -notin @('Apply', 'Save', 'Apply Changes')) {
            continue
        }

        try {
            $pattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            if ($pattern) {
                $pattern.Invoke()
                return
            }
        } catch {
        }
    }
}

& powercfg.exe /s $PowerPlanGuid | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "powercfg.exe returned exit code $LASTEXITCODE"
}

$process = Get-OrStartCc40Process
$window = Wait-Cc40Window -Process $process

Select-Cc40Control -Window $window -AutomationId $PowerModeAutomationId
Select-Cc40Control -Window $window -AutomationId $FanModeAutomationId
Select-Cc40Control -Window $window -AutomationId $GpuSwitchAutomationId
Invoke-Cc40ApplyButton -Window $window

Write-Output "Applied Clevo thermal profile via $PowerPlanGuid."
