using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AegisDocs.UI.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        if (_documentService == null || _filePickerService == null || _aiService == null || _aiParser == null) return;

        var filePath = await _filePickerService.PickFileAsync();
        if (string.IsNullOrEmpty(filePath)) return;

        _lastAnalyzedFilePath = filePath;
        _lastAnalyzedFileName = Path.GetFileName(filePath);
        _lastAuditMode = SelectedTemplate?.Name ?? "Общая проверка";
        IsReportReady = false;
        _currentErrors = null;

        IsAnalyzing = true;
        _analysisCts = new CancellationTokenSource();

        try
        {
            ExtractedText = "1. Читаем файл...";
            var fullText = await Task.Run(() => _documentService.ExtractText(filePath), _analysisCts.Token);

            ExtractedText = $"2. Запускаем ИИ-сервер...\n\nДокумент успешно прочитан. Объем: {fullText.Length} символов.";
            await _aiService.InitializeAsync("");

            string currentMode = SelectedTemplate?.Name ?? "По умолчанию";
            ExtractedText = $"3. Режим: {currentMode}\nНейросеть анализирует договор. Пожалуйста, подождите...\n";
            string systemPrompt = SelectedTemplate?.PromptText ?? "Ты юрист. Найди ошибки в тексте.";

            var aiResponse = await _aiService.AnalyzeTextAsync(systemPrompt, fullText, _analysisCts.Token);

            _currentErrors = _aiParser.ParseAndFilterErrors(aiResponse);

            if (_currentErrors != null && _currentErrors.Count > 0)
            {
                ExtractedText = $"=== АНАЛИЗ ЗАВЕРШЕН ===\n\nНайдено ошибок: {_currentErrors.Count}.\nНажмите кнопку «Выгрузить отчет», чтобы получить Word-файл.";
                IsReportReady = true;
            }
            else
            {
                ExtractedText = $"Ошибок не найдено. Договор чист!\n\n(Сырой ответ для отладки:\n{aiResponse})";
            }
        }
        catch (OperationCanceledException)
        {
            ExtractedText = "Анализ документа отменен пользователем.";
            IsReportReady = false;
        }
        catch (Exception ex)
        {
            ExtractedText = $"Критическая ошибка:\n{ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCts?.Dispose();
            _analysisCts = null;
        }
    }

    [RelayCommand]
    private async Task CancelAnalysisAsync()
    {
        if (IsAnalyzing)
        {
            ExtractedText = "Отмена анализа...";

            _analysisCts?.Cancel();

            if (_aiService != null)
            {
                try
                {
                    await _aiService.CancelCurrentTaskAsync();
                }
                catch
                {
                }
            }
        }
    }
}
