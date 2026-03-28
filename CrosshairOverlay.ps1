param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config\crosshair.json'),
    [int]$AutoCloseMs = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Management

Import-Module (Join-Path $PSScriptRoot 'src\CrosshairOverlay.Core.psm1') -Force

$script:ControlCenterAssemblyDirectory = $null
$script:ControlCenterNativeDirectory = $null
$script:ControlCenterAssemblyResolveRegistered = $false
$script:ControlCenterAssemblyResolveHandler = $null

# @brief Load and validate the final crosshair configuration.
# @param ConfigFile Path to the JSON config file.
# @return The validated configuration object.
function Get-ValidatedCrosshairConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFile
    )

    $defaultConfig = Get-CrosshairDefaultConfig
    $overrideConfig = $null

    if (Test-Path -LiteralPath $ConfigFile) {
        $overrideConfig = Get-Content -Raw -LiteralPath $ConfigFile | ConvertFrom-Json
    }

    $config = Merge-CrosshairConfig -DefaultConfig $defaultConfig -OverrideConfig $overrideConfig
    Assert-CrosshairConfig -Config $config
    return $config
}

# @brief Return the process names that are allowed to show the overlay.
# @return Array of process names.
function Get-TargetProcessNames {
    [CmdletBinding()]
    param()

    @(
        'DeltaForceClient-Win64-Shipping'
        'delta_force_launcher'
    )
}

# @brief Read the current foreground window process and bounds.
# @return Foreground window information or null when unavailable.
function Get-ForegroundWindowInfo {
    [CmdletBinding()]
    param()

    $windowHandle = [CrosshairOverlay.NativeMethods]::GetForegroundWindow()
    if ($windowHandle -eq [IntPtr]::Zero) {
        return $null
    }

    $processId = 0
    [void][CrosshairOverlay.NativeMethods]::GetWindowThreadProcessId($windowHandle, [ref]$processId)
    if ($processId -le 0) {
        return $null
    }

    try {
        $process = Get-Process -Id $processId -ErrorAction Stop
    }
    catch {
        return $null
    }

    $rect = New-Object 'CrosshairOverlay.NativeMethods+RECT'
    if (-not [CrosshairOverlay.NativeMethods]::GetWindowRect($windowHandle, [ref]$rect)) {
        return $null
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) {
        return $null
    }

    [pscustomobject]@{
        Handle      = $windowHandle
        ProcessName = $process.ProcessName
        Left        = $rect.Left
        Top         = $rect.Top
        Width       = $width
        Height      = $height
    }
}

# @brief Escape a WMI string literal for use inside a WQL query.
# @param Value Raw text value.
# @return Escaped text value.
function Escape-WmiStringLiteral {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.Replace('\', '\\').Replace("'", "''")
}

# @brief Execute a lightweight WMI query in a specific namespace.
# @param NamespacePath WMI namespace path.
# @param QueryText WQL query text.
# @return Matching management objects or an empty array.
function Invoke-WmiQuery {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$NamespacePath,

        [Parameter(Mandatory = $true)]
        [string]$QueryText
    )

    $scope = [System.Management.ManagementScope]::new(("\\.\{0}" -f $NamespacePath))
    $query = [System.Management.ObjectQuery]::new($QueryText)
    $searcher = [System.Management.ManagementObjectSearcher]::new($scope, $query)

    try {
        return @($searcher.Get())
    }
    finally {
        $searcher.Dispose()
    }
}

