using System.Diagnostics;
using System.Text;

namespace AspenBurner.Bench;

/// <summary>
/// Queries Event 37 counts by shelling out to PowerShell for reliable Windows filtering.
/// </summary>
public sealed class WindowsEvent37Reader : IEvent37Reader
{
    /// <inheritdoc />
    public int CountSince(DateTimeOffset startTime)
    {
        string script = string.Join(
            Environment.NewLine,
            "$ErrorActionPreference = 'SilentlyContinue'",
            $"$start = [DateTimeOffset]::Parse('{startTime:O}')",
            "$count = @(Get-WinEvent -FilterHashtable @{ LogName='System'; ProviderName='Microsoft-Windows-Kernel-Processor-Power'; Id=37; StartTime=$start.DateTime }).Count",
            "Write-Output $count");

        string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        ProcessStartInfo startInfo = new("powershell.exe", $"-NoProfile -EncodedCommand {encodedScript}")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to launch PowerShell for Event 37 query.");
        string stdout = process.StandardOutput.ReadToEnd().Trim();
        _ = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return int.TryParse(stdout, out int count) && count >= 0 ? count : 0;
    }
}
