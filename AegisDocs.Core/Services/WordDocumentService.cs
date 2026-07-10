using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace AegisDocs.Core.Services;

public class WordDocumentService : IDocumentService
{
    public string ExtractText(string filePath)
    {
        var stringBuilder = new StringBuilder();

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;

            if (body != null)
            {
                var paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();

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
        }

        return stringBuilder.ToString().Trim();
    }
}
