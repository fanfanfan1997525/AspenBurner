using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace AspenBurner.App.Application;

/// <summary>
/// Sends control commands to the primary AspenBurner instance.
/// </summary>
public sealed class AppCommandClient
{
    /// <summary>
    /// Attempts to deliver a command to the named-pipe server.
    /// </summary>
    /// <param name="pipeName">Target pipe name.</param>
    /// <param name="command">Command to send.</param>
    /// <param name="timeoutMilliseconds">Connection timeout in milliseconds.</param>
    /// <returns><see langword="true"/> when the primary instance acknowledged the command.</returns>
    public bool TrySend(string pipeName, AppCommand command, int timeoutMilliseconds = 2000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);

        try
        {
            using NamedPipeClientStream client = new(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.None,
                TokenImpersonationLevel.Impersonation);

            client.Connect(timeoutMilliseconds);

            using StreamWriter writer = new(client, new UTF8Encoding(false), 1024, leaveOpen: true);
            using StreamReader reader = new(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);

            writer.AutoFlush = true;
            writer.WriteLine(AppCommandCodec.Serialize(command));

            string? acknowledgement = reader.ReadLine();
            return string.Equals(acknowledgement, "OK", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
