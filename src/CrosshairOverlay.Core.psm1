Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

function Get-CrosshairDefaultConfig {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        Color                 = 'Green'
        ColorR                = 0
        ColorG                = 255
        ColorB                = 0
        Length                = 6
        Gap                   = 4
        Thickness             = 2
        OutlineThickness      = 1
        Opacity               = 255
        OffsetX               = 0
        OffsetY               = 0
        ShowLeftArm           = $true
        ShowRightArm          = $true
        ShowTopArm            = $true
        ShowBottomArm         = $true
        StatusEnabled         = $false
        StatusPosition        = 'TopRight'
        StatusOffsetX         = 24
        StatusOffsetY         = 24
        StatusRefreshMs       = 1500
        StatusTextColor       = 'Yellow'
        StatusOpacity         = 220
        StatusFontSize        = 11
        StatusShowTemperature = $true
    }
}

function Merge-CrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$DefaultConfig,

        [Parameter()]
        [AllowNull()]
        [psobject]$OverrideConfig
    )

    $merged = [ordered]@{}
    foreach ($propertyName in $DefaultConfig.PSObject.Properties.Name) {
        $merged[$propertyName] = $DefaultConfig.$propertyName
    }

    if ($null -ne $OverrideConfig) {
        foreach ($property in $OverrideConfig.PSObject.Properties) {
            $merged[$property.Name] = $property.Value
        }
    }

    [pscustomobject]@{
        Color                 = [string]$merged.Color
        ColorR                = [int]$merged.ColorR
        ColorG                = [int]$merged.ColorG
        ColorB                = [int]$merged.ColorB
        Length                = [int]$merged.Length
        Gap                   = [int]$merged.Gap
        Thickness             = [int]$merged.Thickness
        OutlineThickness      = [int]$merged.OutlineThickness
        Opacity               = [int]$merged.Opacity
        OffsetX               = [int]$merged.OffsetX
        OffsetY               = [int]$merged.OffsetY
        ShowLeftArm           = [bool]$merged.ShowLeftArm
        ShowRightArm          = [bool]$merged.ShowRightArm
        ShowTopArm            = [bool]$merged.ShowTopArm
        ShowBottomArm         = [bool]$merged.ShowBottomArm
        StatusEnabled         = [bool]$merged.StatusEnabled
        StatusPosition        = [string]$merged.StatusPosition
        StatusOffsetX         = [int]$merged.StatusOffsetX
        StatusOffsetY         = [int]$merged.StatusOffsetY
        StatusRefreshMs       = [int]$merged.StatusRefreshMs
        StatusTextColor       = [string]$merged.StatusTextColor
        StatusOpacity         = [int]$merged.StatusOpacity
        StatusFontSize        = [int]$merged.StatusFontSize
        StatusShowTemperature = [bool]$merged.StatusShowTemperature
    }
}

function Assert-CrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $allowedCrosshairColors = @('Green', 'Yellow', 'Custom')
    if ($allowedCrosshairColors -notcontains $Config.Color) {
        throw "Color must be one of: $($allowedCrosshairColors -join ', ')."
    }

    foreach ($channelName in @('ColorR', 'ColorG', 'ColorB')) {
        if ($Config.$channelName -lt 0 -or $Config.$channelName -gt 255) {
            throw "$channelName must be between 0 and 255."
        }
    }

    if ($Config.Length -lt 2 -or $Config.Length -gt 20) {
        throw 'Length must be between 2 and 20.'
    }

    if ($Config.Gap -lt 1 -or $Config.Gap -gt 20) {
        throw 'Gap must be between 1 and 20.'
    }

    if ($Config.Thickness -lt 1 -or $Config.Thickness -gt 6) {
        throw 'Thickness must be between 1 and 6.'
    }

    if ($Config.OutlineThickness -lt 0 -or $Config.OutlineThickness -gt 4) {
        throw 'OutlineThickness must be between 0 and 4.'
    }

    if ($Config.Opacity -lt 64 -or $Config.Opacity -gt 255) {
        throw 'Opacity must be between 64 and 255.'
    }

    foreach ($offsetName in @('OffsetX', 'OffsetY')) {
        if ($Config.$offsetName -lt -4000 -or $Config.$offsetName -gt 4000) {
            throw "$offsetName must be between -4000 and 4000."
        }
    }

    if (-not ($Config.ShowLeftArm -or $Config.ShowRightArm -or $Config.ShowTopArm -or $Config.ShowBottomArm)) {
        throw 'At least one crosshair arm must be enabled.'
    }

    $allowedStatusPositions = @('TopLeft', 'TopRight', 'BottomLeft', 'BottomRight')
    if ($allowedStatusPositions -notcontains $Config.StatusPosition) {
        throw "StatusPosition must be one of: $($allowedStatusPositions -join ', ')."
    }

    $allowedStatusColors = @('Green', 'Yellow', 'White')
    if ($allowedStatusColors -notcontains $Config.StatusTextColor) {
        throw "StatusTextColor must be one of: $($allowedStatusColors -join ', ')."
    }

    if ($Config.StatusOffsetX -lt 0 -or $Config.StatusOffsetX -gt 500) {
        throw 'StatusOffsetX must be between 0 and 500.'
    }

    if ($Config.StatusOffsetY -lt 0 -or $Config.StatusOffsetY -gt 500) {
        throw 'StatusOffsetY must be between 0 and 500.'
    }

    if ($Config.StatusRefreshMs -lt 500 -or $Config.StatusRefreshMs -gt 5000) {
        throw 'StatusRefreshMs must be between 500 and 5000.'
    }

    if ($Config.StatusOpacity -lt 64 -or $Config.StatusOpacity -gt 255) {
        throw 'StatusOpacity must be between 64 and 255.'
    }

    if ($Config.StatusFontSize -lt 9 -or $Config.StatusFontSize -gt 24) {
        throw 'StatusFontSize must be between 9 and 24.'
    }
}

