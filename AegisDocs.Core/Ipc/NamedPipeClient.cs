using AegisDocs.Core.Interfaces;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.Core.Ipc;

public class NamedPipeClient : IIpcClient
{
    private const string DataPipeName = "AegisAiPipe";
    private const string ControlPipeName = "AegisAiControlPipe";

    public async Task<string> SendAndReceiveAsync(string message, CancellationToken cancellationToken = default)
    {
        using var pipeClient = new NamedPipeClientStream(".", DataPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        await pipeClient.ConnectAsync(30000, cancellationToken);

        using var reader = new StreamReader(pipeClient, new UTF8Encoding(false));
        using var writer = new StreamWriter(pipeClient, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync(message.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);

        string? response = await reader.ReadLineAsync(cancellationToken);

        return response ?? throw new InvalidOperationException("Сервер вернул пустой ответ.");
    }

    public async Task SendCancelSignalAsync()
    {
        try
        {
            using var controlClient = new NamedPipeClientStream(".", ControlPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await controlClient.ConnectAsync(1000);

            using var writer = new StreamWriter(controlClient, new UTF8Encoding(false)) { AutoFlush = true };
            await writer.WriteLineAsync("CANCEL");
            await writer.FlushAsync();
        }
        catch
        {
        }
    }
}
