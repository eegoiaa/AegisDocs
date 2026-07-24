using System.Text.Json.Serialization;

namespace AegisDocs.Core.DTOs;

public class CorrectionItem
{
    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("OriginalText")]
    public string OriginalText { get; set; } = string.Empty;

    [JsonPropertyName("CorrectedText")]
    public string CorrectedText { get; set; } = string.Empty;

    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = string.Empty;
}
