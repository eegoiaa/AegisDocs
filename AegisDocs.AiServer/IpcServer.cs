using System.IO.Pipes;
using System.Text;

namespace AegisDocs.AiServer;

public class IpcServer
{
    private readonly LlamaEngine _engine;

    public IpcServer(LlamaEngine engine)
    {
        _engine = engine;
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Ожидание подключения интерфейса Avalonia...");

        using var pipeServer = new NamedPipeServerStream("AegisAiPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipeServer.WaitForConnectionAsync();

        Console.WriteLine("=== Интерфейс подключен! ===");

        using var reader = new StreamReader(pipeServer, new UTF8Encoding(false));
        using var writer = new StreamWriter(pipeServer, new UTF8Encoding(false)) { AutoFlush = true };

        while (true)
        {
            string? textToAnalyze = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(textToAnalyze)) continue;
            if (textToAnalyze == "EXIT")
            {
                Console.WriteLine("Получена команда EXIT. Завершение работы сервера.");
                break;
            }

            Console.WriteLine("Получен текст. Анализирую...");
            var sb = new StringBuilder();

            await foreach (var token in _engine.GenerateResponseAsync(textToAnalyze))
            {
                sb.Append(token);
                Console.Write(token);
            }
            Console.WriteLine();

            string cleanResponse = sb.ToString().Replace("\n", " ").Replace("\r", "").Trim();

            await writer.WriteLineAsync(cleanResponse);
            await writer.FlushAsync();
            Console.WriteLine("=== Ответ отправлен в UI ===");
        }
    }
}
