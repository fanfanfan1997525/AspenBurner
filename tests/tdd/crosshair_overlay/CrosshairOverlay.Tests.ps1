$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $here '..\..\..'))

Import-Module (Join-Path $projectRoot 'src\CrosshairOverlay.Core.psm1') -Force

function New-TestCrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter()]
        [hashtable]$Override = @{}
    )

    $defaultConfig = Get-CrosshairDefaultConfig
    $mergedValues = @{}

    foreach ($property in $defaultConfig.PSObject.Properties) {
        $mergedValues[$property.Name] = $property.Value
    }

    foreach ($entry in $Override.GetEnumerator()) {
        $mergedValues[$entry.Key] = $entry.Value
    }

    return [pscustomobject]$mergedValues
}

Describe 'Get-CrosshairDefaultConfig' {
    It 'uses green as the default color' {
        $config = Get-CrosshairDefaultConfig
        $config.Color | Should Be 'Green'
    }

    It 'uses a compact default line length' {
        $config = Get-CrosshairDefaultConfig
        $config.Length | Should Be 6
    }

    It 'keeps crosshair offsets centered by default' {
        $config = Get-CrosshairDefaultConfig
        $config.OffsetX | Should Be 0
        $config.OffsetY | Should Be 0
    }

    It 'enables all four arms by default' {
        $config = Get-CrosshairDefaultConfig
        $config.ShowLeftArm | Should Be $true
        $config.ShowRightArm | Should Be $true
        $config.ShowTopArm | Should Be $true
        $config.ShowBottomArm | Should Be $true
    }

    It 'keeps the status overlay disabled by default' {
        $config = Get-CrosshairDefaultConfig
        $config.StatusEnabled | Should Be $false
        $config.StatusPosition | Should Be 'TopRight'
    }
}

Describe 'Merge-CrosshairConfig' {
    It 'applies override values without dropping defaults' {
        $config = Merge-CrosshairConfig -DefaultConfig (Get-CrosshairDefaultConfig) -OverrideConfig ([pscustomobject]@{
            Color      = 'Yellow'
            Gap        = 5
            OffsetX    = 12
            ShowTopArm = $false
        })

        $config.Color | Should Be 'Yellow'
        $config.Thickness | Should Be 2
        $config.OffsetX | Should Be 12
        $config.ShowTopArm | Should Be $false
    }
}

Describe 'Assert-CrosshairConfig' {
    It 'rejects unsupported colors' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Color = 'Blue'
            })
        } | Should Throw
    }

    It 'rejects too-short line lengths' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Length = 1
            })
        } | Should Throw
    }

    It 'rejects zero gap values' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Gap = 0
            })
        } | Should Throw
    }

    It 'rejects zero thickness values' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Thickness = 0
            })
        } | Should Throw
    }

    It 'rejects opacity values below the visible floor' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Opacity = 30
            })
        } | Should Throw
    }

    It 'rejects unsupported status positions' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                StatusPosition = 'CenterRight'
            })
        } | Should Throw
    }

    It 'rejects overly aggressive status refresh intervals' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                StatusRefreshMs = 200
            })
        } | Should Throw
    }

    It 'rejects unsupported custom color channels' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                Color  = 'Custom'
                ColorR = 300
            })
        } | Should Throw
    }

    It 'rejects absurd crosshair offsets' {
        {
            Assert-CrosshairConfig -Config (New-TestCrosshairConfig -Override @{
                OffsetX = 5001
            })
        } | Should Throw
    }
}

Describe 'Resolve-CrosshairColor' {
    It 'resolves green to lime with the requested alpha channel' {
        $color = Resolve-CrosshairColor -ColorName 'Green' -Opacity 200

        $color.A | Should Be 200
        $color.G | Should Be 255
    }

    It 'resolves custom RGB colors with the requested alpha channel' {
        $color = Resolve-CrosshairColor -ColorName 'Custom' -Opacity 210 -Red 12 -Green 34 -Blue 56

        $color.A | Should Be 210
        $color.R | Should Be 12
        $color.G | Should Be 34
        $color.B | Should Be 56
    }
}

