param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config\crosshair.json'),
    [int]$AutoCloseMs = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Import-Module (Join-Path $PSScriptRoot 'src\CrosshairOverlay.Core.psm1') -Force

$script:Editors = @{}
$script:IsApplyingState = $false
$script:PreviewStatusBounds = [System.Drawing.Rectangle]::Empty
$script:IsDraggingStatus = $false
$script:StatusDragOffset = [System.Drawing.Point]::Empty

# @brief Load and validate the crosshair configuration, falling back to defaults on failure.
# @param ConfigFile Path to the JSON config file.
# @return A validated configuration object.
function Get-InitialCrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFile
    )

    try {
        $defaultConfig = Get-CrosshairDefaultConfig
        $overrideConfig = $null

        if (Test-Path -LiteralPath $ConfigFile) {
            $overrideConfig = Get-Content -Raw -LiteralPath $ConfigFile | ConvertFrom-Json
        }

        $config = Merge-CrosshairConfig -DefaultConfig $defaultConfig -OverrideConfig $overrideConfig
        Assert-CrosshairConfig -Config $config
        return $config
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show(
            "Config load failed. Defaults were restored.`r`n`r`n$($_.Exception.Message)",
            'crosshair-overlay',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        ) | Out-Null

        return Get-CrosshairDefaultConfig
    }
}

# @brief Clone the current config using the default property order.
# @param Config Source configuration object.
# @return Ordered hashtable ready for JSON serialization.
function ConvertTo-OrderedCrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $ordered = [ordered]@{}
    foreach ($property in (Get-CrosshairDefaultConfig).PSObject.Properties) {
        $ordered[$property.Name] = $Config.$($property.Name)
    }

    return $ordered
}

# @brief Persist the config to disk in JSON format.
# @param ConfigFile Path to the JSON config file.
# @param Config Configuration object to save.
function Save-CrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFile,

        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    Assert-CrosshairConfig -Config $Config

    $orderedConfig = ConvertTo-OrderedCrosshairConfig -Config $Config
    $json = $orderedConfig | ConvertTo-Json -Depth 4
    Set-Content -LiteralPath $ConfigFile -Value $json -Encoding UTF8
}

# @brief Create a mutable clone of the current config.
# @param Config Source configuration object.
# @return Mutable configuration object.
function New-MutableConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    return [pscustomobject](ConvertTo-OrderedCrosshairConfig -Config $Config)
}

# @brief Create a titled slider editor with a synced numeric box.
# @param LabelText Row title.
# @param Minimum Minimum value.
# @param Maximum Maximum value.
# @param Value Initial value.
# @param OnChanged Callback invoked with the new integer value.
# @return Editor controls packed into an object.
function New-SliderEditor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$LabelText,

        [Parameter(Mandatory = $true)]
        [int]$Minimum,

        [Parameter(Mandatory = $true)]
        [int]$Maximum,

        [Parameter(Mandatory = $true)]
        [int]$Value,

        [Parameter(Mandatory = $true)]
        [scriptblock]$OnChanged
    )

    $panel = [System.Windows.Forms.Panel]::new()
    $panel.Width = 360
    $panel.Height = 58
    $panel.Margin = [System.Windows.Forms.Padding]::new(6)

    $label = [System.Windows.Forms.Label]::new()
    $label.Text = $LabelText
    $label.Location = [System.Drawing.Point]::new(0, 6)
    $label.Width = 120

    $trackBar = [System.Windows.Forms.TrackBar]::new()
    $trackBar.Minimum = $Minimum
    $trackBar.Maximum = $Maximum
    $trackBar.TickStyle = [System.Windows.Forms.TickStyle]::None
    $trackBar.SmallChange = 1
    $trackBar.LargeChange = [Math]::Max([int](($Maximum - $Minimum) / 10), 1)
    $trackBar.Width = 190
    $trackBar.Location = [System.Drawing.Point]::new(0, 24)
    $trackBar.Value = [Math]::Min([Math]::Max($Value, $Minimum), $Maximum)

    $numeric = [System.Windows.Forms.NumericUpDown]::new()
    $numeric.Minimum = $Minimum
    $numeric.Maximum = $Maximum
    $numeric.Width = 72
    $numeric.Location = [System.Drawing.Point]::new(208, 24)
    $numeric.Value = $trackBar.Value

    $trackBar.Add_ValueChanged({
        if ($script:IsApplyingState) {
            return
        }

        if ([int]$numeric.Value -ne $trackBar.Value) {
            $numeric.Value = $trackBar.Value
        }

        & $OnChanged $trackBar.Value
    }.GetNewClosure())

    $numeric.Add_ValueChanged({
        if ($script:IsApplyingState) {
            return
        }

        $value = [int]$numeric.Value
        if ($trackBar.Value -ne $value) {
            $trackBar.Value = $value
            return
        }

        & $OnChanged $value
    }.GetNewClosure())

    $panel.Controls.AddRange(@($label, $trackBar, $numeric))

    return [pscustomobject]@{
        Container = $panel
        Label     = $label
        TrackBar  = $trackBar
        Numeric   = $numeric
    }
}

