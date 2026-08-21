using AegisDocs.Core.DTOs;
using AegisDocs.Core.Services.Docx;

namespace AegisDocs.Core.Services;

public class WordDocumentService : IDocumentService
{
    private readonly DocxTextExtractor _textExtractor;
    private readonly DocxReportBuilder _reportBuilder;
    private readonly DocxCorrectionApplier _correctionApplier;

    public WordDocumentService()
    {
        _textExtractor = new DocxTextExtractor();
        _reportBuilder = new DocxReportBuilder();
        _correctionApplier = new DocxCorrectionApplier();
    }

    public string ExtractText(string filePath) => _textExtractor.ExtractText(filePath);

    public void GenerateAuditReport(string outputPath, List<CorrectionItem> errors, string originalFileName, string auditModeName) =>
        _reportBuilder.GenerateAuditReport(outputPath, errors, originalFileName, auditModeName);

    public void ApplyCorrections(string originalFilePath, string outputFilePath, List<CorrectionItem> corrections) =>
        _correctionApplier.ApplyCorrections(originalFilePath, outputFilePath, corrections);

}
