using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;

namespace AegisDocs.UI.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void ExportReport()
    {
        if (_documentService == null || _currentErrors == null || _currentErrors.Count == 0) return;

        try
        {
            string safeFileName = Path.GetFileNameWithoutExtension(_lastAnalyzedFileName);
            string safeMode = _lastAuditMode.Replace(" ", "");
            string timeStamp = DateTime.Now.ToString("HH-mm-ss");
            string outputFileName = $"Отчет_{safeMode}_{safeFileName}_{timeStamp}.docx";

            string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), outputFileName);

            _documentService.GenerateAuditReport(reportPath, _currentErrors, _lastAnalyzedFileName, _lastAuditMode);

            ExtractedText += $"\n\n[УСПЕХ] Отчет сохранен на Рабочий стол:\n{outputFileName}";
        }
        catch (Exception ex)
        {
            ExtractedText += $"\n\n[ОШИБКА СОХРАНЕНИЯ]\n{ex.Message}";
        }
    }

    [RelayCommand]
    private void GenerateCorrectedDocument()
    {
        if (_documentService == null || _currentErrors == null || _currentErrors.Count == 0 || string.IsNullOrEmpty(_lastAnalyzedFilePath))
            return;

        try
        {
            string safeFileName = Path.GetFileNameWithoutExtension(_lastAnalyzedFileName);
            string timeStamp = DateTime.Now.ToString("HH-mm-ss");
            string outputFileName = $"{safeFileName}_Исправлен_{timeStamp}.docx";

            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), outputFileName);

            _documentService.ApplyCorrections(_lastAnalyzedFilePath, outputPath, _currentErrors);

            ExtractedText += $"\n\nИсправленный договор сохранен на Рабочий стол:\n{outputFileName}";
        }
        catch (Exception ex)
        {
            ExtractedText += $"\n\n[ОШИБКА АВТОЗАМЕНЫ]\n{ex.Message}";
        }
    }
}