# @brief Pick the best direct CPU temperature sensor exposed by hardware-monitor namespaces.
# @return Sensor definition or null when no trustworthy CPU temperature sensor exists.
function Get-PreferredCpuTemperatureSensor {
    [CmdletBinding()]
    param()

    $sensorNamespaces = @(
        'root\LibreHardwareMonitor'
        'root\OpenHardwareMonitor'
    )

    $bestSensor = $null
    $bestScore = -1

    foreach ($namespacePath in $sensorNamespaces) {
        try {
            $sensors = Invoke-WmiQuery `
                -NamespacePath $namespacePath `
                -QueryText "SELECT Name, Identifier, SensorType FROM Sensor WHERE SensorType = 'Temperature'"
        }
        catch {
            continue
        }

        foreach ($sensor in $sensors) {
            $name = [string]$sensor.Name
            $identifier = [string]$sensor.Identifier
            $sensorText = ("{0} {1}" -f $name, $identifier).ToLowerInvariant()
            $score = 0

            if ($sensorText.Contains('cpu package')) {
                $score = 100
            }
            elseif ($sensorText.Contains('tctl') -or $sensorText.Contains('tdie')) {
                $score = 90
            }
            elseif ($sensorText.Contains('core max')) {
                $score = 80
            }
            elseif ($sensorText.Contains('package') -and $sensorText.Contains('cpu')) {
                $score = 70
            }
            elseif ($sensorText.Contains('/cpu/') -or $sensorText.Contains('\cpu\') -or $sensorText.Contains('cpu core')) {
                $score = 50
            }

            if ($score -gt $bestScore) {
                $bestScore = $score
                $bestSensor = [pscustomobject]@{
                    NamespacePath = $namespacePath
                    Identifier    = $identifier
                    Name          = $name
                }
            }
        }
    }

    if ($bestScore -lt 50) {
        return $null
    }

    return $bestSensor
}

# @brief Read the CPU base clock through WMI.
# @return Base clock in MHz or null when unavailable.
function Get-WmiCpuBaseClockMHz {
    [CmdletBinding()]
    param()

    try {
        $processors = Invoke-WmiQuery -NamespacePath 'root\cimv2' -QueryText 'SELECT MaxClockSpeed FROM Win32_Processor'
        if ($processors.Count -eq 0) {
            return $null
        }

        $values = @(
            foreach ($processor in $processors) {
                if ($null -ne $processor.MaxClockSpeed) {
                    [double]$processor.MaxClockSpeed
                }
            }
        )

        if ($values.Count -eq 0) {
            return $null
        }

        return ($values | Measure-Object -Average).Average
    }
    catch {
        return $null
    }
}

# @brief Locate the installed Control Center package and its native DCHU directory.
# @return Installation context or null when the vendor package is not present.
function Get-ControlCenterInstallContext {
    [CmdletBinding()]
    param()

    $assemblyPath = $null

    try {
        $package = Get-AppxPackage -Name 'CLEVOCO.FnhotkeysandOSD' -ErrorAction Stop |
            Sort-Object -Property Version -Descending |
            Select-Object -First 1

        if ($null -ne $package -and -not [string]::IsNullOrWhiteSpace($package.InstallLocation)) {
            $candidateAssemblyPath = Join-Path $package.InstallLocation 'FnKey\CC40\CC40.exe'
            if (Test-Path -LiteralPath $candidateAssemblyPath) {
                $assemblyPath = $candidateAssemblyPath
            }
        }
    }
    catch {
    }

    if ([string]::IsNullOrWhiteSpace($assemblyPath)) {
        return $null
    }

    $nativeDirectory = $null
    foreach ($candidateNativeDirectory in @(
            'C:\Program Files (x86)\ControlCenter\DCHU',
            'C:\Program Files\ControlCenter\DCHU',
            (Join-Path (Split-Path -Parent (Split-Path -Parent $assemblyPath)) 'DCHU')
        )) {
        if (-not [string]::IsNullOrWhiteSpace($candidateNativeDirectory) -and (Test-Path -LiteralPath $candidateNativeDirectory)) {
            $nativeDirectory = $candidateNativeDirectory
            break
        }
    }

    return [pscustomobject]@{
        AssemblyPath    = $assemblyPath
        NativeDirectory = $nativeDirectory
    }
}

# @brief Register an assembly resolver so Control Center can load its managed dependencies.
# @param AssemblyDirectory Directory that contains CC40.exe and its managed DLLs.
# @param NativeDirectory Directory that contains the accessible DCHU native DLLs.
function Register-ControlCenterAssemblyResolver {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AssemblyDirectory,

        [Parameter()]
        [AllowNull()]
        [string]$NativeDirectory
    )

    $script:ControlCenterAssemblyDirectory = $AssemblyDirectory
    $script:ControlCenterNativeDirectory = $NativeDirectory

    if ($script:ControlCenterAssemblyResolveRegistered) {
        return
    }

    $script:ControlCenterAssemblyResolveHandler = [System.ResolveEventHandler]{
        param($sender, $eventArgs)

        $requestedFileName = ('{0}.dll' -f (($eventArgs.Name -split ',')[0]))
        foreach ($searchDirectory in @($script:ControlCenterAssemblyDirectory, $script:ControlCenterNativeDirectory)) {
            if ([string]::IsNullOrWhiteSpace($searchDirectory)) {
                continue
            }

            $candidatePath = Join-Path $searchDirectory $requestedFileName
            if (-not (Test-Path -LiteralPath $candidatePath)) {
                continue
            }

            try {
                return [System.Reflection.Assembly]::LoadFile($candidatePath)
            }
            catch {
            }
        }

        return $null
    }

    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($script:ControlCenterAssemblyResolveHandler)
    $script:ControlCenterAssemblyResolveRegistered = $true
}