function Resolve-CrosshairColor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ColorName,

        [Parameter(Mandatory = $true)]
        [int]$Opacity,

        [Parameter()]
        [int]$Red = 0,

        [Parameter()]
        [int]$Green = 255,

        [Parameter()]
        [int]$Blue = 0
    )

    switch ($ColorName) {
        'Green' {
            return [System.Drawing.Color]::FromArgb($Opacity, [System.Drawing.Color]::Lime)
        }
        'Yellow' {
            return [System.Drawing.Color]::FromArgb($Opacity, [System.Drawing.Color]::Yellow)
        }
        'Custom' {
            return [System.Drawing.Color]::FromArgb($Opacity, $Red, $Green, $Blue)
        }
        default {
            throw "Unknown color: $ColorName"
        }
    }
}

function Resolve-StatusTextColor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ColorName,

        [Parameter(Mandatory = $true)]
        [int]$Opacity
    )

    switch ($ColorName) {
        'Green' {
            return [System.Drawing.Color]::FromArgb($Opacity, [System.Drawing.Color]::Lime)
        }
        'Yellow' {
            return [System.Drawing.Color]::FromArgb($Opacity, [System.Drawing.Color]::Yellow)
        }
        'White' {
            return [System.Drawing.Color]::FromArgb($Opacity, [System.Drawing.Color]::White)
        }
        default {
            throw "Unknown status color: $ColorName"
        }
    }
}

function Get-CrosshairSegments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$CenterX,

        [Parameter(Mandatory = $true)]
        [int]$CenterY,

        [Parameter(Mandatory = $true)]
        [int]$Length,

        [Parameter(Mandatory = $true)]
        [int]$Gap,

        [Parameter()]
        [bool]$ShowLeftArm = $true,

        [Parameter()]
        [bool]$ShowRightArm = $true,

        [Parameter()]
        [bool]$ShowTopArm = $true,

        [Parameter()]
        [bool]$ShowBottomArm = $true
    )

    $segments = @()

    if ($ShowLeftArm) {
        $segments += [pscustomobject]@{
            Name = 'Left'
            X1   = $CenterX - $Gap - $Length
            Y1   = $CenterY
            X2   = $CenterX - $Gap
            Y2   = $CenterY
        }
    }

    if ($ShowRightArm) {
        $segments += [pscustomobject]@{
            Name = 'Right'
            X1   = $CenterX + $Gap
            Y1   = $CenterY
            X2   = $CenterX + $Gap + $Length
            Y2   = $CenterY
        }
    }

    if ($ShowTopArm) {
        $segments += [pscustomobject]@{
            Name = 'Top'
            X1   = $CenterX
            Y1   = $CenterY - $Gap - $Length
            X2   = $CenterX
            Y2   = $CenterY - $Gap
        }
    }

    if ($ShowBottomArm) {
        $segments += [pscustomobject]@{
            Name = 'Bottom'
            X1   = $CenterX
            Y1   = $CenterY + $Gap
            X2   = $CenterX
            Y2   = $CenterY + $Gap + $Length
        }
    }

    return $segments
}

