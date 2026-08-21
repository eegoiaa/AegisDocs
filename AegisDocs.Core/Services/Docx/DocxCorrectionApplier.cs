using AegisDocs.Core.DTOs;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AegisDocs.Core.Services.Docx;

public class DocxCorrectionApplier
{
    public void ApplyCorrections(string originalFilePath, string outputFilePath, List<CorrectionItem> corrections)
    {
        if (string.IsNullOrWhiteSpace(originalFilePath) || !File.Exists(originalFilePath))
            throw new FileNotFoundException("Исходный файл не найден", originalFilePath);

        File.Copy(originalFilePath, outputFilePath, true);

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(outputFilePath, true))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body == null) return;

            var paragraphs = body.Descendants<Paragraph>().ToList();

            foreach (var correction in corrections)
            {
                string search = CleanText(correction.OriginalText);
                string replace = CleanText(correction.CorrectedText);

                if (string.IsNullOrWhiteSpace(search)) continue;

                foreach (var paragraph in paragraphs)
                {
                    ApplyCorrectionToParagraph(paragraph, search, replace);
                }
            }

            wordDoc.MainDocumentPart.Document.Save();
        }
    }

    private void ApplyCorrectionToParagraph(Paragraph paragraph, string search, string replace)
    {
        var textNodes = paragraph.Descendants<Text>().ToList();
        if (textNodes.Count == 0) return;

        string paragraphText = string.Join("", textNodes.Select(t => t.Text));

        int matchIndex = paragraphText.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            string normalizedParagraph = NormalizeSpaces(paragraphText);
            string normalizedSearch = NormalizeSpaces(search);
            matchIndex = normalizedParagraph.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0) return;
        }

        int currentPos = 0;
        bool isReplaced = false;

        foreach (var textNode in textNodes)
        {
            int nodeLen = textNode.Text.Length;

            if (currentPos + nodeLen > matchIndex && currentPos < matchIndex + search.Length)
            {
                if (!isReplaced)
                {
                    int prefixLen = Math.Max(0, matchIndex - currentPos);
                    string prefix = textNode.Text.Substring(0, prefixLen);

                    int suffixStart = (matchIndex + search.Length) - currentPos;
                    string suffix = suffixStart < nodeLen ? textNode.Text.Substring(suffixStart) : string.Empty;

                    textNode.Text = prefix + replace + suffix;
                    PreserveSpaces(textNode);
                    isReplaced = true;
                }
                else
                {
                    int suffixStart = (matchIndex + search.Length) - currentPos;
                    textNode.Text = suffixStart < nodeLen ? textNode.Text.Substring(suffixStart) : string.Empty;
                    PreserveSpaces(textNode);
                }
            }

            currentPos += nodeLen;
        }
    }

    private string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string trimmed = text.Trim();

        if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
            (trimmed.StartsWith("«") && trimmed.EndsWith("»")))
        {
            if (trimmed.Length >= 2)
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        return trimmed.Replace('\u00A0', ' ');
    }

    private string NormalizeSpaces(string text)
    {
        return string.Join(" ", text.Split(new[] { ' ', '\u00A0', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private void PreserveSpaces(Text textNode)
    {
        if (textNode.Text.StartsWith(" ") || textNode.Text.EndsWith(" "))
        {
            textNode.Space = SpaceProcessingModeValues.Preserve;
        }
    }
}