# @brief Create a lightweight provider backed by the vendor Control Center CPU interface.
# @return Provider state or null when the vendor path is unavailable.
function New-ControlCenterCpuProvider {
    [CmdletBinding()]
    param()

    $installContext = Get-ControlCenterInstallContext
    if ($null -eq $installContext) {
        return $null
    }

    if (-not [string]::IsNullOrWhiteSpace($installContext.NativeDirectory)) {
        try {
            [void][CrosshairOverlay.NativeMethods]::SetDllDirectory($installContext.NativeDirectory)
        }
        catch {
        }
    }

    $assemblyDirectory = Split-Path -Parent $installContext.AssemblyPath
    Register-ControlCenterAssemblyResolver -AssemblyDirectory $assemblyDirectory -NativeDirectory $installContext.NativeDirectory

    try {
        $assembly = [System.Reflection.Assembly]::LoadFile($installContext.AssemblyPath)
        $cpuInterfaceType = $assembly.GetType('CC40.PageSystemElement.Interface_CPU', $false)
        if ($null -eq $cpuInterfaceType) {
            return $null
        }

        $cpuInterface = [System.Activator]::CreateInstance($cpuInterfaceType)
        $initMethod = $cpuInterfaceType.GetMethod('Init')
        if ($null -eq $initMethod) {
            return $null
        }

        [void]$initMethod.Invoke($cpuInterface, @())

        return [pscustomobject]@{
            Interface        = $cpuInterface
            ClockProperty    = $cpuInterfaceType.GetProperty('Clock')
            TemperatureProperty = $cpuInterfaceType.GetProperty('Temperature')
            UsageProperty    = $cpuInterfaceType.GetProperty('Usage')
            StopMethod       = $cpuInterfaceType.GetMethod('StopCpuInfoTimer')
        }
    }
    catch {
        return $null
    }
}

# @brief Read a single vendor-backed CPU snapshot from Control Center.
# @param Provider Vendor provider returned by New-ControlCenterCpuProvider.
# @return Frequency and temperature values or nulls when unavailable.
function Get-ControlCenterCpuSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Provider
    )

    try {
        $clockMHz = $null
        if ($null -ne $Provider.ClockProperty) {
            $rawClock = $Provider.ClockProperty.GetValue($Provider.Interface, $null)
            if ($null -ne $rawClock) {
                $clockMHz = [int]$rawClock
            }
        }

        if ($null -ne $clockMHz -and ($clockMHz -le 0 -or $clockMHz -gt 10000)) {
            $clockMHz = $null
        }

        $temperatureC = $null
        if ($null -ne $Provider.TemperatureProperty) {
            $rawTemperature = $Provider.TemperatureProperty.GetValue($Provider.Interface, $null)
            if ($null -ne $rawTemperature) {
                $temperatureC = [int]$rawTemperature
            }
        }

        if ($null -ne $temperatureC -and ($temperatureC -le 0 -or $temperatureC -gt 130)) {
            $temperatureC = $null
        }

        $usagePercent = $null
        if ($null -ne $Provider.UsageProperty) {
            $rawUsage = $Provider.UsageProperty.GetValue($Provider.Interface, $null)
            if ($null -ne $rawUsage) {
                $usagePercent = [int]$rawUsage
            }
        }

        return [pscustomobject]@{
            FrequencyMHz = $clockMHz
            TemperatureC = $temperatureC
            UsagePercent = $usagePercent
        }
    }
    catch {
        return [pscustomobject]@{
            FrequencyMHz = $null
            TemperatureC = $null
            UsagePercent = $null
        }
    }
}

