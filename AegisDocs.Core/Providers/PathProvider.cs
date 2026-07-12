using AegisDocs.Core.Interfaces;

namespace AegisDocs.Core.Providers;

public class PathProvider : IPathProvider
{
    public string GetAiServerExePath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Для продакшена (когда всё в одной папке)
        string prodPath = Path.Combine(baseDir, "AegisDocs.AiServer.exe");
        if (File.Exists(prodPath)) return prodPath;

        DirectoryInfo? currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            string potentialServerProjectDir = Path.Combine(currentDir.FullName, "AegisDocs.AiServer");

            if (Directory.Exists(potentialServerProjectDir))
            {
                var exeFiles = Directory.GetFiles(potentialServerProjectDir, "AegisDocs.AiServer.exe", SearchOption.AllDirectories);
                if (exeFiles.Length > 0)
                {
                    return exeFiles[0]; 
                }
            }
            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException("Не удалось найти файл AegisDocs.AiServer.exe!");
    }

    public string GetModelPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string modelsDir = Path.Combine(appData, "AegisDocs", "Models");

        if(!Directory.Exists(modelsDir))
            Directory.CreateDirectory(modelsDir);

        string modelPath = Path.Combine(modelsDir, "qwen2.5-3b-instruct-q4_k_m.gguf");

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Файл модели не найден по пути: {modelPath}. Пожалуйста, поместите файл туда.");

        return modelPath;
    }
}