Describe 'Resolve-StatusTextColor' {
    It 'resolves yellow with the requested alpha channel' {
        $color = Resolve-StatusTextColor -ColorName 'Yellow' -Opacity 180

        $color.A | Should Be 180
        $color.R | Should Be 255
        $color.G | Should Be 255
    }
}

Describe 'Get-CrosshairSegments' {
    It 'returns exactly four line segments' {
        $segments = Get-CrosshairSegments -CenterX 100 -CenterY 100 -Length 6 -Gap 4
        $segments.Count | Should Be 4
    }

    It 'drops disabled arms instead of forcing a fixed cross' {
        $segments = Get-CrosshairSegments -CenterX 100 -CenterY 100 -Length 6 -Gap 4 -ShowTopArm $false -ShowBottomArm $false

        $segments.Count | Should Be 2
        ($segments.Name -contains 'Top') | Should Be $false
        ($segments.Name -contains 'Bottom') | Should Be $false
    }

    It 'returns the expected coordinates for a known center point' {
        $segments = Get-CrosshairSegments -CenterX 100 -CenterY 100 -Length 6 -Gap 4

        ($segments | Where-Object { $_.Name -eq 'Left' }).X1 | Should Be 90
        ($segments | Where-Object { $_.Name -eq 'Right' }).X2 | Should Be 110
        ($segments | Where-Object { $_.Name -eq 'Top' }).Y1 | Should Be 90
        ($segments | Where-Object { $_.Name -eq 'Bottom' }).Y2 | Should Be 110
    }

    It 'keeps the center point empty when a gap is present' {
        $segments = Get-CrosshairSegments -CenterX 100 -CenterY 100 -Length 6 -Gap 4
        $centerTouched = $false

        foreach ($segment in $segments) {
            if ($segment.X1 -eq $segment.X2 -and $segment.X1 -eq 100) {
                if ($segment.Y1 -le 100 -and $segment.Y2 -ge 100) {
                    $centerTouched = $true
                }
            }

            if ($segment.Y1 -eq $segment.Y2 -and $segment.Y1 -eq 100) {
                if ($segment.X1 -le 100 -and $segment.X2 -ge 100) {
                    $centerTouched = $true
                }
            }
        }

        $centerTouched | Should Be $false
    }

    It 'stays symmetric around the requested center point' {
        $segments = Get-CrosshairSegments -CenterX 50 -CenterY 60 -Length 8 -Gap 3

        $left = $segments | Where-Object { $_.Name -eq 'Left' }
        $right = $segments | Where-Object { $_.Name -eq 'Right' }
        $top = $segments | Where-Object { $_.Name -eq 'Top' }
        $bottom = $segments | Where-Object { $_.Name -eq 'Bottom' }

        (50 - $left.X2) | Should Be ($right.X1 - 50)
        (60 - $top.Y2) | Should Be ($bottom.Y1 - 60)
        ($left.X2 - $left.X1) | Should Be ($right.X2 - $right.X1)
        ($top.Y2 - $top.Y1) | Should Be ($bottom.Y2 - $bottom.Y1)
    }
}

Describe 'Get-CrosshairOverlayBounds' {
    It 'returns a compact odd-sized window centered on the target area' {
        $bounds = Get-CrosshairOverlayBounds -AreaLeft 0 -AreaTop 0 -AreaWidth 1920 -AreaHeight 1080 -Length 3 -Gap 4 -Thickness 1 -OutlineThickness 0

        $bounds.Left | Should Be 950
        $bounds.Top | Should Be 530
        $bounds.Width | Should Be 21
        $bounds.Height | Should Be 21
    }

    It 'expands when outline thickness increases' {
        $plainBounds = Get-CrosshairOverlayBounds -AreaLeft 0 -AreaTop 0 -AreaWidth 1920 -AreaHeight 1080 -Length 3 -Gap 4 -Thickness 1 -OutlineThickness 0
        $outlinedBounds = Get-CrosshairOverlayBounds -AreaLeft 0 -AreaTop 0 -AreaWidth 1920 -AreaHeight 1080 -Length 3 -Gap 4 -Thickness 1 -OutlineThickness 2

        $outlinedBounds.Width | Should BeGreaterThan $plainBounds.Width
        $outlinedBounds.Height | Should BeGreaterThan $plainBounds.Height
    }

    It 'applies crosshair offsets relative to the target center' {
        $bounds = Get-CrosshairOverlayBounds -AreaLeft 0 -AreaTop 0 -AreaWidth 1920 -AreaHeight 1080 -Length 3 -Gap 4 -Thickness 1 -OutlineThickness 0 -OffsetX 20 -OffsetY -10

        $bounds.Left | Should Be 970
        $bounds.Top | Should Be 520
    }
}

