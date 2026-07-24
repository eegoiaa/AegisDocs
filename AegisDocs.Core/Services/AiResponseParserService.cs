using AegisDocs.Core.DTOs;
using AegisDocs.Core.Interfaces;
using System.Text.Json;

namespace AegisDocs.Core.Services;

public class AiResponseParserService : IAiResponseParser
{
    public List<CorrectionItem>? ParseAndFilterErrors(string rawAiResponse)
    {
        if (string.IsNullOrWhiteSpace(rawAiResponse)) return null;

        int startIndex = rawAiResponse.IndexOf('[');
        int endIndex = -1;

        if (startIndex != -1)
        {
            int openBrackets = 0;
            for (int i = startIndex; i < rawAiResponse.Length; i++)
            {
                if (rawAiResponse[i] == '[') openBrackets++;
                else if (rawAiResponse[i] == ']') openBrackets--;

                if (openBrackets == 0)
                {
                    endIndex = i;
                    break;
                }
            }
        }

        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            string cleanJson = rawAiResponse.Substring(startIndex, endIndex - startIndex + 1);

            try
            {
                var parsedErrors = JsonSerializer.Deserialize<List<CorrectionItem>>(cleanJson);

                if (parsedErrors != null && parsedErrors.Count > 0)
                {
                    return parsedErrors
                        .GroupBy(e => e.OriginalText)
                        .Select(group => group.First())
                        .ToList();
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return null;
    }
}