# @brief Create a combo-box editor row.
# @param LabelText Row title.
# @param Items Choices for the combo box.
# @param Value Initial selection.
# @param OnChanged Callback invoked with the new value.
# @return Editor controls packed into an object.
function New-ComboEditor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$LabelText,

        [Parameter(Mandatory = $true)]
        [string[]]$Items,

        [Parameter(Mandatory = $true)]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [scriptblock]$OnChanged
    )

    $panel = [System.Windows.Forms.Panel]::new()
    $panel.Width = 360
    $panel.Height = 42
    $panel.Margin = [System.Windows.Forms.Padding]::new(6)

    $label = [System.Windows.Forms.Label]::new()
    $label.Text = $LabelText
    $label.Location = [System.Drawing.Point]::new(0, 10)
    $label.Width = 120

    $combo = [System.Windows.Forms.ComboBox]::new()
    $combo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $combo.Location = [System.Drawing.Point]::new(128, 6)
    $combo.Width = 150
    [void]$combo.Items.AddRange($Items)
    $combo.SelectedItem = $Value

    $combo.Add_SelectedIndexChanged({
        if ($script:IsApplyingState) {
            return
        }

        if ($null -ne $combo.SelectedItem) {
            & $OnChanged ([string]$combo.SelectedItem)
        }
    }.GetNewClosure())

    $panel.Controls.AddRange(@($label, $combo))

    return [pscustomobject]@{
        Container = $panel
        Label     = $label
        Combo     = $combo
    }
}

# @brief Create a checkbox editor row.
# @param Text Checkbox caption.
# @param Value Initial state.
# @param OnChanged Callback invoked with the new value.
# @return Editor controls packed into an object.
function New-CheckEditor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [bool]$Value,

        [Parameter(Mandatory = $true)]
        [scriptblock]$OnChanged
    )

    $checkBox = [System.Windows.Forms.CheckBox]::new()
    $checkBox.Text = $Text
    $checkBox.Width = 320
    $checkBox.Height = 28
    $checkBox.Margin = [System.Windows.Forms.Padding]::new(6)
    $checkBox.Checked = $Value

    $checkBox.Add_CheckedChanged({
        if ($script:IsApplyingState) {
            return
        }

        & $OnChanged $checkBox.Checked
    }.GetNewClosure())

    return [pscustomobject]@{
        Container = $checkBox
        CheckBox  = $checkBox
    }
}

# @brief Build the sample status text used by the preview panel.
# @return Sample display string.
function Get-PreviewStatusText {
    [CmdletBinding()]
    param()

    return Format-StatusOverlayText `
        -FrequencyMHz 4200 `
        -TemperatureC 95 `
        -ApproximateTemperature $false `
        -ShowTemperature $script:ConfigState.StatusShowTemperature
}