Describe 'Get-StatusOverlayBounds' {
    It 'anchors a status overlay to the top right corner' {
        $bounds = Get-StatusOverlayBounds -AreaLeft 100 -AreaTop 200 -AreaWidth 1600 -AreaHeight 900 -OverlayWidth 180 -OverlayHeight 30 -Position 'TopRight' -OffsetX 24 -OffsetY 16

        $bounds.Left | Should Be 1496
        $bounds.Top | Should Be 216
    }

    It 'anchors a status overlay to the bottom left corner' {
        $bounds = Get-StatusOverlayBounds -AreaLeft 100 -AreaTop 200 -AreaWidth 1600 -AreaHeight 900 -OverlayWidth 180 -OverlayHeight 30 -Position 'BottomLeft' -OffsetX 12 -OffsetY 18

        $bounds.Left | Should Be 112
        $bounds.Top | Should Be 1052
    }
}

Describe 'Resolve-StatusOverlayPlacement' {
    It 'keeps a dragged label near the top right anchored there' {
        $placement = Resolve-StatusOverlayPlacement -AreaWidth 400 -AreaHeight 240 -OverlayWidth 120 -OverlayHeight 24 -OverlayLeft 250 -OverlayTop 18

        $placement.Position | Should Be 'TopRight'
        $placement.OffsetX | Should Be 30
        $placement.OffsetY | Should Be 18
    }

    It 'moves a dragged label near the bottom left to that anchor' {
        $placement = Resolve-StatusOverlayPlacement -AreaWidth 400 -AreaHeight 240 -OverlayWidth 120 -OverlayHeight 24 -OverlayLeft 16 -OverlayTop 190

        $placement.Position | Should Be 'BottomLeft'
        $placement.OffsetX | Should Be 16
        $placement.OffsetY | Should Be 26
    }

    It 'clamps dragged coordinates that exceed the preview bounds' {
        $placement = Resolve-StatusOverlayPlacement -AreaWidth 400 -AreaHeight 240 -OverlayWidth 120 -OverlayHeight 24 -OverlayLeft 500 -OverlayTop -30

        $placement.Position | Should Be 'TopRight'
        $placement.OffsetX | Should Be 0
        $placement.OffsetY | Should Be 0
    }
}

Describe 'Convert-ProcessorPerformanceToMHz' {
    It 'converts processor performance percentage into an effective frequency' {
        $value = Convert-ProcessorPerformanceToMHz -BaseClockMHz 2100 -PerformancePercent 158.5

        $value | Should Be 3329
    }

    It 'returns zero when either input is non-positive' {
        (Convert-ProcessorPerformanceToMHz -BaseClockMHz 0 -PerformancePercent 150) | Should Be 0
        (Convert-ProcessorPerformanceToMHz -BaseClockMHz 2100 -PerformancePercent 0) | Should Be 0
    }
}

