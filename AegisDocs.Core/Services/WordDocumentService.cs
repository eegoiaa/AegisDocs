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

    public void GenerateAuditReport(string outputPath, List<CorrectionItem> errors, string originalFileName, string auditModeName)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            Run titleRun = new Run(new Text($"Отчет: {auditModeName}"));
            titleRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RunFonts() { Ascii = "Arial" });
            body.AppendChild(new Paragraph(titleRun) { ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center }) });

            body.AppendChild(new Paragraph());

            AppendMetadataLine(body, "Проверенный файл:", originalFileName);
            AppendMetadataLine(body, "Дата проверки:", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            AppendMetadataLine(body, "Найдено ошибок:", errors.Count.ToString(), true); 

            Table table = new Table();

            TableProperties tblProp = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                    new RightBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct } 
            );
            table.AppendChild(tblProp);

            // Шапка таблицы (Серый фон)
            TableRow headerRow = new TableRow();
            headerRow.Append(CreateCell("Категория", true, "EFEFEF"));
            headerRow.Append(CreateCell("Как было (Ошибка)", true, "EFEFEF"));
            headerRow.Append(CreateCell("Как надо (Исправление)", true, "EFEFEF"));
            headerRow.Append(CreateCell("Обоснование ИИ", true, "EFEFEF"));
            table.AppendChild(headerRow);

            // Данные с цветовой индикацией
            foreach (var error in errors)
            {
                TableRow dataRow = new TableRow();
                dataRow.Append(CreateCell(error.Category));
                dataRow.Append(CreateCell(error.OriginalText, false, "FFEBEB")); 
                dataRow.Append(CreateCell(error.CorrectedText, false, "E8F5E9")); 
                dataRow.Append(CreateCell(error.Reason));
                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            mainPart.Document.Save();
        }
    }

    private TableCell CreateCell(string text, bool isHeader = false, string? bgColorHex = null)
    {
        TableCell cell = new TableCell();

        TableCellProperties tcp = new TableCellProperties();
        tcp.Append(new TableCellMargin(
            new TopMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
            new BottomMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
            new LeftMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
            new RightMargin() { Width = "100", Type = TableWidthUnitValues.Dxa }
        ));

        if (!string.IsNullOrEmpty(bgColorHex))
        {
            tcp.Append(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = bgColorHex });
        }
        cell.Append(tcp);

        Run run = new Run(new Text(text ?? string.Empty));
        RunProperties rPr = new RunProperties(
            new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", ComplexScript = "Arial" },
            new FontSize() { Val = "22" } 
        );
        if (isHeader) rPr.Append(new Bold());
        run.RunProperties = rPr;

        Paragraph p = new Paragraph(run);
        if (isHeader)
        {
            p.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
        }

        cell.Append(p);
        return cell;
    }

    private void AppendMetadataLine(Body docBody, string boldLabel, string normalText, bool addBottomSpacing = false)
    {
        Paragraph p = new Paragraph();

        Text boldText = new Text(boldLabel + " ");
        boldText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;

        Run boldRun = new Run(boldText);
        boldRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RunFonts() { Ascii = "Arial" });

        Run normalRun = new Run(new Text(normalText));
        normalRun.RunProperties = new RunProperties(new FontSize() { Val = "24" }, new RunFonts() { Ascii = "Arial" });

        p.AppendChild(boldRun);
        p.AppendChild(normalRun);

        if (addBottomSpacing)
        {
            p.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "400" });
        }

        docBody.AppendChild(p);
    }
}