# @brief Create a low-overhead provider object for CPU frequency and temperature.
# @return Provider state used by the overlay refresh loop.
function New-CpuStatusProvider {
    [CmdletBinding()]
    param()

    $performanceCounter = $null
    try {
        $performanceCounter = [System.Diagnostics.PerformanceCounter]::new('Processor Information', '% Processor Performance', '_Total')
        [void]$performanceCounter.NextValue()
    }
    catch {
        if ($null -ne $performanceCounter) {
            $performanceCounter.Dispose()
        }

        $performanceCounter = $null
    }

    $controlCenterCpuProvider = New-ControlCenterCpuProvider
    $directTemperatureSensor = Get-PreferredCpuTemperatureSensor
    $baseClockMHz = Get-WmiCpuBaseClockMHz

    return [pscustomobject]@{
        ControlCenterCpuProvider = $controlCenterCpuProvider
        PerformanceCounter       = $performanceCounter
        BaseClockMHz             = $baseClockMHz
        DirectTemperatureSensor = $directTemperatureSensor
    }
}

# @brief Query the configured direct temperature sensor.
# @param SensorDefinition Sensor definition returned by Get-PreferredCpuTemperatureSensor.
# @return Temperature in Celsius or null when unavailable.
function Get-DirectCpuTemperatureC {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$SensorDefinition
    )

    try {
        $escapedIdentifier = Escape-WmiStringLiteral -Value ([string]$SensorDefinition.Identifier)
        $queryText = "SELECT Value FROM Sensor WHERE Identifier = '$escapedIdentifier'"
        $sensorValues = Invoke-WmiQuery -NamespacePath $SensorDefinition.NamespacePath -QueryText $queryText
        $firstSensor = $sensorValues | Select-Object -First 1
        if ($null -eq $firstSensor -or $null -eq $firstSensor.Value) {
            return $null
        }

        return [double]$firstSensor.Value
    }
    catch {
        return $null
    }
}

# @brief Read a single CPU status snapshot.
# @param Provider Provider state returned by New-CpuStatusProvider.
# @return Frequency and temperature data for the overlay text.
function Get-CpuStatusSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Provider
    )

    $frequencyMHz = $null
    if ($null -ne $Provider.PerformanceCounter -and $null -ne $Provider.BaseClockMHz -and $Provider.BaseClockMHz -gt 0) {
        try {
            $performancePercent = [double]$Provider.PerformanceCounter.NextValue()
            $frequencyMHz = [double](Convert-ProcessorPerformanceToMHz -BaseClockMHz $Provider.BaseClockMHz -PerformancePercent $performancePercent)
        }
        catch {
            $frequencyMHz = $null
        }
    }

    if ($null -eq $frequencyMHz -or $frequencyMHz -le 0) {
        $frequencyMHz = $Provider.BaseClockMHz
    }

    if ($null -eq $frequencyMHz -or $frequencyMHz -le 0) {
        $frequencyMHz = 0
    }

    $temperatureC = $null
    $approximateTemperature = $false

    if ($null -ne $Provider.DirectTemperatureSensor) {
        $temperatureC = Get-DirectCpuTemperatureC -SensorDefinition $Provider.DirectTemperatureSensor
    }

    $vendorSnapshot = $null
    if ($null -ne $Provider.ControlCenterCpuProvider) {
        $vendorSnapshot = Get-ControlCenterCpuSnapshot -Provider $Provider.ControlCenterCpuProvider
    }

    return Resolve-PreferredCpuStatusSnapshot `
        -FallbackFrequencyMHz $frequencyMHz `
        -FallbackTemperatureC $temperatureC `
        -FallbackApproximateTemperature $approximateTemperature `
        -VendorFrequencyMHz $(if ($null -ne $vendorSnapshot) { $vendorSnapshot.FrequencyMHz } else { $null }) `
        -VendorTemperatureC $(if ($null -ne $vendorSnapshot) { $vendorSnapshot.TemperatureC } else { $null }) `
        -BaseClockMHz $Provider.BaseClockMHz
}

