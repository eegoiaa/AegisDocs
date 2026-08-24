using LLama;
using LLama.Common;
using LLama.Sampling;
using System.Text;


namespace AegisDocs.AiServer;

public class LlamaEngine : IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ModelParams? _parameters;
    private readonly object _syncLock = new();

    public bool IsInitialized => _weights != null && _context != null && _executor != null;

    public void Initialize(string modelPath)
    {
        if (IsInitialized) return;

        _parameters = new ModelParams(modelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 0
        };

        _weights = LLamaWeights.LoadFromFile(_parameters);
        _context = _weights.CreateContext(_parameters);
        _executor = new InteractiveExecutor(_context);
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userText, CancellationToken cancellationToken)
    {
        if (!IsInitialized || _parameters == null || _weights == null)
            throw new InvalidOperationException("LlamaEngine не инициализирован");

        lock (_syncLock)
        {
            _context?.Dispose();
            _context = _weights.CreateContext(_parameters);
            _executor = new InteractiveExecutor(_context);
        }

        string formattedPrompt = $"<|system|>\n{systemPrompt}</s>\n<|user|>\n{userText}</s>\n<|assistant|>\n";

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1500,
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.1f,
                TopP = 0.9f,
                RepeatPenalty = 1.15f
            },
            AntiPrompts = new[] { "</s>", "<|user|>", "<|system|>", "]\n\n", "] ]" }
        };

        var responseBuilder = new StringBuilder();

        await foreach (var token in _executor.InferAsync(formattedPrompt, inferenceParams, cancellationToken))
        {
            responseBuilder.Append(token);

            string currentText = responseBuilder.ToString();
            if (IsCompleteJsonArray(currentText))
            {
                break;
            }
        }

        return responseBuilder.ToString().Trim();
    }

    private bool IsCompleteJsonArray(string text)
    {
        int startIndex = text.IndexOf('[');
        if (startIndex == -1) return false;

        int openBrackets = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '[') openBrackets++;
            else if (text[i] == ']') openBrackets--;

            if (openBrackets == 0 && i > startIndex + 2)
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
    }
}
