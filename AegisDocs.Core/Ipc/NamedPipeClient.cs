using AegisDocs.Core.Interfaces;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.Core.Ipc;

public class NamedPipeClient : IIpcClient
{
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task ConnectAsync(string pipeName, int timeoutMs)
    {
        await Task.Run(async () =>
        {
            _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipeClient.ConnectAsync(timeoutMs);

            _reader = new StreamReader(_pipeClient, new UTF8Encoding(false));
            _writer = new StreamWriter(_pipeClient, new UTF8Encoding(false)) { AutoFlush = true };
        });
    }

    public async Task<string> SendAndReceiveAsync(string message, CancellationToken cancellationToken)
    {
        if (_writer == null || _reader == null) throw new InvalidOperationException("Пайп не подключен!");

        return await Task.Run(async () =>
        {
            try
            {
                await _writer.WriteLineAsync(message);
                await _writer.FlushAsync();

                string? response = await _reader.ReadLineAsync();
                return response ?? "Ошибка: Сервер вернул пустой ответ.";
            }
            catch (Exception ex)
            {
                return $"Ошибка канала связи: {ex.Message}";
            }
        }, cancellationToken);
    }

    public void SendDisconnectSignal()
    {
        try { _writer?.WriteLine("EXIT"); } catch { }
    }

    public void Dispose()
    {
        _pipeClient?.Dispose();
    }
}
