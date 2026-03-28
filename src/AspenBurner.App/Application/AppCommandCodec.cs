using System.Text;

namespace AspenBurner.App.Application;

/// <summary>
/// Serializes and parses named-pipe command payloads.
/// </summary>
public static class AppCommandCodec
{
    /// <summary>
    /// Serializes a command into a stable pipe payload.
    /// </summary>
    public static string Serialize(AppCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ConfigPath))
        {
            return $"{command.Kind}|{command.Argument}";
        }

        string encodedConfigPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(command.ConfigPath));
        return $"{command.Kind}|{command.Argument}|{encodedConfigPath}";
    }

    /// <summary>
    /// Parses a pipe payload into a command instance.
    /// </summary>
    public static AppCommand Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Command payload must not be empty.", nameof(payload));
        }

        string[] parts = payload.Split('|');
        if (parts.Length is < 1 or > 3)
        {
            throw new ArgumentException("Command payload is malformed.", nameof(payload));
        }

        if (!Enum.TryParse(parts[0], ignoreCase: true, out AppCommandKind kind))
        {
            throw new ArgumentException($"Unknown command: {parts[0]}", nameof(payload));
        }

        int argument = 0;
        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) && !int.TryParse(parts[1], out argument))
        {
            throw new ArgumentException("Command argument is malformed.", nameof(payload));
        }

        string? configPath = null;
        if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
        {
            try
            {
                configPath = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("Command config path is malformed.", nameof(payload), exception);
            }
        }

        return new AppCommand(kind, argument, configPath);
    }
}
