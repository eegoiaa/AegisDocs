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
    [ObservableProperty] private bool _isAnalyzing;

    #endregion

    #region Внутреннее состояние (Кэш и токены)

    private CancellationTokenSource? _analysisCts;
    private List<CorrectionItem>? _currentErrors;
    private string _lastAnalyzedFileName = string.Empty;
    private string _lastAnalyzedFilePath = string.Empty;
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
}
