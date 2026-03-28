using System.IO.Pipes;
using System.Text;

namespace AspenBurner.App.Application;

/// <summary>
/// Hosts the single-instance named-pipe command endpoint.
/// </summary>
public sealed class AppCommandServer : IDisposable
{
    private readonly string pipeName;
    private readonly Action<AppCommand> onCommand;
    private readonly Action<Exception>? onError;
    private readonly SynchronizationContext? synchronizationContext;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task? listenTask;

    /// <summary>
    /// Initializes a new command server.
    /// </summary>
    public AppCommandServer(
        string pipeName,
        Action<AppCommand> onCommand,
        Action<Exception>? onError = null,
        SynchronizationContext? synchronizationContext = null)
    {
        this.pipeName = pipeName;
        this.onCommand = onCommand ?? throw new ArgumentNullException(nameof(onCommand));
        this.onError = onError;
        this.synchronizationContext = synchronizationContext;
    }

    /// <summary>
    /// Starts the asynchronous listen loop.
    /// </summary>
    public void Start()
    {
        if (this.listenTask is not null)
        {
            return;
        }

        this.listenTask = Task.Run(this.ListenLoopAsync);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();

        if (this.listenTask is not null)
        {
            try
            {
                this.listenTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Shutdown should stay best-effort.
            }
        }

        this.cancellationTokenSource.Dispose();
    }

    private async Task ListenLoopAsync()
    {
        while (!this.cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream server = new(
                    this.pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(this.cancellationTokenSource.Token).ConfigureAwait(false);

                using StreamReader reader = new(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);
                using StreamWriter writer = new(server, new UTF8Encoding(false), 1024, leaveOpen: true)
                {
                    AutoFlush = true,
                };

                string? payload = await reader.ReadLineAsync(this.cancellationTokenSource.Token).ConfigureAwait(false);
                AppCommand command = AppCommandCodec.Parse(payload ?? string.Empty);
                this.Dispatch(command);
                await writer.WriteLineAsync("OK").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                this.onError?.Invoke(exception);
            }
        }
    }

    private void Dispatch(AppCommand command)
    {
        if (this.synchronizationContext is null)
        {
            this.onCommand(command);
            return;
        }

        this.synchronizationContext.Post(static state =>
        {
            DispatchState dispatchState = (DispatchState)state!;
            dispatchState.Handler(dispatchState.Command);
        }, new DispatchState(this.onCommand, command));
    }

    private sealed record DispatchState(Action<AppCommand> Handler, AppCommand Command);
}
