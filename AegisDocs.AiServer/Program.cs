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

        // Хардкод пути оставляем, как договаривались
        string modelPath = @"D:\AI_models\qwen2.5-3b-instruct-q4_k_m.gguf";

        using var engine = new LlamaEngine();

        try
        {
            Console.WriteLine("Грузим веса нейросети в память...");
            engine.Initialize(modelPath);
            Console.WriteLine("=== Модель успешно загружена! ===");

            var ipcServer = new IpcServer(engine);
            await ipcServer.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[КРИТИЧЕСКАЯ ОШИБКА]: {ex.Message}");
            Console.ReadLine(); // Держим консоль открытой, чтобы успеть прочитать ошибку
        }
    }
}
