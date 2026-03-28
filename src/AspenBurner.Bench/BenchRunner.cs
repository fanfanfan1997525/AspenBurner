namespace AspenBurner.Bench;

/// <summary>
/// Orchestrates both workloads, telemetry capture and final classification.
/// </summary>
public sealed class BenchRunner : IBenchRunner
{
    private readonly IFrameLoopExecutor frameLoopExecutor;
    private readonly IParallelExecutor parallelExecutor;
    private readonly Func<ITelemetrySource> telemetrySourceFactory;
    private readonly IEvent37Reader event37Reader;
    private readonly Func<DateTimeOffset> nowProvider;
    private readonly Func<string> processorNameProvider;
    private readonly TimeSpan telemetryInterval;

    /// <summary>
    /// Initializes a new runner instance.
    /// </summary>
    public BenchRunner(
        IFrameLoopExecutor frameLoopExecutor,
        IParallelExecutor parallelExecutor,
        Func<ITelemetrySource> telemetrySourceFactory,
        IEvent37Reader event37Reader,
        Func<DateTimeOffset>? nowProvider = null,
        Func<string>? processorNameProvider = null,
        TimeSpan? telemetryInterval = null)
    {
        this.frameLoopExecutor = frameLoopExecutor ?? throw new ArgumentNullException(nameof(frameLoopExecutor));
        this.parallelExecutor = parallelExecutor ?? throw new ArgumentNullException(nameof(parallelExecutor));
        this.telemetrySourceFactory = telemetrySourceFactory ?? throw new ArgumentNullException(nameof(telemetrySourceFactory));
        this.event37Reader = event37Reader ?? throw new ArgumentNullException(nameof(event37Reader));
        this.nowProvider = nowProvider ?? (() => DateTimeOffset.Now);
        this.processorNameProvider = processorNameProvider ?? ResolveProcessorName;
        this.telemetryInterval = telemetryInterval ?? TimeSpan.FromSeconds(1);
    }

    /// <inheritdoc />
    public async Task<BenchReport> RunAsync(BenchOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        DateTimeOffset startedAt = this.nowProvider();
        Event37Probe probe = new(this.event37Reader);
        probe.CaptureBaseline(startedAt);

        using ITelemetrySource telemetrySource = this.telemetrySourceFactory();
        using CancellationTokenSource samplingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<IReadOnlyList<AspenBurner.App.Telemetry.CpuStatusSnapshot>> telemetryTask =
            TelemetrySampler.CollectAsync(telemetrySource, this.telemetryInterval, samplingCts.Token);

        if (options.WarmupSeconds > 0)
        {
            await WarmUpAsync(options.WarmupSeconds, options.WorkerCount, cancellationToken);
        }

        int frameDurationSeconds = Math.Max(1, (int)Math.Round(options.DurationSeconds * 0.4, MidpointRounding.AwayFromZero));
        int parallelDurationSeconds = Math.Max(1, options.DurationSeconds - frameDurationSeconds);

        FrameLoopResult frameLoop = await this.frameLoopExecutor.RunAsync(
            frameDurationSeconds,
            options.FrameLoopTargetFps,
            options.WorkerCount,
            cancellationToken);

        SustainedParallelResult parallel = await this.parallelExecutor.RunAsync(
            parallelDurationSeconds,
            options.WorkerCount,
            cancellationToken);

        AspenBurner.App.Telemetry.CpuStatusSnapshot? finalSample = null;
        try
        {
            finalSample = telemetrySource.Capture();
        }
        catch
        {
            // The background sampler already gives us best-effort coverage.
        }

        samplingCts.Cancel();
        List<AspenBurner.App.Telemetry.CpuStatusSnapshot> telemetrySamples = (await telemetryTask).ToList();
        if (finalSample is not null)
        {
            telemetrySamples.Add(finalSample);
        }
        TelemetrySummary telemetry = TelemetrySampler.Summarize(telemetrySamples);
        int event37CountDelta = probe.CaptureDelta(startedAt);

        BenchEvidence evidence = new(
            telemetry.AverageFrequencyMHz,
            telemetry.PeakTemperatureC,
            event37CountDelta,
            frameLoop.MissRate,
            frameLoop.P95FrameMs,
            1000.0 / options.FrameLoopTargetFps,
            parallel.PerThreadBalanceRatio);
        BenchAssessment assessment = BenchClassifier.Classify(evidence);

        return new BenchReport(
            startedAt,
            options.DurationSeconds,
            this.processorNameProvider(),
            Environment.ProcessorCount,
            frameLoop,
            parallel,
            telemetry,
            event37CountDelta,
            assessment);
    }

    private static async Task WarmUpAsync(int warmupSeconds, int workerCount, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(warmupSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = BenchWorkloads.RunFrameWorkload(workerCount);
            await Task.Yield();
        }
    }

    private static string ResolveProcessorName()
    {
        try
        {
            using System.Management.ManagementObjectSearcher searcher = new("SELECT Name FROM Win32_Processor");
            foreach (System.Management.ManagementObject processor in searcher.Get())
            {
                if (processor["Name"] is string name && !string.IsNullOrWhiteSpace(name))
                {
                    return name.Trim();
                }
            }
        }
        catch
        {
            // Best-effort only.
        }

        return "Unknown CPU";
    }
}
