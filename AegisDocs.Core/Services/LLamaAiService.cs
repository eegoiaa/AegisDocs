using AegisDocs.Core.Interfaces;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace AegisDocs.Core.Services;

public class LLamaAiService : ILocalAiService, IDisposable
{
    private readonly IAiProcessManager _processManager;
    private readonly IIpcClient _ipcClient;
    private bool _isInitialized;

    public LLamaAiService(
        IAiProcessManager processManager,
        IIpcClient ipcClient)
    {
        _processManager = processManager;
        _ipcClient = ipcClient;
    }

    public async Task InitializeAsync(string modelPath)
    {
        if (_isInitialized) return;

        string serverExeName = "AegisDocs.AiServer";
        string serverExePath = @"D:\AegisDocsProject\AegisDocs\AegisDocs.AiServer\bin\Debug\net8.0\AegisDocs.AiServer.exe";

        if (!File.Exists(serverExePath))
            throw new FileNotFoundException($"Сервер не найден: {serverExePath}");

        Debug.WriteLine("[LLamaAiService] Инициализация...");

        // 1. Делегируем работу с процессами
        _processManager.KillOldProcesses(serverExeName);
        _processManager.StartProcess(serverExePath);

        // 2. Делегируем работу с сетью
        await _ipcClient.ConnectAsync("AegisAiPipe", 60000);

        _isInitialized = true;
        Debug.WriteLine("[LLamaAiService] Инициализация успешна!");
    }

    public async Task<string> AnalyzeTextAsync(string systemPrompt, string userText, CancellationToken cancellationToken)
    {
        if (!_isInitialized) throw new InvalidOperationException("ИИ-сервер не запущен!");

        string formattedMessage = PrepareMessageToSend(systemPrompt, userText);

        return await _ipcClient.SendAndReceiveAsync(formattedMessage, cancellationToken);
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            _ipcClient.SendDisconnectSignal();
            _ipcClient.Dispose();
            _processManager.StopProcess();
        }
    }

    private string PrepareMessageToSend(string systemPrompt, string userText)
    {
        string combinedText = $"{systemPrompt} Текст договора: {userText}";
        return combinedText.Replace("\n", " ").Replace("\r", " ");
    }
}