# @brief Dispose any provider resources that keep system handles open.
# @param Provider Provider state returned by New-CpuStatusProvider.
function Close-CpuStatusProvider {
    [CmdletBinding()]
    param(
        [Parameter()]
        [AllowNull()]
        [psobject]$Provider
    )

    if ($null -eq $Provider) {
        return
    }

    if ($null -ne $Provider.ControlCenterCpuProvider -and $null -ne $Provider.ControlCenterCpuProvider.StopMethod) {
        try {
            [void]$Provider.ControlCenterCpuProvider.StopMethod.Invoke($Provider.ControlCenterCpuProvider.Interface, @())
        }
        catch {
        }
    }

    foreach ($resourceName in @('PerformanceCounter')) {
        $resource = $Provider.$resourceName
        if ($null -ne $resource) {
            try {
                $resource.Dispose()
            }
            catch {
            }
        }
    }
}

if (-not ('CrosshairOverlay.NativeMethods' -as [type])) {
    Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrosshairOverlay
{
    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetDllDirectory(string lpPathName);
    }

    /// <summary>
    /// A transparent click-through overlay that draws a compact center crosshair.
    /// </summary>
    public sealed class CrosshairOverlayForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        public Color CrosshairColor { get; set; }
        public int LineLength { get; set; }
        public int Gap { get; set; }
        public int Thickness { get; set; }
        public int OutlineThickness { get; set; }
        public bool ShowLeftArm { get; set; }
        public bool ShowRightArm { get; set; }
        public bool ShowTopArm { get; set; }
        public bool ShowBottomArm { get; set; }

        public CrosshairOverlayForm()
        {
            CrosshairColor = Color.Lime;
            LineLength = 6;
            Gap = 4;
            Thickness = 2;
            OutlineThickness = 1;
            ShowLeftArm = true;
            ShowRightArm = true;
            ShowTopArm = true;
            ShowBottomArm = true;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_TOOLWINDOW = 0x80;
                const int WS_EX_LAYERED = 0x80000;

                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
                return createParams;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PinTopMost();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.None;

            int centerX = ClientSize.Width / 2;
            int centerY = ClientSize.Height / 2;
            DrawCrosshair(e.Graphics, centerX, centerY);
        }

        public void PinTopMost()
        {
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void DrawCrosshair(Graphics graphics, int centerX, int centerY)
        {
            if (OutlineThickness > 0)
            {
                using (Pen outlinePen = CreatePen(Color.FromArgb(CrosshairColor.A, Color.Black), Thickness + (OutlineThickness * 2)))
                {
                    DrawSegments(graphics, outlinePen, centerX, centerY);
                }
            }

            using (Pen crosshairPen = CreatePen(CrosshairColor, Thickness))
            {
                DrawSegments(graphics, crosshairPen, centerX, centerY);
            }
        }

        private static Pen CreatePen(Color color, int width)
        {
            Pen pen = new Pen(color, width);
            pen.StartCap = LineCap.Square;
            pen.EndCap = LineCap.Square;
            return pen;
        }

        private void DrawSegments(Graphics graphics, Pen pen, int centerX, int centerY)
        {
            if (ShowLeftArm)
            {
                graphics.DrawLine(pen, centerX - Gap - LineLength, centerY, centerX - Gap, centerY);
            }

            if (ShowRightArm)
            {
                graphics.DrawLine(pen, centerX + Gap, centerY, centerX + Gap + LineLength, centerY);
            }

            if (ShowTopArm)
            {
                graphics.DrawLine(pen, centerX, centerY - Gap - LineLength, centerX, centerY - Gap);
            }

            if (ShowBottomArm)
            {
                graphics.DrawLine(pen, centerX, centerY + Gap, centerX, centerY + Gap + LineLength);
            }
        }
    }

    /// <summary>
    /// A transparent click-through overlay that draws a compact CPU status label.
    /// </summary>
    public sealed class StatusOverlayForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int PaddingX = 6;
        private const int PaddingY = 4;
        private string displayText = "CPU --.-GHz | --C";
        private float fontPointSize = 11f;

        public Color TextColor { get; private set; }

        public StatusOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;
            TextColor = Color.Yellow;
            UpdateWindowSize();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_TOOLWINDOW = 0x80;
                const int WS_EX_LAYERED = 0x80000;

                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
                return createParams;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PinTopMost();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Font font = CreateOverlayFont())
            {
                TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(e.Graphics, displayText, font, new Point(PaddingX + 1, PaddingY + 1), Color.FromArgb(Math.Max(TextColor.A / 2, 90), Color.Black), flags);
                TextRenderer.DrawText(e.Graphics, displayText, font, new Point(PaddingX, PaddingY), TextColor, flags);
            }
        }

        public void ApplyStatus(string text, float pointSize, Color textColor)
        {
            string nextText = text ?? string.Empty;
            bool changed = displayText != nextText || Math.Abs(fontPointSize - pointSize) > 0.01f || TextColor.ToArgb() != textColor.ToArgb();
            displayText = nextText;
            fontPointSize = pointSize;
            TextColor = textColor;
            UpdateWindowSize();

            if (changed)
            {
                Invalidate();
            }
        }

        public void PinTopMost()
        {
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private Font CreateOverlayFont()
        {
            return new Font("Consolas", fontPointSize, FontStyle.Bold, GraphicsUnit.Point);
        }

        private void UpdateWindowSize()
        {
            using (Font font = CreateOverlayFont())
            {
                TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                Size textSize = TextRenderer.MeasureText(displayText, font, new Size(int.MaxValue, int.MaxValue), flags);
                Width = Math.Max(40, textSize.Width + (PaddingX * 2));
                Height = Math.Max(18, textSize.Height + (PaddingY * 2));
            }
        }
    }
}
"@
}

