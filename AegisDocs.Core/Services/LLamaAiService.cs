using AegisDocs.Core.Interfaces;
using System.Diagnostics;

namespace AegisDocs.Core.Services;

public class LLamaAiService : ILocalAiService, IDisposable
{
    private readonly IAiProcessManager _processManager;
    private readonly IIpcClient _ipcClient;
    private readonly IPathProvider _pathProvider;
    private bool _isInitialized;

    public LLamaAiService(
        IAiProcessManager processManager,
        IIpcClient ipcClient,
        IPathProvider pathProvider)
    {
        _processManager = processManager;
        _ipcClient = ipcClient;
        _pathProvider = pathProvider;
    }

    public async Task InitializeAsync(string _)
    {
        if (_isInitialized) return;

        Debug.WriteLine("[LLamaAiService] Инициализация...");

        string serverExePath = _pathProvider.GetAiServerExePath();
        string modelPath = _pathProvider.GetModelPath();

        _processManager.KillOldProcesses("AegisDocs.AiServer");
        _processManager.StartProcess(serverExePath, modelPath);

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
