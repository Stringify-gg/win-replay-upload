using System.IO.Pipes;
using System.Text.Json;

namespace StringifyDesktop.Services;

public sealed class SingleInstanceService : IAsyncDisposable
{
    private readonly Mutex mutex;
    private readonly string pipeName;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task listenerTask;

    private SingleInstanceService(Mutex mutex, string pipeName, string[] startupArguments)
    {
        this.mutex = mutex;
        this.pipeName = pipeName;
        StartupArguments = startupArguments;
        listenerTask = Task.Run(ListenLoopAsync);
    }

    public string[] StartupArguments { get; }

    public event EventHandler<string[]>? ArgumentsReceived;

    public static bool TryCreate(string key, string[] args, out SingleInstanceService? service)
    {
        var mutex = new Mutex(true, $@"Local\{key}", out var createdNew);
        var pipeName = $"{key}.Pipe";

        if (!createdNew)
        {
            TryForwardArgumentsAsync(pipeName, args).GetAwaiter().GetResult();
            mutex.Dispose();
            service = null;
            return false;
        }

        service = new SingleInstanceService(mutex, pipeName, args);
        return true;
    }

    public void PublishStartupArguments()
    {
        if (StartupArguments.Length > 0)
        {
            ArgumentsReceived?.Invoke(this, StartupArguments);
        }
    }

    public async ValueTask DisposeAsync()
    {
        shutdown.Cancel();
        try
        {
            await listenerTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            shutdown.Dispose();
            mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }

    private async Task ListenLoopAsync()
    {
        while (!shutdown.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(shutdown.Token);
            using var reader = new StreamReader(server);
            var payload = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            var args = JsonSerializer.Deserialize<string[]>(payload) ?? [];
            ArgumentsReceived?.Invoke(this, args);
        }
    }

    private static async Task TryForwardArgumentsAsync(string pipeName, string[] args)
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            await client.ConnectAsync(1500);
            await using var writer = new StreamWriter(client) { AutoFlush = true };
            await writer.WriteAsync(JsonSerializer.Serialize(args));
        }
        catch
        {
            // Best effort: if the first instance is still starting up, Windows will just open a second process.
        }
    }
}