Describe 'Resolve-PreferredCpuStatusSnapshot' {
    It 'prefers vendor temperature when a real reading is available' {
        $snapshot = Resolve-PreferredCpuStatusSnapshot `
            -FallbackFrequencyMHz 3329 `
            -FallbackTemperatureC 35.2 `
            -FallbackApproximateTemperature $true `
            -VendorFrequencyMHz 4383 `
            -VendorTemperatureC 80 `
            -BaseClockMHz 2100

        $snapshot.FrequencyMHz | Should Be 4383
        $snapshot.TemperatureC | Should Be 80
        $snapshot.ApproximateTemperature | Should Be $false
    }

    It 'keeps the fallback frequency during vendor warm-up when vendor clock is stuck at base speed' {
        $snapshot = Resolve-PreferredCpuStatusSnapshot `
            -FallbackFrequencyMHz 3329 `
            -FallbackTemperatureC $null `
            -FallbackApproximateTemperature $false `
            -VendorFrequencyMHz 2100 `
            -VendorTemperatureC $null `
            -BaseClockMHz 2100

        $snapshot.FrequencyMHz | Should Be 3329
        $snapshot.TemperatureC | Should Be $null
    }

    It 'falls back cleanly when vendor telemetry is unavailable' {
        $snapshot = Resolve-PreferredCpuStatusSnapshot `
            -FallbackFrequencyMHz 3200 `
            -FallbackTemperatureC $null `
            -FallbackApproximateTemperature $false `
            -VendorFrequencyMHz 0 `
            -VendorTemperatureC 0 `
            -BaseClockMHz 2100

        $snapshot.FrequencyMHz | Should Be 3200
        $snapshot.TemperatureC | Should Be $null
        $snapshot.ApproximateTemperature | Should Be $false
    }
}

Describe 'Format-StatusOverlayText' {
    It 'formats direct sensor readings with frequency and temperature' {
        $text = Format-StatusOverlayText -FrequencyMHz 4387 -TemperatureC 94.6 -ApproximateTemperature $false -ShowTemperature $true

        $text | Should Be 'CPU 4.4GHz | 95C'
    }

    It 'shows a placeholder when temperature is unavailable' {
        $text = Format-StatusOverlayText -FrequencyMHz 3200 -TemperatureC $null -ApproximateTemperature $false -ShowTemperature $true

        $text | Should Be 'CPU 3.2GHz | --C'
    }

    It 'marks thermal-zone readings as approximate' {
        $text = Format-StatusOverlayText -FrequencyMHz 4100 -TemperatureC 35.2 -ApproximateTemperature $true -ShowTemperature $true

        $text | Should Be 'CPU 4.1GHz | TZ 35C'
    }

    It 'omits temperature text when the feature is disabled' {
        $text = Format-StatusOverlayText -FrequencyMHz 4100 -TemperatureC 35.2 -ApproximateTemperature $true -ShowTemperature $false

        $text | Should Be 'CPU 4.1GHz'
    }
}

Describe 'Test-CrosshairTargetProcess' {
    It 'matches allowed process names ignoring case' {
        $result = Test-CrosshairTargetProcess -ProcessName 'deltaforceclient-win64-shipping' -TargetProcessNames @('DeltaForceClient-Win64-Shipping', 'delta_force_launcher')
        $result | Should Be $true
    }

    It 'rejects unrelated process names' {
        $result = Test-CrosshairTargetProcess -ProcessName 'explorer' -TargetProcessNames @('DeltaForceClient-Win64-Shipping', 'delta_force_launcher')
        $result | Should Be $false
    }
}

Describe 'Get-CrosshairVisibilityState' {
    It 'shows immediately when the target is present' {
        $state = Get-CrosshairVisibilityState -WasVisible $false -ShouldShowTarget $true -MissCount 3 -HideAfterMisses 4

        $state.ShouldShow | Should Be $true
        $state.MissCount | Should Be 0
    }

    It 'keeps a visible overlay alive during short target misses' {
        $state = Get-CrosshairVisibilityState -WasVisible $true -ShouldShowTarget $false -MissCount 0 -HideAfterMisses 4

        $state.ShouldShow | Should Be $true
        $state.MissCount | Should Be 1
    }

    It 'hides after the configured number of misses' {
        $state = Get-CrosshairVisibilityState -WasVisible $true -ShouldShowTarget $false -MissCount 3 -HideAfterMisses 4

        $state.ShouldShow | Should Be $false
        $state.MissCount | Should Be 4
    }

    It 'stays hidden without accumulating misses when already hidden' {
        $state = Get-CrosshairVisibilityState -WasVisible $false -ShouldShowTarget $false -MissCount 2 -HideAfterMisses 4

        $state.ShouldShow | Should Be $false
        $state.MissCount | Should Be 0
    }
}
