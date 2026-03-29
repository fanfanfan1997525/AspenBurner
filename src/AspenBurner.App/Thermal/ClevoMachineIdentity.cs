using System.Management;

namespace AspenBurner.App.Thermal;

/// <summary>
/// Detects whether the current machine matches the validated Clevo platform.
/// </summary>
public static class ClevoMachineIdentity
{
    private static readonly string[] SupportedMarkers = ["NP5x_6x_7x_SNx"];

    /// <summary>
    /// Determines whether a single model or board string matches the validated machine family.
    /// </summary>
    public static bool IsSupportedModel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return SupportedMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the current machine matches the validated Clevo family.
    /// </summary>
    public static bool IsCurrentMachineSupported()
    {
        return GetCandidateMachineMarkers().Any(IsSupportedModel);
    }

    private static IEnumerable<string> GetCandidateMachineMarkers()
    {
        foreach (string value in QueryValues("SELECT Product FROM Win32_BaseBoard", "Product"))
        {
            yield return value;
        }

        foreach (string value in QueryValues("SELECT Model FROM Win32_ComputerSystem", "Model"))
        {
            yield return value;
        }

        foreach (string value in QueryValues("SELECT Name FROM Win32_ComputerSystemProduct", "Name"))
        {
            yield return value;
        }
    }

    private static IEnumerable<string> QueryValues(string query, string propertyName)
    {
        List<string> values = [];

        try
        {
            using ManagementObjectSearcher searcher = new(query);
            foreach (ManagementObject result in searcher.Get())
            {
                if (result[propertyName] is string value && !string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }
        catch
        {
        }

        return values;
    }
}
