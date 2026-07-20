using AegisDocs.Core.DTOs;

namespace AegisDocs.Core.Services;

public interface IDocumentService
{
    string ExtractText(string filePath);
    void GenerateAuditReport(string outputPath, List<CorrectionItem> errors);
}
