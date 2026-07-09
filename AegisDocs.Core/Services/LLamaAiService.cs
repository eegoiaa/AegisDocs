using AegisDocs.Core.Interfaces;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.Core.Services;

public class LLamaAiService : ILocalAiService, IDisposable
{
    private Process? _aiProcess;
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _isInitialized;

    public async Task InitializeAsync(string modelPath)
    {
        if (_isInitialized) return;

        Debug.WriteLine("[CLIENT-LOG] Начинаем инициализацию...");

        // 1. УБИВАЕМ ЗОМБИ-ПРОЦЕССЫ
        Debug.WriteLine("[CLIENT-LOG] Очистка старых процессов сервера...");
        foreach (var proc in Process.GetProcessesByName("AegisDocs.AiServer"))
        {
            try { proc.Kill(); Debug.WriteLine($"[CLIENT-LOG] Убит процесс {proc.Id}"); } catch { }
        }

        string serverExePath = @"D:\AegisDocsProject\AegisDocs\AegisDocs.AiServer\bin\Debug\net8.0\AegisDocs.AiServer.exe";
        if (!File.Exists(serverExePath)) throw new FileNotFoundException($"Сервер не найден: {serverExePath}");

        // 2. Запускаем чистый сервер
        Debug.WriteLine("[CLIENT-LOG] Запуск нового сервера...");
        _aiProcess = new Process
        {
            StartInfo = new ProcessStartInfo { FileName = serverExePath, UseShellExecute = true }
        };
        _aiProcess.Start();

        // 3. Подключаемся к каналу связи
        Debug.WriteLine("[CLIENT-LOG] Попытка подключения к трубе (ожидание до 60 сек)...");
        await Task.Run(async () =>
        {
            _pipeClient = new NamedPipeClientStream(".", "AegisAiPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipeClient.ConnectAsync(60000);

            _reader = new StreamReader(_pipeClient, new UTF8Encoding(false));
            _writer = new StreamWriter(_pipeClient, new UTF8Encoding(false)) { AutoFlush = true };
        });

        _isInitialized = true;
        Debug.WriteLine("[CLIENT-LOG] ИНИЦИАЛИЗАЦИЯ УСПЕШНА! Связь установлена.");
    }

    public async Task<string> AnalyzeTextAsync(string systemPrompt, string userText, CancellationToken cancellationToken)
    {
        Debug.WriteLine("[CLIENT-LOG] Начат метод AnalyzeTextAsync...");
        if (!_isInitialized || _writer == null || _reader == null) throw new InvalidOperationException("ИИ-сервер не запущен!");

        string combinedText = $"{systemPrompt} Текст договора: {userText}";
        string cleanText = combinedText.Replace("\n", " ").Replace("\r", " ");

        return await Task.Run(async () =>
        {
            try
            {
                Debug.WriteLine("[CLIENT-LOG] Отправляем текст на сервер...");
                await _writer.WriteLineAsync(cleanText);
                await _writer.FlushAsync();
                Debug.WriteLine("[CLIENT-LOG] Текст отправлен. Ждем ответ...");

                string? response = await _reader.ReadLineAsync();
                Debug.WriteLine("[CLIENT-LOG] Ответ от сервера получен!");
                return response ?? "Ошибка: Сервер вернул пустой ответ.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENT-LOG] ОШИБКА ПРИ ОБМЕНЕ: {ex.Message}");
                return $"Критическая ошибка канала связи: {ex.Message}";
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        try { _writer?.WriteLine("EXIT"); } catch { }
        _pipeClient?.Dispose();
        if (_aiProcess != null && !_aiProcess.HasExited) { _aiProcess.Kill(); }
    }
}
