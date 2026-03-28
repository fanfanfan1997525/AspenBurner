namespace AspenBurner.Bench;

/// <summary>
/// Defines command-line options for one bench run.
/// </summary>
public sealed record BenchOptions(
    int DurationSeconds,
    int FrameLoopTargetFps,
    int WarmupSeconds,
    int WorkerCount)
{
    /// <summary>
    /// Parses command-line arguments into validated options.
    /// </summary>
    public static BenchOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        int durationSeconds = 75;
        int frameLoopTargetFps = 120;
        int warmupSeconds = 5;
        int workerCount = Math.Max(1, Environment.ProcessorCount);

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {argument}.", argument);
            }

            string value = args[index + 1];
            index++;

            switch (argument)
            {
                case "--duration-seconds":
                    durationSeconds = ParsePositiveInt(value, "duration");
                    break;
                case "--frame-loop-target-fps":
                    frameLoopTargetFps = ParsePositiveInt(value, "frame-loop-target-fps");
                    break;
                case "--warmup-seconds":
                    warmupSeconds = ParseNonNegativeInt(value, "warmup");
                    break;
                case "--worker-count":
                    workerCount = ParsePositiveInt(value, "worker-count");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument {argument}.", argument);
            }
        }

        return new BenchOptions(durationSeconds, frameLoopTargetFps, warmupSeconds, workerCount);
    }

    private static int ParsePositiveInt(string value, string name)
    {
        if (!int.TryParse(value, out int parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid {name} value: {value}.", name);
        }

        return parsed;
    }

    private static int ParseNonNegativeInt(string value, string name)
    {
        if (!int.TryParse(value, out int parsed) || parsed < 0)
        {
            throw new ArgumentException($"Invalid {name} value: {value}.", name);
        }

        return parsed;
    }
}
