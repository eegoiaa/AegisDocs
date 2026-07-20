using AegisDocs.Core.DTOs;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
        }

        return stringBuilder.ToString().Trim();
    }

    public void GenerateAuditReport(string outputPath, List<CorrectionItem> errors)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            Paragraph title = new Paragraph(new Run(new Text("Отчет об аудите договора")))
            {
                ParagraphProperties = new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Center })
            };
            body.AppendChild(title);
            body.AppendChild(new Paragraph(new Run(new Text($"Найдено ошибок: {errors.Count}"))));
            body.AppendChild(new Paragraph()); 

            Table table = new Table();

            TableProperties tblProp = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
                )
            );
            table.AppendChild(tblProp);

            TableRow headerRow = new TableRow();
            headerRow.Append(CreateCell("Категория", true));
            headerRow.Append(CreateCell("Как было (Ошибка)", true));
            headerRow.Append(CreateCell("Как надо (Исправление)", true));
            headerRow.Append(CreateCell("Обоснование ИИ", true));
            table.AppendChild(headerRow);

            foreach (var error in errors)
            {
                TableRow dataRow = new TableRow();
                dataRow.Append(CreateCell(error.Category));
                dataRow.Append(CreateCell(error.OriginalText));
                dataRow.Append(CreateCell(error.CorrectedText));
                dataRow.Append(CreateCell(error.Reason));
                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            mainPart.Document.Save();
        }
    }

    private TableCell CreateCell(string text, bool isHeader = false)
    {
        TableCell cell = new TableCell();
        Run run = new Run(new Text(text ?? string.Empty));

        if (isHeader)
            run.RunProperties = new RunProperties(new Bold());

        cell.Append(new Paragraph(run));
        return cell;
    }
}