# @brief Compute the preview-space bounds for the draggable status label.
# @param PreviewPanel Preview panel control.
# @return Bounds for the status label inside the preview.
function Get-PreviewStatusBounds {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Forms.Control]$PreviewPanel
    )

    $statusText = Get-PreviewStatusText
    $font = [System.Drawing.Font]::new('Consolas', [float]$script:ConfigState.StatusFontSize, [System.Drawing.FontStyle]::Bold)
    try {
        $flags = [System.Windows.Forms.TextFormatFlags]::NoPadding -bor [System.Windows.Forms.TextFormatFlags]::SingleLine
        $textSize = [System.Windows.Forms.TextRenderer]::MeasureText($statusText, $font, [System.Drawing.Size]::new(1000, 1000), $flags)
        $overlayWidth = $textSize.Width + 12
        $overlayHeight = $textSize.Height + 8

        return Get-StatusOverlayBounds `
            -AreaLeft 0 `
            -AreaTop 0 `
            -AreaWidth $PreviewPanel.ClientSize.Width `
            -AreaHeight $PreviewPanel.ClientSize.Height `
            -OverlayWidth $overlayWidth `
            -OverlayHeight $overlayHeight `
            -Position $script:ConfigState.StatusPosition `
            -OffsetX $script:ConfigState.StatusOffsetX `
            -OffsetY $script:ConfigState.StatusOffsetY
    }
    finally {
        $font.Dispose()
    }
}

# @brief Enable or disable dependent controls based on the current state.
function Update-ControlStates {
    [CmdletBinding()]
    param()

    $isCustomColor = ($script:ConfigState.Color -eq 'Custom')
    foreach ($editorName in @('ColorR', 'ColorG', 'ColorB')) {
        $script:Editors[$editorName].TrackBar.Enabled = $isCustomColor
        $script:Editors[$editorName].Numeric.Enabled = $isCustomColor
        $script:Editors[$editorName].Label.Enabled = $isCustomColor
    }

    $isStatusEnabled = $script:ConfigState.StatusEnabled
    foreach ($editorName in @('StatusPosition', 'StatusOffsetX', 'StatusOffsetY', 'StatusTextColor', 'StatusOpacity', 'StatusFontSize', 'StatusRefreshMs', 'StatusShowTemperature')) {
        $editor = $script:Editors[$editorName]
        foreach ($propertyName in @('Label', 'TrackBar', 'Numeric', 'Combo', 'CheckBox')) {
            $editorProperty = $editor.PSObject.Properties[$propertyName]
            if ($null -ne $editorProperty) {
                $editorProperty.Value.Enabled = $isStatusEnabled
            }
        }
    }
}

# @brief Push the current config state into the UI controls.
function Sync-ControlsFromState {
    [CmdletBinding()]
    param()

    $script:IsApplyingState = $true
    try {
        $script:Editors['Color'].Combo.SelectedItem = $script:ConfigState.Color

        foreach ($editorName in @('ColorR', 'ColorG', 'ColorB', 'Length', 'Gap', 'Thickness', 'OutlineThickness', 'Opacity', 'OffsetX', 'OffsetY', 'StatusOpacity', 'StatusFontSize')) {
            $editor = $script:Editors[$editorName]
            $value = [int]$script:ConfigState.$editorName
            $editor.TrackBar.Value = [Math]::Min([Math]::Max($value, $editor.TrackBar.Minimum), $editor.TrackBar.Maximum)
            $editor.Numeric.Value = $editor.TrackBar.Value
        }

        foreach ($editorName in @('StatusOffsetX', 'StatusOffsetY', 'StatusRefreshMs')) {
            $editor = $script:Editors[$editorName]
            $editor.Numeric.Value = [decimal]$script:ConfigState.$editorName
        }

        foreach ($editorName in @('ShowLeftArm', 'ShowRightArm', 'ShowTopArm', 'ShowBottomArm', 'StatusEnabled', 'StatusShowTemperature')) {
            $script:Editors[$editorName].CheckBox.Checked = [bool]$script:ConfigState.$editorName
        }

        $script:Editors['StatusPosition'].Combo.SelectedItem = $script:ConfigState.StatusPosition
        $script:Editors['StatusTextColor'].Combo.SelectedItem = $script:ConfigState.StatusTextColor
    }
    finally {
        $script:IsApplyingState = $false
    }

    Update-ControlStates
    if ($null -ne $script:PreviewPanel) {
        $script:PreviewPanel.Invalidate()
    }
}

# @brief Launch the overlay restart chain through the existing starter script.
function Restart-CrosshairOverlay {
    [CmdletBinding()]
    param()

    Start-Process -FilePath (Join-Path $PSScriptRoot 'Start-Crosshair.cmd')
}

# @brief Stop the current overlay through the existing stop script.
function Stop-CrosshairOverlay {
    [CmdletBinding()]
    param()

    Start-Process -FilePath (Join-Path $PSScriptRoot 'Stop-Crosshair.cmd')
}

$script:ConfigState = New-MutableConfig -Config (Get-InitialCrosshairConfig -ConfigFile $ConfigPath)
$script:AutoSaveTimer = [System.Windows.Forms.Timer]::new()
$script:AutoSaveTimer.Interval = 300
$script:AutoSaveTimer.Add_Tick({
    $script:AutoSaveTimer.Stop()
    try {
        Save-CrosshairConfig -ConfigFile $ConfigPath -Config $script:ConfigState
    }
    catch {
    }
})

# @brief Queue a debounced config save so the live overlay can reload changes.
function Request-AutoSave {
    [CmdletBinding()]
    param()

    $script:AutoSaveTimer.Stop()
    $script:AutoSaveTimer.Start()
}

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

$form = [System.Windows.Forms.Form]::new()
$form.Text = 'crosshair-overlay Settings'
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
$form.Size = [System.Drawing.Size]::new(1120, 760)
$form.MinimumSize = [System.Drawing.Size]::new(1000, 680)

$splitContainer = [System.Windows.Forms.SplitContainer]::new()
$splitContainer.Dock = [System.Windows.Forms.DockStyle]::Fill
$splitContainer.SplitterDistance = 560

$previewHost = [System.Windows.Forms.Panel]::new()
$previewHost.Dock = [System.Windows.Forms.DockStyle]::Fill
$previewHost.Padding = [System.Windows.Forms.Padding]::new(16)

$previewTitle = [System.Windows.Forms.Label]::new()
$previewTitle.Text = 'Live Preview'
$previewTitle.Dock = [System.Windows.Forms.DockStyle]::Top
$previewTitle.Height = 24
$previewTitle.Font = [System.Drawing.Font]::new('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)

$previewHint = [System.Windows.Forms.Label]::new()
$previewHint.Text = 'Drag the CPU label in preview to move it. Sliders and inputs update live.'
$previewHint.Dock = [System.Windows.Forms.DockStyle]::Top
$previewHint.Height = 28

$script:PreviewPanel = [System.Windows.Forms.Panel]::new()
$script:PreviewPanel.Dock = [System.Windows.Forms.DockStyle]::Fill
$script:PreviewPanel.BackColor = [System.Drawing.Color]::FromArgb(18, 20, 26)
$script:PreviewPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle

$doubleBufferedProperty = $script:PreviewPanel.GetType().GetProperty('DoubleBuffered', [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic)
if ($null -ne $doubleBufferedProperty) {
    $doubleBufferedProperty.SetValue($script:PreviewPanel, $true, $null)
}

$previewHost.Controls.Add($script:PreviewPanel)
$previewHost.Controls.Add($previewHint)
$previewHost.Controls.Add($previewTitle)

$controlHost = [System.Windows.Forms.FlowLayoutPanel]::new()
$controlHost.Dock = [System.Windows.Forms.DockStyle]::Fill
$controlHost.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
$controlHost.WrapContents = $false
$controlHost.AutoScroll = $true
$controlHost.Padding = [System.Windows.Forms.Padding]::new(10)

$crosshairGroup = [System.Windows.Forms.GroupBox]::new()
$crosshairGroup.Text = 'Crosshair'
$crosshairGroup.Width = 470
$crosshairGroup.Height = 470

$crosshairFlow = [System.Windows.Forms.FlowLayoutPanel]::new()
$crosshairFlow.Dock = [System.Windows.Forms.DockStyle]::Fill
$crosshairFlow.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
$crosshairFlow.WrapContents = $false

$script:Editors['Color'] = New-ComboEditor -LabelText 'Color Mode' -Items @('Green', 'Yellow', 'Custom') -Value $script:ConfigState.Color -OnChanged {
    param($value)
    $script:ConfigState.Color = $value
    Update-ControlStates
    Request-AutoSave
    $script:PreviewPanel.Invalidate()
}
$crosshairFlow.Controls.Add($script:Editors['Color'].Container)

foreach ($channelName in @('ColorR', 'ColorG', 'ColorB')) {
    $labelText = switch ($channelName) {
        'ColorR' { 'Color R' }
        'ColorG' { 'Color G' }
        default { 'Color B' }
    }

    $script:Editors[$channelName] = New-SliderEditor -LabelText $labelText -Minimum 0 -Maximum 255 -Value ([int]$script:ConfigState.$channelName) -OnChanged {
        param($value)
        $propertyName = $channelName
        $script:ConfigState.Color = 'Custom'
        $script:ConfigState.$propertyName = $value
        Update-ControlStates
        if ($script:Editors['Color'].Combo.SelectedItem -ne 'Custom') {
            $script:IsApplyingState = $true
            try {
                $script:Editors['Color'].Combo.SelectedItem = 'Custom'
            }
            finally {
                $script:IsApplyingState = $false
            }
        }
        Request-AutoSave
        $script:PreviewPanel.Invalidate()
    }.GetNewClosure()
    $crosshairFlow.Controls.Add($script:Editors[$channelName].Container)
}

foreach ($definition in @(
    @{ Name = 'Length';           Label = 'Length';    Min = 2;    Max = 20 }
    @{ Name = 'Gap';              Label = 'Gap';       Min = 1;    Max = 20 }
    @{ Name = 'Thickness';        Label = 'Thickness'; Min = 1;    Max = 6  }
    @{ Name = 'OutlineThickness'; Label = 'Outline';   Min = 0;    Max = 4  }
    @{ Name = 'Opacity';          Label = 'Opacity';   Min = 64;   Max = 255 }
    @{ Name = 'OffsetX';          Label = 'Offset X';  Min = -200; Max = 200 }
    @{ Name = 'OffsetY';          Label = 'Offset Y';  Min = -200; Max = 200 }
)) {
    $propertyName = [string]$definition.Name
    $script:Editors[$propertyName] = New-SliderEditor -LabelText $definition.Label -Minimum $definition.Min -Maximum $definition.Max -Value ([int]$script:ConfigState.$propertyName) -OnChanged {
        param($value)
        $script:ConfigState.$propertyName = $value
        Request-AutoSave
        $script:PreviewPanel.Invalidate()
    }.GetNewClosure()
    $crosshairFlow.Controls.Add($script:Editors[$propertyName].Container)
}

foreach ($definition in @(
    @{ Name = 'ShowLeftArm';   Text = 'Show Left Arm' }
    @{ Name = 'ShowRightArm';  Text = 'Show Right Arm' }
    @{ Name = 'ShowTopArm';    Text = 'Show Top Arm' }
    @{ Name = 'ShowBottomArm'; Text = 'Show Bottom Arm' }
)) {
    $propertyName = [string]$definition.Name
    $script:Editors[$propertyName] = New-CheckEditor -Text $definition.Text -Value ([bool]$script:ConfigState.$propertyName) -OnChanged {
        param($checked)
        $script:ConfigState.$propertyName = $checked
        try {
            Assert-CrosshairConfig -Config $script:ConfigState
        }
        catch {
            $script:IsApplyingState = $true
            try {
                $script:Editors[$propertyName].CheckBox.Checked = $true
            }
            finally {
                $script:IsApplyingState = $false
            }
            $script:ConfigState.$propertyName = $true
        }
        Request-AutoSave
        $script:PreviewPanel.Invalidate()
    }.GetNewClosure()
    $crosshairFlow.Controls.Add($script:Editors[$propertyName].Container)
}

$crosshairGroup.Controls.Add($crosshairFlow)
$controlHost.Controls.Add($crosshairGroup)

$statusGroup = [System.Windows.Forms.GroupBox]::new()
$statusGroup.Text = 'CPU Status'
$statusGroup.Width = 470
$statusGroup.Height = 360

$statusFlow = [System.Windows.Forms.FlowLayoutPanel]::new()
$statusFlow.Dock = [System.Windows.Forms.DockStyle]::Fill
$statusFlow.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
$statusFlow.WrapContents = $false

$script:Editors['StatusEnabled'] = New-CheckEditor -Text 'Enable CPU Status' -Value $script:ConfigState.StatusEnabled -OnChanged {
    param($checked)
    $script:ConfigState.StatusEnabled = $checked
    Update-ControlStates
    Request-AutoSave
    $script:PreviewPanel.Invalidate()
}
$statusFlow.Controls.Add($script:Editors['StatusEnabled'].Container)

$script:Editors['StatusShowTemperature'] = New-CheckEditor -Text 'Show Temperature' -Value $script:ConfigState.StatusShowTemperature -OnChanged {
    param($checked)
    $script:ConfigState.StatusShowTemperature = $checked
    Request-AutoSave
    $script:PreviewPanel.Invalidate()
}
$statusFlow.Controls.Add($script:Editors['StatusShowTemperature'].Container)

$script:Editors['StatusPosition'] = New-ComboEditor -LabelText 'Anchor' -Items @('TopLeft', 'TopRight', 'BottomLeft', 'BottomRight') -Value $script:ConfigState.StatusPosition -OnChanged {
    param($value)
    $script:ConfigState.StatusPosition = $value
    Request-AutoSave
    $script:PreviewPanel.Invalidate()
}
$statusFlow.Controls.Add($script:Editors['StatusPosition'].Container)

foreach ($definition in @(
    @{ Name = 'StatusOffsetX'; Label = 'Margin X';     Min = 0;   Max = 500 }
    @{ Name = 'StatusOffsetY'; Label = 'Margin Y';     Min = 0;   Max = 500 }
    @{ Name = 'StatusRefreshMs'; Label = 'Refresh (ms)'; Min = 500; Max = 5000 }
)) {
    $propertyName = [string]$definition.Name

    $panel = [System.Windows.Forms.Panel]::new()
    $panel.Width = 360
    $panel.Height = 42
    $panel.Margin = [System.Windows.Forms.Padding]::new(6)

    $label = [System.Windows.Forms.Label]::new()
    $label.Text = $definition.Label
    $label.Location = [System.Drawing.Point]::new(0, 10)
    $label.Width = 120

    $numeric = [System.Windows.Forms.NumericUpDown]::new()
    $numeric.Minimum = $definition.Min
    $numeric.Maximum = $definition.Max
    $numeric.Location = [System.Drawing.Point]::new(128, 6)
    $numeric.Width = 90
    $numeric.Value = [decimal]$script:ConfigState.$propertyName

    $numeric.Add_ValueChanged({
        if ($script:IsApplyingState) {
            return
        }

        $script:ConfigState.$propertyName = [int]$numeric.Value
        Request-AutoSave
        $script:PreviewPanel.Invalidate()
    }.GetNewClosure())

    $panel.Controls.AddRange(@($label, $numeric))
    $statusFlow.Controls.Add($panel)

    $script:Editors[$propertyName] = [pscustomobject]@{
        Container = $panel
        Label     = $label
        Numeric   = $numeric
    }
}

$script:Editors['StatusTextColor'] = New-ComboEditor -LabelText 'Text Color' -Items @('Yellow', 'Green', 'White') -Value $script:ConfigState.StatusTextColor -OnChanged {
    param($value)
    $script:ConfigState.StatusTextColor = $value
    Request-AutoSave
    $script:PreviewPanel.Invalidate()
}
$statusFlow.Controls.Add($script:Editors['StatusTextColor'].Container)

foreach ($definition in @(
    @{ Name = 'StatusOpacity';  Label = 'Text Opacity'; Min = 64; Max = 255 }
    @{ Name = 'StatusFontSize'; Label = 'Font Size';    Min = 9;  Max = 24  }
)) {
    $propertyName = [string]$definition.Name
    $script:Editors[$propertyName] = New-SliderEditor -LabelText $definition.Label -Minimum $definition.Min -Maximum $definition.Max -Value ([int]$script:ConfigState.$propertyName) -OnChanged {
        param($value)
        $script:ConfigState.$propertyName = $value
        Request-AutoSave
        $script:PreviewPanel.Invalidate()
    }.GetNewClosure()
    $statusFlow.Controls.Add($script:Editors[$propertyName].Container)
}

$statusHint = [System.Windows.Forms.Label]::new()
$statusHint.Text = 'Tip: drag the CPU text in the preview to place it.'
$statusHint.Width = 380
$statusHint.Height = 36
$statusHint.Margin = [System.Windows.Forms.Padding]::new(6)
$statusFlow.Controls.Add($statusHint)

$statusGroup.Controls.Add($statusFlow)
$controlHost.Controls.Add($statusGroup)

$actionGroup = [System.Windows.Forms.GroupBox]::new()
$actionGroup.Text = 'Actions'
$actionGroup.Width = 470
$actionGroup.Height = 120

$buttonFlow = [System.Windows.Forms.FlowLayoutPanel]::new()
$buttonFlow.Dock = [System.Windows.Forms.DockStyle]::Fill
$buttonFlow.Padding = [System.Windows.Forms.Padding]::new(12)
$buttonFlow.FlowDirection = [System.Windows.Forms.FlowDirection]::LeftToRight
$buttonFlow.WrapContents = $true

$saveButton = [System.Windows.Forms.Button]::new()
$saveButton.Text = 'Save'
$saveButton.Width = 100
$saveButton.Height = 32
$saveButton.Add_Click({
    try {
        Save-CrosshairConfig -ConfigFile $ConfigPath -Config $script:ConfigState
        [System.Windows.Forms.MessageBox]::Show('Config saved.', 'crosshair-overlay') | Out-Null
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Save Failed', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    }
})

$applyButton = [System.Windows.Forms.Button]::new()
$applyButton.Text = 'Save + Restart'
$applyButton.Width = 128
$applyButton.Height = 32
$applyButton.Add_Click({
    try {
        Save-CrosshairConfig -ConfigFile $ConfigPath -Config $script:ConfigState
        Restart-CrosshairOverlay
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Apply Failed', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    }
})

$stopButton = [System.Windows.Forms.Button]::new()
$stopButton.Text = 'Stop Overlay'
$stopButton.Width = 100
$stopButton.Height = 32
$stopButton.Add_Click({
    Stop-CrosshairOverlay
})

$resetButton = [System.Windows.Forms.Button]::new()
$resetButton.Text = 'Reset Defaults'
$resetButton.Width = 100
$resetButton.Height = 32
$resetButton.Add_Click({
    $script:ConfigState = New-MutableConfig -Config (Get-CrosshairDefaultConfig)
    Sync-ControlsFromState
    Request-AutoSave
})

$buttonFlow.Controls.AddRange(@($saveButton, $applyButton, $stopButton, $resetButton))
$actionGroup.Controls.Add($buttonFlow)
$controlHost.Controls.Add($actionGroup)

$splitContainer.Panel1.Controls.Add($previewHost)
$splitContainer.Panel2.Controls.Add($controlHost)
$form.Controls.Add($splitContainer)

$script:PreviewPanel.Add_Paint({
    param($sender, $eventArgs)

    $graphics = $eventArgs.Graphics
    $graphics.Clear([System.Drawing.Color]::FromArgb(18, 20, 26))
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None

    $clientWidth = $sender.ClientSize.Width
    $clientHeight = $sender.ClientSize.Height

    $overlayBounds = Get-CrosshairOverlayBounds `
        -AreaLeft 0 `
        -AreaTop 0 `
        -AreaWidth $clientWidth `
        -AreaHeight $clientHeight `
        -Length $script:ConfigState.Length `
        -Gap $script:ConfigState.Gap `
        -Thickness $script:ConfigState.Thickness `
        -OutlineThickness $script:ConfigState.OutlineThickness `
        -OffsetX $script:ConfigState.OffsetX `
        -OffsetY $script:ConfigState.OffsetY

    $centerX = $overlayBounds.CenterX
    $centerY = $overlayBounds.CenterY
    $segments = Get-CrosshairSegments `
        -CenterX $centerX `
        -CenterY $centerY `
        -Length $script:ConfigState.Length `
        -Gap $script:ConfigState.Gap `
        -ShowLeftArm $script:ConfigState.ShowLeftArm `
        -ShowRightArm $script:ConfigState.ShowRightArm `
        -ShowTopArm $script:ConfigState.ShowTopArm `
        -ShowBottomArm $script:ConfigState.ShowBottomArm

    if ($script:ConfigState.OutlineThickness -gt 0) {
        $outlinePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb($script:ConfigState.Opacity, [System.Drawing.Color]::Black), $script:ConfigState.Thickness + ($script:ConfigState.OutlineThickness * 2))
        try {
            $outlinePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Square
            $outlinePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
            foreach ($segment in $segments) {
                $graphics.DrawLine($outlinePen, $segment.X1, $segment.Y1, $segment.X2, $segment.Y2)
            }
        }
        finally {
            $outlinePen.Dispose()
        }
    }

    $crosshairColor = Resolve-CrosshairColor `
        -ColorName $script:ConfigState.Color `
        -Opacity $script:ConfigState.Opacity `
        -Red $script:ConfigState.ColorR `
        -Green $script:ConfigState.ColorG `
        -Blue $script:ConfigState.ColorB

    $crosshairPen = [System.Drawing.Pen]::new($crosshairColor, $script:ConfigState.Thickness)
    try {
        $crosshairPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Square
        $crosshairPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
        foreach ($segment in $segments) {
            $graphics.DrawLine($crosshairPen, $segment.X1, $segment.Y1, $segment.X2, $segment.Y2)
        }
    }
    finally {
        $crosshairPen.Dispose()
    }

    $guidePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(50, 255, 255, 255), 1)
    try {
        $graphics.DrawLine($guidePen, [int]($clientWidth / 2), 0, [int]($clientWidth / 2), $clientHeight)
        $graphics.DrawLine($guidePen, 0, [int]($clientHeight / 2), $clientWidth, [int]($clientHeight / 2))
    }
    finally {
        $guidePen.Dispose()
    }

    if ($script:ConfigState.StatusEnabled) {
        $statusBounds = Get-PreviewStatusBounds -PreviewPanel $sender
        $script:PreviewStatusBounds = [System.Drawing.Rectangle]::new($statusBounds.Left, $statusBounds.Top, $statusBounds.Width, $statusBounds.Height)

        $statusText = Get-PreviewStatusText
        $statusFont = [System.Drawing.Font]::new('Consolas', [float]$script:ConfigState.StatusFontSize, [System.Drawing.FontStyle]::Bold)
        try {
            $statusTextColor = Resolve-StatusTextColor -ColorName $script:ConfigState.StatusTextColor -Opacity $script:ConfigState.StatusOpacity
            $flags = [System.Windows.Forms.TextFormatFlags]::NoPadding -bor [System.Windows.Forms.TextFormatFlags]::SingleLine
            [System.Windows.Forms.TextRenderer]::DrawText($graphics, $statusText, $statusFont, [System.Drawing.Point]::new($script:PreviewStatusBounds.Left + 7, $script:PreviewStatusBounds.Top + 5), [System.Drawing.Color]::FromArgb([Math]::Max([int]($statusTextColor.A / 2), 90), [System.Drawing.Color]::Black), $flags)
            [System.Windows.Forms.TextRenderer]::DrawText($graphics, $statusText, $statusFont, [System.Drawing.Point]::new($script:PreviewStatusBounds.Left + 6, $script:PreviewStatusBounds.Top + 4), $statusTextColor, $flags)
        }
        finally {
            $statusFont.Dispose()
        }
    }
    else {
        $script:PreviewStatusBounds = [System.Drawing.Rectangle]::Empty
    }
})

