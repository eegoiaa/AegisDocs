using AegisDocs.Core.DTOs;

namespace AegisDocs.Core.Interfaces;

public interface IAiResponseParser
{
    List<CorrectionItem>? ParseAndFilterErrors(string rawAiResponse);
}
