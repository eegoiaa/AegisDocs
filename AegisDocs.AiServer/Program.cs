using LLama;
using LLama.Common;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.AiServer;

public class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Запуск локального ИИ-сервера ===");

        string modelPath = @"D:\AI_models\qwen2.5-3b-instruct-q4_k_m.gguf";

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 0
        };

        Console.WriteLine("Грузим веса нейросети в память...");
        using var weights = LLamaWeights.LoadFromFile(parameters);
        using var context = weights.CreateContext(parameters);
        var executor = new InteractiveExecutor(context);

        Console.WriteLine("=== Модель успешно загружена! ===");
        Console.WriteLine("Ожидание подключения интерфейса Avalonia...");

        // Открываем канал связи (Named Pipe)
        using var pipeServer = new NamedPipeServerStream("AegisAiPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipeServer.WaitForConnectionAsync();
        Console.WriteLine("=== Интерфейс подключен! ===");

        using var reader = new StreamReader(pipeServer, new UTF8Encoding(false));
        using var writer = new StreamWriter(pipeServer, new UTF8Encoding(false)) { AutoFlush = true };

        while (true)
        {
            string? textToAnalyze = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(textToAnalyze)) continue;
            if (textToAnalyze == "EXIT") break;

            Console.WriteLine("Получен текст. Анализирую...");

            // Наш базовый промпт. Позже мы его усложним для поиска скрытых рисков.
            string prompt = $"Проанализируй юридический текст и найди риски. Отвечай по существу.\nТекст: {textToAnalyze}\nОтвет:";
            var inferenceParams = new InferenceParams { MaxTokens = 1500 };

            var sb = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams))
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
