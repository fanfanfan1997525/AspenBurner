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
        return $"{command.Kind}|{command.Argument}";
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
        if (parts.Length is < 1 or > 2)
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

        return new AppCommand(kind, argument);
    }
}