# @brief Return a compact signature for the config file contents.
# @param ConfigFile Path to the JSON config file.
# @return Config signature string.
function Get-ConfigFileSignature {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFile
    )

    if (-not (Test-Path -LiteralPath $ConfigFile)) {
        return 'missing'
    }

    $item = Get-Item -LiteralPath $ConfigFile
    return '{0}|{1}' -f $item.LastWriteTimeUtc.Ticks, $item.Length
}

# @brief Ensure the optional CPU status window matches the current config.
# @param Config Current overlay config.
function Ensure-StatusOverlayState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    if ($Config.StatusEnabled) {
        if ($null -eq $script:StatusForm) {
            $script:StatusForm = [CrosshairOverlay.StatusOverlayForm]::new()
        }

        if ($null -eq $script:StatusProvider) {
            $script:StatusProvider = New-CpuStatusProvider
        }

        $script:StatusForm.ApplyStatus('CPU --.-GHz | --C', [float]$Config.StatusFontSize, $script:CurrentStatusTextColor)
        return
    }

    if ($null -ne $script:StatusForm) {
        if ($script:StatusForm.Visible) {
            $script:StatusForm.Hide()
        }

        $script:StatusForm.Close()
        $script:StatusForm.Dispose()
        $script:StatusForm = $null
    }

    Close-CpuStatusProvider -Provider $script:StatusProvider
    $script:StatusProvider = $null
}

