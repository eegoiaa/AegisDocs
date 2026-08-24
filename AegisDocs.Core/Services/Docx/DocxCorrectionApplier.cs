using AegisDocs.Core.DTOs;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AegisDocs.Core.Services.Docx;

public class DocxCorrectionApplier
{
    private static readonly char[] TrimQuotes = new[] { '"', '«', '»', '\'', '“', '”', '`' };

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
                string search = CleanAndStripQuotes(correction.OriginalText);
                string replace = CleanAndStripQuotes(correction.CorrectedText);

                if (string.IsNullOrWhiteSpace(search) || string.IsNullOrWhiteSpace(replace)) continue;

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

        // 1. Прямой поиск
        int matchIndex = paragraphText.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        int matchedLength = search.Length;

        if (matchIndex < 0)
        {
            string normParagraph = NormalizePunctuationAndSpaces(paragraphText);
            string normSearch = NormalizePunctuationAndSpaces(search);

            matchIndex = normParagraph.IndexOf(normSearch, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0) return;

            matchedLength = Math.Min(search.Length, paragraphText.Length - matchIndex);
        }

        int currentPos = 0;
        bool isReplaced = false;

        foreach (var textNode in textNodes)
        {
            int nodeLen = textNode.Text.Length;

            if (currentPos + nodeLen > matchIndex && currentPos < matchIndex + matchedLength)
            {
                if (!isReplaced)
                {
                    int prefixLen = Math.Max(0, matchIndex - currentPos);
                    string prefix = textNode.Text.Substring(0, prefixLen);

                    int suffixStart = (matchIndex + matchedLength) - currentPos;
                    string suffix = suffixStart < nodeLen ? textNode.Text.Substring(suffixStart) : string.Empty;

                    textNode.Text = prefix + replace + suffix;
                    PreserveSpaces(textNode);
                    isReplaced = true;
                }
                else
                {
                    int suffixStart = (matchIndex + matchedLength) - currentPos;
                    textNode.Text = suffixStart < nodeLen ? textNode.Text.Substring(suffixStart) : string.Empty;
                    PreserveSpaces(textNode);
                }
            }

            currentPos += nodeLen;
        }
    }

    private string CleanAndStripQuotes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string trimmed = text.Trim();

        trimmed = trimmed.Trim(TrimQuotes).Trim();

        return trimmed.Replace('\u00A0', ' ');
    }

    private string NormalizePunctuationAndSpaces(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string normalized = text
            .Replace('«', '"')
            .Replace('»', '"')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('\u00A0', ' ');

        return string.Join(" ", normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private void PreserveSpaces(Text textNode)
    {
        if (textNode.Text.StartsWith(" ") || textNode.Text.EndsWith(" "))
        {
            textNode.Space = SpaceProcessingModeValues.Preserve;
        }
    }
}
