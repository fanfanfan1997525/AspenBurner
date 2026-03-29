using System.Text;

namespace AspenBurner.Bench;

/// <summary>
/// Converts a bench report into terminal-friendly text.
/// </summary>
public static class ReportFormatter
{
    /// <summary>
    /// Formats one bench report.
    /// </summary>
    public static string Format(BenchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder builder = new();
        builder.AppendLine("CPU 验证报告");
        builder.AppendLine($"StartedAt: {report.StartedAt:O}");
        builder.AppendLine($"DurationSeconds: {report.DurationSeconds}");
        builder.AppendLine($"Processor: {report.ProcessorName}");
        builder.AppendLine($"LogicalCores: {report.LogicalCoreCount}");
        builder.AppendLine();
        builder.AppendLine("FrameLoop");
        builder.AppendLine($"  Frames: {report.FrameLoop.FrameCount}");
        builder.AppendLine($"  AvgFrameMs: {report.FrameLoop.AverageFrameMs:F2}");
        builder.AppendLine($"  P95FrameMs: {report.FrameLoop.P95FrameMs:F2}");
        builder.AppendLine($"  P99FrameMs: {report.FrameLoop.P99FrameMs:F2}");
        builder.AppendLine($"  MissRate: {report.FrameLoop.MissRate:P2}");
        builder.AppendLine($"  WorkUnitsPerSecond: {report.FrameLoop.WorkUnitsPerSecond}");
        builder.AppendLine();
        builder.AppendLine("SustainedParallel");
        builder.AppendLine($"  TotalOperations: {report.Parallel.TotalOperations}");
        builder.AppendLine($"  OperationsPerSecond: {report.Parallel.OperationsPerSecond:F0}");
        builder.AppendLine($"  PerThreadBalanceRatio: {report.Parallel.PerThreadBalanceRatio:F2}");
        builder.AppendLine();
        builder.AppendLine("Telemetry");
        builder.AppendLine($"  AverageFrequencyMHz: {report.Telemetry.AverageFrequencyMHz}");
        builder.AppendLine($"  MaxFrequencyMHz: {report.Telemetry.MaxFrequencyMHz}");
        builder.AppendLine($"  AverageTemperatureC: {(report.Telemetry.AverageTemperatureC?.ToString("F0") ?? "--")}");
        builder.AppendLine($"  PeakTemperatureC: {(report.Telemetry.PeakTemperatureC?.ToString("F0") ?? "--")}");
        builder.AppendLine($"  Samples: {report.Telemetry.SampleCount}");
        builder.AppendLine($"  Source: {report.Telemetry.Source}");
        builder.AppendLine($"Event37Delta: {report.Event37CountDelta}");
        builder.AppendLine();
        builder.AppendLine($"Conclusion: {report.Assessment.Summary}");
        foreach (string reason in report.Assessment.Reasons)
        {
            builder.AppendLine($"- {reason}");
        }

        return builder.ToString().TrimEnd();
    }
}
