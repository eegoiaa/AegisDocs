using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AegisDocs.AiServer;

public record AiRequestDto(string SystemPrompt, string DocumentText);
public record AiResponseDto(string Answer, bool IsSuccess, string ErrorMessage);

public class IpcServer
{
    private readonly LlamaEngine _engine;
    private const string PipeName = "AegisAiPipe";

    public IpcServer(LlamaEngine engine)
    {
        _engine = engine;
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Ожидание подключения интерфейса Avalonia...");

        while (true)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine("\n[IPC] Клиент подключился. Обработка запроса...");

                using var reader = new StreamReader(pipeServer, new UTF8Encoding(false));
                using var writer = new StreamWriter(pipeServer, new UTF8Encoding(false)) { AutoFlush = true };

                string? rawJsonLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(rawJsonLine) || rawJsonLine == "EXIT")
                {
                    pipeServer.Disconnect();
                    continue;
                }

                var request = JsonSerializer.Deserialize<AiRequestDto>(rawJsonLine);
                if (request == null) throw new Exception("Пустой JSON-запрос");

                Console.WriteLine("[IPC] Запуск генерации...");
                string aiResult = await _engine.GenerateResponseAsync(
                    request.SystemPrompt,
                    request.DocumentText,
                    CancellationToken.None
                );

                var responseObj = new AiResponseDto(aiResult, true, "");
                string jsonResponse = JsonSerializer.Serialize(responseObj);

                await writer.WriteLineAsync(jsonResponse);
                await writer.FlushAsync();

                Console.WriteLine("[IPC] Ответ успешно передан клиенту.");
            }
            catch (IOException)
            {
                Console.WriteLine("[IPC] Клиент отменил операцию и разорвал соединение.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPC ОШИБКА]: {ex.Message}");
            }
        }
    }
}
