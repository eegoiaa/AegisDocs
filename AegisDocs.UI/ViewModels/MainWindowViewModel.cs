using AegisDocs.Core.DTOs;
using AegisDocs.Core.Interfaces;
using AegisDocs.Core.Services;
using AegisDocs.UI.Interfaces;
using AegisDocs.UI.Models;
using AegisDocs.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace AegisDocs.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Сервисы (Зависимости)

    private readonly IDocumentService? _documentService;
    private readonly IFilePickerService? _filePickerService;
    private readonly ILocalAiService? _aiService;
    private readonly IAiResponseParser? _aiParser;

    #endregion


    #region Привязки UI (Данные для интерфейса)

    [ObservableProperty] private string _extractedText = "Здесь появится текст документа...";
    public ObservableCollection<PromptTemplate> Templates { get; }
    [ObservableProperty] private PromptTemplate? _selectedTemplate;
    [ObservableProperty] private bool _isReportReady;


    #endregion


    #region Внутреннее состояние (Кэш текущего аудита)

    private List<CorrectionItem>? _currentErrors;
    private string _lastAnalyzedFileName = string.Empty;
    private string _lastAuditMode = string.Empty;

    #endregion

    public MainWindowViewModel() 
    {
        Templates = new ObservableCollection<PromptTemplate>();
    }

    public MainWindowViewModel(
        IDocumentService documentService,
        IFilePickerService filePickerService,
        ILocalAiService aiService,
        IAiResponseParser? aiParser)
    {
        _documentService = documentService;
        _filePickerService = filePickerService;
        _aiService = aiService;
        _aiParser = aiParser;

        Templates = new ObservableCollection<PromptTemplate>(PromptProvider.GetDefaultTemplates());

        if (Templates.Count > 0)
            SelectedTemplate = Templates[0];
        
    }

    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        if (_documentService == null || _filePickerService == null || _aiService == null || _aiParser == null) return;

        var filePath = await _filePickerService.PickFileAsync();

        if (string.IsNullOrEmpty(filePath)) return;

        _lastAnalyzedFileName = System.IO.Path.GetFileName(filePath);
        _lastAuditMode = SelectedTemplate?.Name ?? "Общая проверка";
        IsReportReady = false;
        _currentErrors = null;

        try
        {
            ExtractedText = "1. Читаем файл...";
            var fullText = await Task.Run(() => _documentService.ExtractText(filePath));

            ExtractedText = $"2. Запускаем ИИ-сервер...\n\nДокумент успешно прочитан. Объем: {fullText.Length} символов.";
            await _aiService.InitializeAsync("");

            string currentMode = SelectedTemplate?.Name ?? "По умолчанию";
            ExtractedText = $"3. Режим: {currentMode}\nНейросеть анализирует договор. Пожалуйста, подождите...\n";
            string systemPrompt = SelectedTemplate?.PromptText ?? "Ты юрист. Найди ошибки в тексте.";

            var aiResponse = await _aiService.AnalyzeTextAsync(systemPrompt, fullText, CancellationToken.None);

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
        catch (Exception ex)
        {
            ExtractedText = $"Критическая ошибка:\n{ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (_documentService == null || _currentErrors == null || _currentErrors.Count == 0) return;

        try
        {
            string safeFileName = System.IO.Path.GetFileNameWithoutExtension(_lastAnalyzedFileName);

            string safeMode = _lastAuditMode.Replace(" ", "");

            string timeStamp = DateTime.Now.ToString("HH-mm-ss");

            string outputFileName = $"Отчет_{safeMode}_{safeFileName}_{timeStamp}.docx";

            string reportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), outputFileName);

            _documentService.GenerateAuditReport(reportPath, _currentErrors, _lastAnalyzedFileName, _lastAuditMode);

            ExtractedText += $"\n\n[УСПЕХ] Отчет сохранен на Рабочий стол:\n{outputFileName}";
        }
        catch (Exception ex)
        {
            ExtractedText += $"\n\n[ОШИБКА СОХРАНЕНИЯ]\n{ex.Message}";
        }
    }
}