$script:PreviewPanel.Add_MouseDown({
    param($sender, $eventArgs)

    if (-not $script:ConfigState.StatusEnabled) {
        return
    }

    if ($eventArgs.Button -eq [System.Windows.Forms.MouseButtons]::Left -and $script:PreviewStatusBounds.Contains($eventArgs.Location)) {
        $script:IsDraggingStatus = $true
        $script:StatusDragOffset = [System.Drawing.Point]::new($eventArgs.X - $script:PreviewStatusBounds.Left, $eventArgs.Y - $script:PreviewStatusBounds.Top)
        $sender.Cursor = [System.Windows.Forms.Cursors]::SizeAll
    }
})

$script:PreviewPanel.Add_MouseMove({
    param($sender, $eventArgs)

    if ($script:IsDraggingStatus) {
        $nextLeft = $eventArgs.X - $script:StatusDragOffset.X
        $nextTop = $eventArgs.Y - $script:StatusDragOffset.Y
        $placement = Resolve-StatusOverlayPlacement `
            -AreaWidth $sender.ClientSize.Width `
            -AreaHeight $sender.ClientSize.Height `
            -OverlayWidth $script:PreviewStatusBounds.Width `
            -OverlayHeight $script:PreviewStatusBounds.Height `
            -OverlayLeft $nextLeft `
            -OverlayTop $nextTop

        $script:ConfigState.StatusPosition = $placement.Position
        $script:ConfigState.StatusOffsetX = $placement.OffsetX
        $script:ConfigState.StatusOffsetY = $placement.OffsetY

        Sync-ControlsFromState
        Request-AutoSave
        $sender.Cursor = [System.Windows.Forms.Cursors]::SizeAll
        return
    }

    if ($script:ConfigState.StatusEnabled -and $script:PreviewStatusBounds.Contains($eventArgs.Location)) {
        $sender.Cursor = [System.Windows.Forms.Cursors]::SizeAll
    }
    else {
        $sender.Cursor = [System.Windows.Forms.Cursors]::Default
    }
})

$script:PreviewPanel.Add_MouseUp({
    param($sender, $eventArgs)
    $script:IsDraggingStatus = $false
    $sender.Cursor = [System.Windows.Forms.Cursors]::Default
})

$form.Add_FormClosing({
    if ($script:AutoSaveTimer.Enabled) {
        $script:AutoSaveTimer.Stop()
        try {
            Save-CrosshairConfig -ConfigFile $ConfigPath -Config $script:ConfigState
        }
        catch {
        }
    }
})

Sync-ControlsFromState

if ($AutoCloseMs -gt 0) {
    $timer = [System.Windows.Forms.Timer]::new()
    $timer.Interval = [Math]::Max($AutoCloseMs, 100)
    $timer.Add_Tick({
        $timer.Stop()
        $form.Close()
        $timer.Dispose()
    })
    $timer.Start()
}

[void][System.Windows.Forms.Application]::Run($form)
