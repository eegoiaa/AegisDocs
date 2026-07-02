using AegisDocs.Core.Services;
using AegisDocs.UI.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace AegisDocs.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDocumentService? _documentService;
    private readonly IFilePickerService? _filePickerService;

    [ObservableProperty]
    private string _extractedText = "Здесь появится текст документа...";

    public MainWindowViewModel() { }

    public MainWindowViewModel(IDocumentService documentService, IFilePickerService filePickerService)
    {
        _documentService = documentService;
        _filePickerService = filePickerService;
    }

    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        if (_documentService == null || _filePickerService == null) return;

        var filePath = await _filePickerService.PickFileAsync();

        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            ExtractedText = "Читаем файл...";
            ExtractedText = await Task.Run(() => _documentService.ExtractText(filePath));
        }
        catch (Exception ex)
        {
            ExtractedText = $"Критическая ошибка:\n{ex.Message}\n\nСтек:\n{ex.StackTrace}";
        }
    }
}