# @brief Compute a compact overlay window centered inside a target area.
# @param AreaLeft Left edge of the target area.
# @param AreaTop Top edge of the target area.
# @param AreaWidth Width of the target area.
# @param AreaHeight Height of the target area.
# @param Length Crosshair arm length.
# @param Gap Empty center gap.
# @param Thickness Main stroke width.
# @param OutlineThickness Outline stroke width.
# @return A compact rectangle large enough to contain the crosshair.
function Get-CrosshairOverlayBounds {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$AreaLeft,

        [Parameter(Mandatory = $true)]
        [int]$AreaTop,

        [Parameter(Mandatory = $true)]
        [int]$AreaWidth,

        [Parameter(Mandatory = $true)]
        [int]$AreaHeight,

        [Parameter(Mandatory = $true)]
        [int]$Length,

        [Parameter(Mandatory = $true)]
        [int]$Gap,

        [Parameter(Mandatory = $true)]
        [int]$Thickness,

        [Parameter(Mandatory = $true)]
        [int]$OutlineThickness,

        [Parameter()]
        [int]$OffsetX = 0,

        [Parameter()]
        [int]$OffsetY = 0
    )

    if ($AreaWidth -le 0 -or $AreaHeight -le 0) {
        throw 'Area dimensions must be positive.'
    }

    $strokeWidth = $Thickness + ($OutlineThickness * 2)
    $padding = [int][Math]::Ceiling($strokeWidth / 2.0) + 2
    $halfSpan = $Gap + $Length + $padding
    $windowSize = ($halfSpan * 2) + 1
    $targetCenterX = $AreaLeft + [int]($AreaWidth / 2) + $OffsetX
    $targetCenterY = $AreaTop + [int]($AreaHeight / 2) + $OffsetY
    $localCenter = [int]($windowSize / 2)

    [pscustomobject]@{
        Left    = $targetCenterX - $localCenter
        Top     = $targetCenterY - $localCenter
        Width   = $windowSize
        Height  = $windowSize
        CenterX = $localCenter
        CenterY = $localCenter
    }
}

# @brief Compute the bounds for a small status overlay anchored to a game window.
# @param AreaLeft Left edge of the target area.
# @param AreaTop Top edge of the target area.
# @param AreaWidth Width of the target area.
# @param AreaHeight Height of the target area.
# @param OverlayWidth Width of the status overlay window.
# @param OverlayHeight Height of the status overlay window.
# @param Position Anchoring position.
# @param OffsetX Horizontal inset from the edge.
# @param OffsetY Vertical inset from the edge.
# @return Bounds for the status overlay window.
function Get-StatusOverlayBounds {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$AreaLeft,

        [Parameter(Mandatory = $true)]
        [int]$AreaTop,

        [Parameter(Mandatory = $true)]
        [int]$AreaWidth,

        [Parameter(Mandatory = $true)]
        [int]$AreaHeight,

        [Parameter(Mandatory = $true)]
        [int]$OverlayWidth,

        [Parameter(Mandatory = $true)]
        [int]$OverlayHeight,

        [Parameter(Mandatory = $true)]
        [string]$Position,

        [Parameter(Mandatory = $true)]
        [int]$OffsetX,

        [Parameter(Mandatory = $true)]
        [int]$OffsetY
    )

    if ($AreaWidth -le 0 -or $AreaHeight -le 0) {
        throw 'Area dimensions must be positive.'
    }

    if ($OverlayWidth -le 0 -or $OverlayHeight -le 0) {
        throw 'Overlay dimensions must be positive.'
    }

    switch ($Position) {
        'TopLeft' {
            $left = $AreaLeft + $OffsetX
            $top = $AreaTop + $OffsetY
        }
        'TopRight' {
            $left = $AreaLeft + $AreaWidth - $OverlayWidth - $OffsetX
            $top = $AreaTop + $OffsetY
        }
        'BottomLeft' {
            $left = $AreaLeft + $OffsetX
            $top = $AreaTop + $AreaHeight - $OverlayHeight - $OffsetY
        }
        'BottomRight' {
            $left = $AreaLeft + $AreaWidth - $OverlayWidth - $OffsetX
            $top = $AreaTop + $AreaHeight - $OverlayHeight - $OffsetY
        }
        default {
            throw "Unknown status position: $Position"
        }
    }

    $maxLeft = $AreaLeft + [Math]::Max($AreaWidth - $OverlayWidth, 0)
    $maxTop = $AreaTop + [Math]::Max($AreaHeight - $OverlayHeight, 0)

    [pscustomobject]@{
        Left   = [Math]::Min([Math]::Max($left, $AreaLeft), $maxLeft)
        Top    = [Math]::Min([Math]::Max($top, $AreaTop), $maxTop)
        Width  = $OverlayWidth
        Height = $OverlayHeight
    }
}

