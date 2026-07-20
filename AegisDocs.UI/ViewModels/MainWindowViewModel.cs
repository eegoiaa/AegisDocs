using AegisDocs.Core.Interfaces;
using AegisDocs.Core.Services;
using AegisDocs.UI.Interfaces;
using AegisDocs.UI.Models;
using AegisDocs.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
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

    public ObservableCollection<PromptTemplate> Templates { get; }

    [ObservableProperty]
    private PromptTemplate? _selectedTemplate;

    public MainWindowViewModel() 
    {
        Templates = new ObservableCollection<PromptTemplate>();
    }

    public MainWindowViewModel(
        IDocumentService documentService,
        IFilePickerService filePickerService,
        ILocalAiService aiService)
    {
        _documentService = documentService;
        _filePickerService = filePickerService;
        _aiService = aiService;

        Templates = new ObservableCollection<PromptTemplate>(PromptProvider.GetDefaultTemplates());

        if (Templates.Count > 0)
            SelectedTemplate = Templates[0];
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

            await _aiService.InitializeAsync("");

            string currentMode = SelectedTemplate?.Name ?? "По умолчанию";
            ExtractedText = $"3. Режим: {currentMode}\nНейросеть анализирует договор. Пожалуйста, подождите...\n";

            string systemPrompt = SelectedTemplate?.PromptText
                ?? "Ты юрист. Найди ошибки в тексте.";

            var aiResponse = await _aiService.AnalyzeTextAsync(systemPrompt, fullText, CancellationToken.None);

            int startIndex = aiResponse.IndexOf('[');
            int endIndex = aiResponse.LastIndexOf(']');

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                string cleanJson = aiResponse.Substring(startIndex, endIndex - startIndex + 1);

                ExtractedText = $"=== ИДЕАЛЬНЫЙ JSON ===\n\n{cleanJson}";
            }
            else
            {
                ExtractedText = $"Ошибка: ИИ не вернул корректный JSON.\n\nСырой ответ:\n{aiResponse}";
            }

            ExtractedText = $"=== ОТВЕТ ИИ ===\n\n{aiResponse}";
        }
        catch (Exception ex)
        {
            ExtractedText = $"Критическая ошибка:\n{ex.Message}\n\nСтек:\n{ex.StackTrace}";
        }
    }
}
