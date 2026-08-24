using AegisDocs.Core.DTOs;
using AegisDocs.Core.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AegisDocs.Core.Services;

public class AiResponseParserService : IAiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public List<CorrectionItem>? ParseAndFilterErrors(string rawAiResponse)
    {
        if (string.IsNullOrWhiteSpace(rawAiResponse)) return null;

        var results = new List<CorrectionItem>();

        string? cleanJson = ExtractFirstJsonArray(rawAiResponse);
        if (!string.IsNullOrEmpty(cleanJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<CorrectionItem>>(cleanJson, JsonOptions);
                if (parsed != null && parsed.Count > 0)
                {
                    results.AddRange(parsed);
                }
            }
            catch (JsonException)
            {
            }
        }

        if (results.Count == 0)
        {
            results = ExtractObjectsByRegex(rawAiResponse);
        }

        var validErrors = results
            .Where(e => !string.IsNullOrWhiteSpace(e.OriginalText) && !string.IsNullOrWhiteSpace(e.CorrectedText))
            .GroupBy(e => e.OriginalText.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        return validErrors.Count > 0 ? validErrors : null;
    }

    private string? ExtractFirstJsonArray(string text)
    {
        int startIndex = text.IndexOf('[');
        if (startIndex == -1) return null;

        int openBrackets = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '[') openBrackets++;
            else if (text[i] == ']') openBrackets--;

            if (openBrackets == 0)
            {
                return text.Substring(startIndex, i - startIndex + 1);
            }
        }

        return null;
    }

    private List<CorrectionItem> ExtractObjectsByRegex(string text)
    {
        var items = new List<CorrectionItem>();

        var objectMatches = Regex.Matches(text, @"\{[^{}]*\}", RegexOptions.Singleline);

        foreach (Match match in objectMatches)
        {
            try
            {
                var item = JsonSerializer.Deserialize<CorrectionItem>(match.Value, JsonOptions);
                if (item != null && !string.IsNullOrWhiteSpace(item.OriginalText))
                {
                    items.Add(item);
                }
            }
            catch
            {
                var fallbackItem = ExtractFieldsManually(match.Value);
                if (fallbackItem != null)
                {
                    items.Add(fallbackItem);
                }
            }
        }

        return items;
    }

    private CorrectionItem? ExtractFieldsManually(string block)
    {
        string GetValue(string fieldName)
        {
            var match = Regex.Match(block, $@"""?{fieldName}""?\s*:\s*""(.*?)""(?=\s*,\s*""?[a-zA-Z]+""?\s*:|\s*\}})", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        string original = GetValue("OriginalText");
        string corrected = GetValue("CorrectedText");
        string category = GetValue("Category");
        string reason = GetValue("Reason");

        if (string.IsNullOrWhiteSpace(original)) return null;

        return new CorrectionItem
        {
            Category = string.IsNullOrWhiteSpace(category) ? "Общая ошибка" : category,
            OriginalText = original,
            CorrectedText = corrected,
            Reason = reason
        };
    }
}