# @brief Resolve a free-dragged status label into an anchor position and offsets.
# @param AreaWidth Width of the preview or target area.
# @param AreaHeight Height of the preview or target area.
# @param OverlayWidth Width of the dragged label.
# @param OverlayHeight Height of the dragged label.
# @param OverlayLeft Dragged left position.
# @param OverlayTop Dragged top position.
# @return Position and offsets compatible with Get-StatusOverlayBounds.
function Resolve-StatusOverlayPlacement {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$AreaWidth,

        [Parameter(Mandatory = $true)]
        [int]$AreaHeight,

        [Parameter(Mandatory = $true)]
        [int]$OverlayWidth,

        [Parameter(Mandatory = $true)]
        [int]$OverlayHeight,

        [Parameter(Mandatory = $true)]
        [int]$OverlayLeft,

        [Parameter(Mandatory = $true)]
        [int]$OverlayTop
    )

    if ($AreaWidth -le 0 -or $AreaHeight -le 0) {
        throw 'Area dimensions must be positive.'
    }

    if ($OverlayWidth -le 0 -or $OverlayHeight -le 0) {
        throw 'Overlay dimensions must be positive.'
    }

    $clampedLeft = [Math]::Min([Math]::Max($OverlayLeft, 0), [Math]::Max($AreaWidth - $OverlayWidth, 0))
    $clampedTop = [Math]::Min([Math]::Max($OverlayTop, 0), [Math]::Max($AreaHeight - $OverlayHeight, 0))
    $rightInset = [Math]::Max($AreaWidth - $OverlayWidth - $clampedLeft, 0)
    $bottomInset = [Math]::Max($AreaHeight - $OverlayHeight - $clampedTop, 0)

    $candidates = @(
        [pscustomobject]@{ Position = 'TopLeft';     OffsetX = $clampedLeft; OffsetY = $clampedTop; Score = $clampedLeft + $clampedTop }
        [pscustomobject]@{ Position = 'TopRight';    OffsetX = $rightInset;  OffsetY = $clampedTop; Score = $rightInset + $clampedTop }
        [pscustomobject]@{ Position = 'BottomLeft';  OffsetX = $clampedLeft; OffsetY = $bottomInset; Score = $clampedLeft + $bottomInset }
        [pscustomobject]@{ Position = 'BottomRight'; OffsetX = $rightInset;  OffsetY = $bottomInset; Score = $rightInset + $bottomInset }
    )

    return $candidates |
        Sort-Object -Property Score, Position |
        Select-Object -First 1
}

# @brief Convert a processor performance percentage into an effective frequency estimate.
# @param BaseClockMHz Base CPU clock in MHz.
# @param PerformancePercent Processor performance percentage.
# @return Estimated effective frequency in MHz.
function Convert-ProcessorPerformanceToMHz {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [double]$BaseClockMHz,

        [Parameter(Mandatory = $true)]
        [double]$PerformancePercent
    )

    if ($BaseClockMHz -le 0 -or $PerformancePercent -le 0) {
        return 0
    }

    return [int][Math]::Round(($BaseClockMHz * $PerformancePercent) / 100.0, 0, [System.MidpointRounding]::AwayFromZero)
}

