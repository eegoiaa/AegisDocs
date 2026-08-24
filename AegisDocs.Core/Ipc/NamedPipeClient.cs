using AegisDocs.Core.Interfaces;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.Core.Ipc;

public class NamedPipeClient : IIpcClient
{
    private const string PipeName = "AegisAiPipe";

    public async Task<string> SendAndReceiveAsync(string message, CancellationToken cancellationToken)
    {
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        await pipeClient.ConnectAsync(10000, cancellationToken);

        using var reader = new StreamReader(pipeClient, new UTF8Encoding(false));
        using var writer = new StreamWriter(pipeClient, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync(message.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);

        string? response = await reader.ReadLineAsync(cancellationToken);

        return response ?? throw new InvalidOperationException("Сервер вернул пустой ответ.");
    }
}
