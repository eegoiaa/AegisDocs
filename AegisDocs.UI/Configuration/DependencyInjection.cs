using AegisDocs.Core.Interfaces;
using AegisDocs.Core.Ipc;
using AegisDocs.Core.Processes;
using AegisDocs.Core.Services;
using AegisDocs.UI.Interfaces;
using AegisDocs.UI.Services;
using AegisDocs.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AegisDocs.UI.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationService(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentService, WordDocumentService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ILocalAiService, LLamaAiService>();
        services.AddTransient<IAiProcessManager, AiProcessManager>();
        services.AddTransient<IIpcClient, NamedPipeClient>();

        return services;
    }
}
