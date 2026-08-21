using AegisDocs.Core.DTOs;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AegisDocs.Core.Services.Docx;

public class DocxReportBuilder
{
    public void GenerateAuditReport(string outputPath, List<CorrectionItem> errors, string originalFileName, string auditModeName)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            Run titleRun = new Run(new Text($"Отчет: {auditModeName}"))
            {
                RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RunFonts() { Ascii = "Arial" })
            };
            body.AppendChild(new Paragraph(titleRun)
            {
                ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center })
            });

            body.AppendChild(new Paragraph());

            AppendMetadataLine(body, "Проверенный файл:", originalFileName);
            AppendMetadataLine(body, "Дата проверки:", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            AppendMetadataLine(body, "Найдено ошибок:", errors.Count.ToString(), addBottomSpacing: true);

            Table table = new Table();
            table.AppendChild(CreateReportTableProperties());

            TableRow headerRow = new TableRow();
            headerRow.Append(CreateCell("Категория", isHeader: true, "EFEFEF"));
            headerRow.Append(CreateCell("Как было (Ошибка)", isHeader: true, "EFEFEF"));
            headerRow.Append(CreateCell("Как надо (Исправление)", isHeader: true, "EFEFEF"));
            headerRow.Append(CreateCell("Обоснование ИИ", isHeader: true, "EFEFEF"));
            table.AppendChild(headerRow);

            foreach (var error in errors)
            {
                TableRow dataRow = new TableRow();
                dataRow.Append(CreateCell(error.Category));
                dataRow.Append(CreateCell(error.OriginalText, isHeader: false, "FFEBEB"));
                dataRow.Append(CreateCell(error.CorrectedText, isHeader: false, "E8F5E9"));
                dataRow.Append(CreateCell(error.Reason));
                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            mainPart.Document.Save();
        }
    }

    private TableProperties CreateReportTableProperties()
    {
        return new TableProperties(
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

        Text boldText = new Text(boldLabel + " ") { Space = SpaceProcessingModeValues.Preserve };
        Run boldRun = new Run(boldText)
        {
            RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RunFonts() { Ascii = "Arial" })
        };

        Run normalRun = new Run(new Text(normalText))
        {
            RunProperties = new RunProperties(new FontSize() { Val = "24" }, new RunFonts() { Ascii = "Arial" })
        };

        p.AppendChild(boldRun);
        p.AppendChild(normalRun);

        if (addBottomSpacing)
        {
            p.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "400" });
        }

        docBody.AppendChild(p);
    }
}
