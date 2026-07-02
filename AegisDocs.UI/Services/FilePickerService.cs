using AegisDocs.UI.Interfaces;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace AegisDocs.UI.Services;

public class FilePickerService : IFilePickerService
{
    public async Task<string?> PickFileAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите договор",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Word Documents") { Patterns = new[] { "*.docx" } }
                }
            });

            if (files.Count > 0)
            {
                return files[0].Path.LocalPath;
            }
        }

        return null;
    }
}
