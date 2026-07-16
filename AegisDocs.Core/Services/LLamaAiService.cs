using AegisDocs.Core.DTOs;
using AegisDocs.Core.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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

        var requestObj = new AiRequestDto(systemPrompt, userText);
        string jsonRequest = JsonSerializer.Serialize(requestObj);
        string jsonResponse = await _ipcClient.SendAndReceiveAsync(jsonRequest, cancellationToken);

        try
        {
            var responseObj = JsonSerializer.Deserialize<AiResponseDto>(jsonResponse);

            if (responseObj != null && responseObj.IsSuccess)
                return responseObj.Answer;

            return $"Ошибка ИИ: {responseObj?.ErrorMessage ?? "Неизвестная ошибка"}";
        }
        catch (JsonException ex)
        {
            return $"Ошибка расшифровки ответа сервера: {ex.Message}\nСырой ответ: {jsonResponse}";
        }
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
}
