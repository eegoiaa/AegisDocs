using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AegisDocs.AiServer;

public record AiRequestDto(string SystemPrompt, string DocumentText);
public record AiResponseDto(string Answer, bool IsSuccess, string ErrorMessage);

public class IpcServer
{
    private readonly LlamaEngine _engine;
    private const string DataPipeName = "AegisAiPipe";
    private const string ControlPipeName = "AegisAiControlPipe";

    private CancellationTokenSource? _currentGenerationCts;

    public IpcServer(LlamaEngine engine)
    {
        _engine = engine;
    }

    public async Task StartAsync()
    {
        Console.WriteLine("=== Сервер ИИ запущен и ожидает запросы ===");

        _ = StartControlListenerAsync();

        while (true)
        {
            using var pipeServer = new NamedPipeServerStream(
                DataPipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine("\n[IPC] Клиент подключился к каналу данных...");

                using var reader = new StreamReader(pipeServer, new UTF8Encoding(false));
                using var writer = new StreamWriter(pipeServer, new UTF8Encoding(false)) { AutoFlush = true };

                string? rawJsonLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(rawJsonLine)) continue;

                var request = JsonSerializer.Deserialize<AiRequestDto>(rawJsonLine);
                if (request == null) throw new Exception("Пустой JSON-запрос");

                Console.WriteLine("[IPC] Запуск генерации...");

                _currentGenerationCts = new CancellationTokenSource();

                string aiResult = string.Empty;
                bool isSuccess = false;
                string errorMessage = string.Empty;

                try
                {
                    aiResult = await _engine.GenerateResponseAsync(
                        request.SystemPrompt,
                        request.DocumentText,
                        _currentGenerationCts.Token
                    );
                    isSuccess = true;
                    Console.WriteLine("[IPC] Генерация успешно завершена.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[IPC] Генерация прервана по команде CANCEL.");
                    errorMessage = "Операция отменена.";
                }

                if (pipeServer.IsConnected)
                {
                    var responseObj = new AiResponseDto(aiResult, isSuccess, errorMessage);
                    string jsonResponse = JsonSerializer.Serialize(responseObj);
                    await writer.WriteLineAsync(jsonResponse);
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPC ОШИБКА]: {ex.Message}");
            }
            finally
            {
                _currentGenerationCts?.Dispose();
                _currentGenerationCts = null;

                try
                {
                    if (pipeServer.IsConnected) pipeServer.Disconnect();
                }
                catch { }
            }
        }
    }

    private async Task StartControlListenerAsync()
    {
        while (true)
        {
            try
            {
                using var controlPipe = new NamedPipeServerStream(
                    ControlPipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await controlPipe.WaitForConnectionAsync();
                using var reader = new StreamReader(controlPipe, new UTF8Encoding(false));

                string? cmd = await reader.ReadLineAsync();
                if (cmd == "CANCEL")
                {
                    Console.WriteLine("[IPC-CONTROL] Получена команда CANCEL. Прерывание...");
                    _currentGenerationCts?.Cancel();
                }
            }
            catch
            {
            }
        }
    }
}
