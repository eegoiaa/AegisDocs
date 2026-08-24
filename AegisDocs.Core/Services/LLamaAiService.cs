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

    public Task InitializeAsync(string _)
    {
        if (_isInitialized) return Task.CompletedTask;

        Debug.WriteLine("[LLamaAiService] Запуск процесса AiServer...");

        string serverExePath = _pathProvider.GetAiServerExePath();
        string modelPath = _pathProvider.GetModelPath();

        _processManager.KillOldProcesses("AegisDocs.AiServer");
        _processManager.StartProcess(serverExePath, modelPath);

        _isInitialized = true;
        Debug.WriteLine("[LLamaAiService] Процесс AiServer запущен!");

        return Task.CompletedTask;
    }

    public async Task<string> AnalyzeTextAsync(string systemPrompt, string userText, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("ИИ-сервер не запущен!");

        var requestObj = new AiRequestDto(systemPrompt, userText);
        string jsonRequest = JsonSerializer.Serialize(requestObj);

        string jsonResponse = await _ipcClient.SendAndReceiveAsync(jsonRequest, cancellationToken);

        try
        {
            var responseObj = JsonSerializer.Deserialize<AiResponseDto>(jsonResponse);

            if (responseObj != null && responseObj.IsSuccess)
                return responseObj.Answer;

            throw new InvalidOperationException(responseObj?.ErrorMessage ?? "Неизвестная ошибка сервера ИИ");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Ошибка десериализации ответа: {ex.Message}\nОтвет: {jsonResponse}", ex);
        }
    }

    public async Task CancelCurrentTaskAsync()
    {
        await _ipcClient.SendCancelSignalAsync();
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            _processManager.StopProcess();
            _isInitialized = false;
        }
    }
}