# @brief Merge vendor telemetry with generic fallback telemetry.
# @param FallbackFrequencyMHz Frequency from the generic fallback path.
# @param FallbackTemperatureC Temperature from the generic fallback path.
# @param FallbackApproximateTemperature True when the fallback temperature is approximate.
# @param VendorFrequencyMHz Frequency from the vendor path.
# @param VendorTemperatureC Temperature from the vendor path.
# @param BaseClockMHz CPU base clock used to suppress vendor warm-up samples that are stuck at base speed.
# @return Preferred frequency and temperature snapshot.
function Resolve-PreferredCpuStatusSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [double]$FallbackFrequencyMHz,

        [Parameter()]
        [AllowNull()]
        [Nullable[double]]$FallbackTemperatureC,

        [Parameter(Mandatory = $true)]
        [bool]$FallbackApproximateTemperature,

        [Parameter()]
        [AllowNull()]
        [Nullable[int]]$VendorFrequencyMHz,

        [Parameter()]
        [AllowNull()]
        [Nullable[int]]$VendorTemperatureC,

        [Parameter()]
        [AllowNull()]
        [Nullable[double]]$BaseClockMHz
    )

    $preferredFrequencyMHz = $FallbackFrequencyMHz
    $preferredTemperatureC = $FallbackTemperatureC
    $preferredApproximateTemperature = $FallbackApproximateTemperature

    if ($null -ne $VendorTemperatureC -and $VendorTemperatureC -gt 0) {
        $preferredTemperatureC = [double]$VendorTemperatureC
        $preferredApproximateTemperature = $false
    }

    if ($null -ne $VendorFrequencyMHz -and $VendorFrequencyMHz -gt 0) {
        $shouldUseVendorFrequency = $true
        if (
            $null -ne $BaseClockMHz -and
            $BaseClockMHz -gt 0 -and
            $VendorFrequencyMHz -le ($BaseClockMHz + 1) -and
            $FallbackFrequencyMHz -gt ($BaseClockMHz + 200)
        ) {
            $shouldUseVendorFrequency = $false
        }

        if ($shouldUseVendorFrequency) {
            $preferredFrequencyMHz = [double]$VendorFrequencyMHz
        }
    }

    return [pscustomobject]@{
        FrequencyMHz           = $preferredFrequencyMHz
        TemperatureC           = $preferredTemperatureC
        ApproximateTemperature = $preferredApproximateTemperature
    }
}

# @brief Format the compact CPU status text shown in the corner overlay.
# @param FrequencyMHz Total processor frequency in MHz.
# @param TemperatureC Temperature in Celsius or null when unavailable.
# @param ApproximateTemperature True when the temperature is only a thermal-zone approximation.
# @param ShowTemperature True when the temperature field should be included.
# @return Display string for the status overlay.
function Format-StatusOverlayText {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [double]$FrequencyMHz,

        [Parameter()]
        [AllowNull()]
        [Nullable[double]]$TemperatureC,

        [Parameter(Mandatory = $true)]
        [bool]$ApproximateTemperature,

        [Parameter(Mandatory = $true)]
        [bool]$ShowTemperature
    )

    $frequencyGHz = [Math]::Round(($FrequencyMHz / 1000.0), 1)
    $text = "CPU {0:N1}GHz" -f $frequencyGHz

    if (-not $ShowTemperature) {
        return $text
    }

    if ($null -eq $TemperatureC) {
        return "$text | --C"
    }

    $roundedTemperature = [int][Math]::Round($TemperatureC, 0)
    if ($ApproximateTemperature) {
        return "$text | TZ $roundedTemperature" + 'C'
    }

    return "$text | $roundedTemperature" + 'C'
}

# @brief Test whether the foreground process should show the overlay.
# @param ProcessName Foreground process name.
# @param TargetProcessNames Allowed process names.
# @return True when the process is in the allow list.
function Test-CrosshairTargetProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProcessName,

        [Parameter(Mandatory = $true)]
        [string[]]$TargetProcessNames
    )

    foreach ($targetProcessName in $TargetProcessNames) {
        if ([string]::Equals($ProcessName, $targetProcessName, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-CrosshairVisibilityState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [bool]$WasVisible,

        [Parameter(Mandatory = $true)]
        [bool]$ShouldShowTarget,

        [Parameter(Mandatory = $true)]
        [int]$MissCount,

        [Parameter(Mandatory = $true)]
        [int]$HideAfterMisses
    )

    if ($HideAfterMisses -lt 1) {
        throw 'HideAfterMisses must be at least 1.'
    }

    if ($ShouldShowTarget) {
        return [pscustomobject]@{
            ShouldShow = $true
            MissCount  = 0
        }
    }

    if (-not $WasVisible) {
        return [pscustomobject]@{
            ShouldShow = $false
            MissCount  = 0
        }
    }

    $nextMissCount = $MissCount + 1
    return [pscustomobject]@{
        ShouldShow = ($nextMissCount -lt $HideAfterMisses)
        MissCount  = $nextMissCount
    }
}

Export-ModuleMember -Function `
    Get-CrosshairDefaultConfig, `
    Merge-CrosshairConfig, `
    Assert-CrosshairConfig, `
    Resolve-CrosshairColor, `
    Resolve-StatusTextColor, `
    Get-CrosshairSegments, `
    Get-CrosshairOverlayBounds, `
    Get-StatusOverlayBounds, `
    Resolve-StatusOverlayPlacement, `
    Convert-ProcessorPerformanceToMHz, `
    Resolve-PreferredCpuStatusSnapshot, `
    Format-StatusOverlayText, `
    Test-CrosshairTargetProcess, `
    Get-CrosshairVisibilityState
