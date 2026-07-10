using AegisDocs.Core.Interfaces;
using AegisDocs.Core.Services;
using AegisDocs.UI.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AegisDocs.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDocumentService? _documentService;
    private readonly IFilePickerService? _filePickerService;
    private readonly ILocalAiService? _aiService;

    [ObservableProperty]
    private string _extractedText = "Здесь появится текст документа...";

    public MainWindowViewModel() { }

    public MainWindowViewModel(
        IDocumentService documentService,
        IFilePickerService filePickerService,
        ILocalAiService aiService)
    {
        _documentService = documentService;
        _filePickerService = filePickerService;
        _aiService = aiService;
    }

    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        if (_documentService == null || _filePickerService == null || _aiService == null) return;

        var filePath = await _filePickerService.PickFileAsync();
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            ExtractedText = "1. Читаем файл...";

            var fullText = await Task.Run(() => _documentService.ExtractText(filePath));

            ExtractedText = $"2. Запускаем ИИ-сервер (при первом запуске загрузка весов займет время)...\n\nДокумент успешно прочитан. Объем: {fullText.Length} символов.";

            string modelPath = @"D:\AI_models\qwen2.5-3b-instruct-q4_k_m.gguf";
            await _aiService.InitializeAsync(modelPath);

            ExtractedText = "3. Нейросеть читает и анализирует ВЕСЬ договор. Пожалуйста, подождите...\n";

            string systemPrompt = "Ты опытный юрист. Кратко проанализируй следующий текст, укажи на возможные риски и найди ошибки в нем. Отвечай на русском языке.";

            var aiResponse = await _aiService.AnalyzeTextAsync(systemPrompt, fullText, CancellationToken.None);

            ExtractedText = $"=== ОТВЕТ ИИ ===\n\n{aiResponse}";
        }
        catch (Exception ex)
        {
            ExtractedText = $"Критическая ошибка:\n{ex.Message}\n\nСтек:\n{ex.StackTrace}";
        }
    }
}
