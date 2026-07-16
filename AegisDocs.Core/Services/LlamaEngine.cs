using LLama;
using LLama.Common;
using System.Runtime.CompilerServices;

namespace AegisDocs.AiServer;

public class LlamaEngine : IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;

    public void Initialize(string modelPath)
    {
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 0
        };

        _weights = LLamaWeights.LoadFromFile(parameters);
        _context = _weights.CreateContext(parameters);
        _executor = new InteractiveExecutor(_context);
    }

    public async IAsyncEnumerable<string> GenerateResponseAsync(string textToAnalyze, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_executor == null) throw new InvalidOperationException("Модель не инициализирована.");

        // Пока оставляем зашитый промпт тут, раз систему промптов отложили на потом
        string prompt = textToAnalyze;
        var inferenceParams = new InferenceParams { MaxTokens = 1500 };

        await foreach (var token in _executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
    }
}