# @brief Apply a validated config to the live overlay forms.
# @param Config Validated overlay config.
function Apply-OverlayConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $script:CurrentConfig = $Config
    $script:CurrentCrosshairColor = Resolve-CrosshairColor `
        -ColorName $Config.Color `
        -Opacity $Config.Opacity `
        -Red $Config.ColorR `
        -Green $Config.ColorG `
        -Blue $Config.ColorB
    $script:CurrentStatusTextColor = Resolve-StatusTextColor -ColorName $Config.StatusTextColor -Opacity $Config.StatusOpacity

    $script:OverlayForm.CrosshairColor = $script:CurrentCrosshairColor
    $script:OverlayForm.LineLength = $Config.Length
    $script:OverlayForm.Gap = $Config.Gap
    $script:OverlayForm.Thickness = $Config.Thickness
    $script:OverlayForm.OutlineThickness = $Config.OutlineThickness
    $script:OverlayForm.ShowLeftArm = $Config.ShowLeftArm
    $script:OverlayForm.ShowRightArm = $Config.ShowRightArm
    $script:OverlayForm.ShowTopArm = $Config.ShowTopArm
    $script:OverlayForm.ShowBottomArm = $Config.ShowBottomArm
    $script:OverlayForm.Invalidate()

    Ensure-StatusOverlayState -Config $Config
    $script:LastStatusUpdatedAt = [datetime]::MinValue
    $script:LastStatusText = ''
}

# @brief Reload config from disk when it changes.
# @param ConfigFile Path to the JSON config file.
function Update-OverlayConfigFromDisk {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFile
    )

    $nextSignature = Get-ConfigFileSignature -ConfigFile $ConfigFile
    if ($nextSignature -eq $script:ConfigFileSignature) {
        return
    }

    try {
        $nextConfig = Get-ValidatedCrosshairConfig -ConfigFile $ConfigFile
        Apply-OverlayConfig -Config $nextConfig
        $script:ConfigFileSignature = $nextSignature
    }
    catch {
        # Ignore transient partial writes and retry on the next timer pass.
    }
}

$targetProcessNames = Get-TargetProcessNames

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

$form = [CrosshairOverlay.CrosshairOverlayForm]::new()
$script:OverlayForm = $form
$script:StatusForm = $null
$script:StatusProvider = $null
$script:LastStatusUpdatedAt = [datetime]::MinValue
$script:LastStatusText = ''
$script:CurrentConfig = $null
$script:CurrentCrosshairColor = $null
$script:CurrentStatusTextColor = $null
$script:ConfigFileSignature = Get-ConfigFileSignature -ConfigFile $ConfigPath
$script:LastConfigPollAt = [datetime]::MinValue

Apply-OverlayConfig -Config (Get-ValidatedCrosshairConfig -ConfigFile $ConfigPath)

$hideAfterMisses = 4
$missCount = 0
$lastPinnedAt = [datetime]::MinValue
$lastTargetHandle = [IntPtr]::Zero

$form.Add_Shown({
    $form.Hide()
    if ($null -ne $script:StatusForm) {
        $script:StatusForm.Hide()
    }
})

$stateTimer = [System.Windows.Forms.Timer]::new()
$stateTimer.Interval = 200
$stateTimer.Add_Tick({
    $now = Get-Date
    if (($now - $script:LastConfigPollAt).TotalMilliseconds -ge 250) {
        Update-OverlayConfigFromDisk -ConfigFile $ConfigPath
        $script:LastConfigPollAt = $now
    }

    $foregroundWindow = Get-ForegroundWindowInfo
    $shouldShowTarget = (
        $null -ne $foregroundWindow -and
        (Test-CrosshairTargetProcess -ProcessName $foregroundWindow.ProcessName -TargetProcessNames $targetProcessNames)
    )

    $visibilityState = Get-CrosshairVisibilityState `
        -WasVisible $form.Visible `
        -ShouldShowTarget $shouldShowTarget `
        -MissCount $missCount `
        -HideAfterMisses $hideAfterMisses

    $missCount = $visibilityState.MissCount

    if (-not $visibilityState.ShouldShow) {
        if ($form.Visible) {
            $form.Hide()
        }

        if ($null -ne $script:StatusForm -and $script:StatusForm.Visible) {
            $script:StatusForm.Hide()
        }

        $lastTargetHandle = [IntPtr]::Zero
        return
    }

    if (-not $shouldShowTarget) {
        return
    }

    $overlayBounds = Get-CrosshairOverlayBounds `
        -AreaLeft $foregroundWindow.Left `
        -AreaTop $foregroundWindow.Top `
        -AreaWidth $foregroundWindow.Width `
        -AreaHeight $foregroundWindow.Height `
        -Length $script:CurrentConfig.Length `
        -Gap $script:CurrentConfig.Gap `
        -Thickness $script:CurrentConfig.Thickness `
        -OutlineThickness $script:CurrentConfig.OutlineThickness `
        -OffsetX $script:CurrentConfig.OffsetX `
        -OffsetY $script:CurrentConfig.OffsetY

    $newCrosshairBounds = [System.Drawing.Rectangle]::new($overlayBounds.Left, $overlayBounds.Top, $overlayBounds.Width, $overlayBounds.Height)
    if (
        $form.Left -ne $newCrosshairBounds.Left -or
        $form.Top -ne $newCrosshairBounds.Top -or
        $form.Width -ne $newCrosshairBounds.Width -or
        $form.Height -ne $newCrosshairBounds.Height
    ) {
        $form.Bounds = $newCrosshairBounds
        $form.Invalidate()
    }

    if ($null -ne $script:StatusForm) {
        $needsStatusRefresh = (
            -not $script:StatusForm.Visible -or
            ($now - $script:LastStatusUpdatedAt).TotalMilliseconds -ge $script:CurrentConfig.StatusRefreshMs
        )

        if ($needsStatusRefresh) {
            $statusSnapshot = Get-CpuStatusSnapshot -Provider $script:StatusProvider
            $statusText = Format-StatusOverlayText `
                -FrequencyMHz $statusSnapshot.FrequencyMHz `
                -TemperatureC $statusSnapshot.TemperatureC `
                -ApproximateTemperature $statusSnapshot.ApproximateTemperature `
                -ShowTemperature $script:CurrentConfig.StatusShowTemperature

            if ($statusText -ne $script:LastStatusText) {
                $script:StatusForm.ApplyStatus($statusText, [float]$script:CurrentConfig.StatusFontSize, $script:CurrentStatusTextColor)
                $script:LastStatusText = $statusText
            }

            $script:LastStatusUpdatedAt = $now
        }

        $statusBounds = Get-StatusOverlayBounds `
            -AreaLeft $foregroundWindow.Left `
            -AreaTop $foregroundWindow.Top `
            -AreaWidth $foregroundWindow.Width `
            -AreaHeight $foregroundWindow.Height `
            -OverlayWidth $script:StatusForm.Width `
            -OverlayHeight $script:StatusForm.Height `
            -Position $script:CurrentConfig.StatusPosition `
            -OffsetX $script:CurrentConfig.StatusOffsetX `
            -OffsetY $script:CurrentConfig.StatusOffsetY

        $newStatusBounds = [System.Drawing.Rectangle]::new($statusBounds.Left, $statusBounds.Top, $statusBounds.Width, $statusBounds.Height)
        if (
            $script:StatusForm.Left -ne $newStatusBounds.Left -or
            $script:StatusForm.Top -ne $newStatusBounds.Top -or
            $script:StatusForm.Width -ne $newStatusBounds.Width -or
            $script:StatusForm.Height -ne $newStatusBounds.Height
        ) {
            $script:StatusForm.Bounds = $newStatusBounds
        }

        if ($form.Visible -and -not $script:StatusForm.Visible) {
            $script:StatusForm.Show()
            $script:StatusForm.PinTopMost()
        }
    }

    if (-not $form.Visible) {
        $form.Show()
        $form.PinTopMost()
        if ($null -ne $script:StatusForm) {
            $script:StatusForm.Show()
            $script:StatusForm.PinTopMost()
        }

        $lastPinnedAt = $now
    }

    if ($foregroundWindow.Handle -ne $lastTargetHandle) {
        $form.PinTopMost()
        if ($null -ne $script:StatusForm) {
            $script:StatusForm.PinTopMost()
        }

        $lastPinnedAt = $now
        $lastTargetHandle = $foregroundWindow.Handle
    }

    if (($now - $lastPinnedAt).TotalSeconds -ge 2) {
        $form.PinTopMost()
        if ($null -ne $script:StatusForm) {
            $script:StatusForm.PinTopMost()
        }

        $lastPinnedAt = $now
    }
})
$stateTimer.Start()

$form.Add_FormClosed({
    $stateTimer.Stop()
    $stateTimer.Dispose()

    if ($null -ne $script:StatusForm) {
        $script:StatusForm.Close()
        $script:StatusForm.Dispose()
    }

    Close-CpuStatusProvider -Provider $script:StatusProvider
})

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
