namespace AspenBurner.App.Core;

/// <summary>
/// Formats compact CPU status strings for the overlay.
/// </summary>
public static class StatusTextFormatter
{
    /// <summary>
    /// Formats a CPU status string using legacy-compatible wording.
    /// </summary>
    public static string FormatCpuStatus(int frequencyMHz, double? temperatureC, bool approximateTemperature, bool showTemperature)
    {
        double frequencyGHz = Math.Round(frequencyMHz / 1000.0, 1);
        string text = $"CPU {frequencyGHz:N1}GHz";

        if (!showTemperature)
        {
            return text;
        }

        if (!temperatureC.HasValue)
        {
            return $"{text} | --C";
        }

        int roundedTemperature = (int)Math.Round(temperatureC.Value, 0, MidpointRounding.AwayFromZero);
        if (approximateTemperature)
        {
            return $"{text} | TZ {roundedTemperature}C";
        }

        return $"{text} | {roundedTemperature}C";
    }
}
