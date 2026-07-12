using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AegisDocs.AiServer;

public record AiRequestDto(string SystemPrompt, string DocumentText);
public record AiResponseDto(string Answer, bool IsSuccess, string ErrorMessage);

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
            string? rawJsonLine = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(rawJsonLine)) continue;
            if (rawJsonLine == "EXIT") break;

            AiResponseDto responseObj;
            try
            {
                var request = JsonSerializer.Deserialize<AiRequestDto>(rawJsonLine);
                if (request == null) throw new Exception("Пришел пустой JSON");

                Console.WriteLine("Получен текст. Анализирую...");

                string combinedPrompt = $"{request.SystemPrompt}\n\nТекст документа:\n{request.DocumentText}\n\nОтвет:";

                var sb = new StringBuilder();
                await foreach (var token in _engine.GenerateResponseAsync(combinedPrompt))
                {
                    sb.Append(token);
                    Console.Write(token);
                }
                Console.WriteLine();

                responseObj = new AiResponseDto(sb.ToString().Trim(), true, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА ОБРАБОТКИ]: {ex.Message}");
                responseObj = new AiResponseDto("", false, ex.Message);
            }

            // 4. Упаковываем ответ в JSON и отправляем ОДНОЙ строкой
            string jsonResponse = JsonSerializer.Serialize(responseObj);
            await writer.WriteLineAsync(jsonResponse);
            await writer.FlushAsync();
            Console.WriteLine("=== Ответ отправлен в UI ===");
        }
    }
}
