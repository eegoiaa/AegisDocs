using AegisDocs.Core.Interfaces;
using LLama;
using LLama.Common;
using System.Text;

namespace AegisDocs.Core.Services;

public class LLamaAiService : ILocalAiService
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private bool _isInitialized;

    public async Task InitializeAsync(string modelPath)
    {
        if (_isInitialized) return;

        await Task.Run(() =>
        {
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                GpuLayerCount = 0,
                MainGpu = 0,
                UseMemorymap = false
            };

            _weights = LLamaWeights.LoadFromFile(parameters);
            _context = _weights.CreateContext(parameters);
            _isInitialized = true;
        });
    }

    public async Task<string> AnalyzeTextAsync(string systemPrompt, string userText, CancellationToken cancellationToken)
    {
        if (!_isInitialized || _context == null || _weights == null)
            throw new InvalidOperationException("Модель не инициализирована!");

        var executor = new InteractiveExecutor(_context);

        // Настройки генерации: низкая температура = меньше фантазии, больше юридической точности
        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1000,
            SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
            {
                Temperature = 0.1f 
            }
        };

        string prompt = $"{systemPrompt}\n\nТекст для анализа:\n{userText}\n\nОтвет:";

        var stringBuilder = new StringBuilder();

        await foreach (var text in executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            stringBuilder.Append(text);
        }

        return stringBuilder.ToString().Trim();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
    }

    
}
