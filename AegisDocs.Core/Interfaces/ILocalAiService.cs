namespace AegisDocs.Core.Interfaces;

public interface ILocalAiService : IDisposable
{
    /// <summary>
    /// Загружает веса модели в оперативную память (вызывается один раз).
    /// </summary>
    Task InitializeAsync(string modelPath);

    /// <summary>
    /// Анализирует кусок текста и возвращает результат.
    /// </summary>
    Task<string> AnalyzeTextAsync(string systemPrompt, string userText, CancellationToken cancellationToken);
}
