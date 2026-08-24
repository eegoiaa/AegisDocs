using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace AegisDocs.Core.Services.Docx;

public class DocxTextExtractor
{
    public string ExtractText(string filePath)
    {
        var stringBuilder = new StringBuilder();

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            var paragraphs = body.Descendants<Paragraph>();

            foreach (var paragraph in paragraphs)
            {
                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    stringBuilder.AppendLine(text);
                }
            }

            if (stringBuilder.Length == 0 && !string.IsNullOrWhiteSpace(body.InnerText))
            {
                stringBuilder.Append(body.InnerText);
            }
        }

        return stringBuilder.ToString().Trim();
    }
}
