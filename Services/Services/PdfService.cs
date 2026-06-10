using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoStock.Services.Services;

public class PdfService : IPdfService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public PdfService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public byte[] CreateServicePdf(CreateServicePdfRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var now = _dateTimeProvider.Now;

        var documentNo = string.IsNullOrWhiteSpace(request.RecordNumber)
            ? $"SVX-{now:yyyyMMdd-HHmm}"
            : request.RecordNumber;

        var requestRows = BuildServiceRows(request);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);

                page.DefaultTextStyle(x => x
                    .FontSize(9)
                    .FontFamily("Arial")
                    .FontColor(Colors.Grey.Darken4));

                page.Header().Element(c =>
                    BuildCompactHeader(
                        c,
                        documentNo,
                        request.WorkshopName,
                        null,
                        null,
                        now));

                page.Content().PaddingTop(8).Element(c =>
                    BuildServiceContent(c, request, requestRows));

                page.Footer().Element(c =>
                    BuildCompactFooter(c));
            });
        });

        return pdf.GeneratePdf();
    }

    public byte[] CreateQuickOfferPdf(CreateQuickOfferPdfRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var now = _dateTimeProvider.Now;
        var documentNo = $"QO-{now:yyyyMMdd-HHmm}";

        var requestRows = request.RequestItems
            .Select(x => new CompactRequestPdfRow(
                x.Title,
                x.Note,
                x.EstimatedAmount.HasValue && x.EstimatedAmount.Value > 0
                    ? x.EstimatedAmount.Value
                    : null))
            .ToList();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);

                page.DefaultTextStyle(x => x
                    .FontSize(9)
                    .FontFamily("Arial")
                    .FontColor(Colors.Grey.Darken4));

                page.Header().Element(c =>
                    BuildCompactHeader(
                        c,
                        documentNo,
                        request.WorkshopName,
                        null,
                        null,
                        now));

                page.Content().PaddingTop(8).Element(c =>
                    BuildQuickOfferContent(c, request, requestRows));

                page.Footer().Element(c =>
                    BuildCompactFooter(c));
            });
        });

        return pdf.GeneratePdf();
    }

    private static void BuildCompactHeader(
        IContainer container,
        string documentNo,
        string? workshopName,
        string? workshopAddress,
        string? workshopPhone,
        DateTime now)
    {
        var displayWorkshopName = string.IsNullOrWhiteSpace(workshopName)
            ? "OTOSERVİS A.Ş."
            : workshopName.Trim();

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(40)
                    .Height(40)
                    .Background(Colors.Blue.Lighten5)
                    .Border(1)
                    .BorderColor(Colors.Blue.Lighten3)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("S")
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                row.ConstantItem(10);

                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(displayWorkshopName)
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    left.Item().PaddingTop(3).Text(GetValue(workshopAddress, "Servis adresi belirtilmedi"))
                        .FontSize(8)
                        .Bold()
                        .FontColor(Colors.Grey.Darken1);

                    left.Item().Text($"Tel: {GetValue(workshopPhone, "Telefon belirtilmedi")}")
                        .FontSize(8)
                        .Bold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(230).AlignRight().Column(right =>
                {
                    right.Item().Text("SERVİS ÖN KABUL")
                        .FontSize(17)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    right.Item().Text("FORMU")
                        .FontSize(17)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    right.Item().PaddingTop(5).Text($"No: {documentNo}")
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    right.Item().PaddingTop(3).Text($"Tarih: {now:dd.MM.yyyy}")
                        .FontSize(8)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                });
            });

            column.Item()
                .PaddingTop(10)
                .LineHorizontal(1.2f)
                .LineColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildServiceContent(
        IContainer container,
        CreateServicePdfRequest request,
        List<CompactRequestPdfRow> requestRows)
    {
        var brandModel = JoinParts(request.Brand, request.Model);

        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Element(c =>
                BuildCompactInfoBand(
                    c,
                    new[]
                    {
                        ("Müşteri", request.CustomerName),
                        ("Tel", request.CustomerPhone)
                    },
                    new[]
                    {
                        ("Araç", request.Plate),
                        ("Marka/Model", brandModel),
                        ("Yıl", request.ModelYear)
                    }));

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                column.Item().Element(c =>
                {
                    c.Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten5)
                        .PaddingVertical(5)
                        .PaddingHorizontal(7)
                        .Text(text =>
                        {
                            text.Span("Not: ")
                                .FontSize(8)
                                .Bold()
                                .FontColor(Colors.Grey.Darken1);

                            text.Span(request.Note.Trim())
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken3);
                        });
                });
            }

            column.Item().Element(BuildRequestTitle);

            column.Item().Element(c =>
                BuildCompactRequestTable(c, requestRows));
        });
    }

    private static void BuildQuickOfferContent(
        IContainer container,
        CreateQuickOfferPdfRequest request,
        List<CompactRequestPdfRow> requestRows)
    {
        var brandModel = JoinParts(request.Brand, request.Model);
        var mileage = string.IsNullOrWhiteSpace(request.Mileage)
            ? null
            : $"{request.Mileage} KM";

        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Element(c =>
                BuildCompactInfoBand(
                    c,
                    new[]
                    {
                        ("Müşteri", request.CustomerName),
                        ("Tel", request.CustomerPhone)
                    },
                    new[]
                    {
                        ("Araç", request.Plate),
                        ("Marka/Model", brandModel),
                        ("KM", mileage),
                        ("Şasi", request.ChassisNumber)
                    }));

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                column.Item().Element(c =>
                {
                    c.Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten5)
                        .PaddingVertical(5)
                        .PaddingHorizontal(7)
                        .Text(text =>
                        {
                            text.Span("Not: ")
                                .FontSize(8)
                                .Bold()
                                .FontColor(Colors.Grey.Darken1);

                            text.Span(request.Note.Trim())
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken3);
                        });
                });
            }

            column.Item().Element(BuildRequestTitle);

            column.Item().Element(c =>
                BuildCompactRequestTable(c, requestRows));
        });
    }

    private static void BuildCompactInfoBand(
        IContainer container,
        (string Label, string? Value)[] firstLine,
        (string Label, string? Value)[] secondLine)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .Column(column =>
            {
                column.Item().Element(c => BuildInfoLine(c, firstLine));
                column.Item().PaddingTop(3).Element(c => BuildInfoLine(c, secondLine));
            });
    }

    private static void BuildInfoLine(
        IContainer container,
        (string Label, string? Value)[] items)
    {
        container.Text(text =>
        {
            foreach (var item in items)
            {
                text.Span($"{item.Label}: ")
                    .FontSize(8)
                    .Bold()
                    .FontColor(Colors.Grey.Darken1);

                text.Span($"{GetValue(item.Value)}   ")
                    .FontSize(8)
                    .Bold()
                    .FontColor(Colors.Grey.Darken4);
            }
        });
    }

    private static void BuildRequestTitle(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("YAPILACAK İŞLEMLER / MÜŞTERİ TALEPLERİ")
                .FontSize(10)
                .Bold()
                .FontColor(Colors.Grey.Darken4);

            column.Item()
                .PaddingTop(3)
                .LineHorizontal(1.2f)
                .LineColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildCompactRequestTable(
        IContainer container,
        List<CompactRequestPdfRow> rows)
    {
        var showPrice = rows.Any(x => x.Amount.HasValue && x.Amount.Value > 0);
        var total = rows.Sum(x => x.Amount ?? 0);

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn();

                if (showPrice)
                    columns.ConstantColumn(90);
            });

            table.Header(header =>
            {
                CompactHeaderCell(header.Cell(), "No");
                CompactHeaderCell(header.Cell(), "İşlem / Talep");

                if (showPrice)
                    CompactHeaderCell(header.Cell(), "Tahmini", alignRight: true);
            });

            if (rows.Count == 0)
            {
                CompactEmptyCell(
                    table.Cell().ColumnSpan(showPrice ? 3u : 2u),
                    "İşlem belirtilmedi.");

                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];

                CompactNoCell(table.Cell(), (i + 1).ToString());
                CompactTitleCell(table.Cell(), item.Title, item.Note);

                if (showPrice)
                {
                    CompactMoneyCell(
                        table.Cell(),
                        item.Amount.HasValue && item.Amount.Value > 0
                            ? FormatMoney(item.Amount.Value)
                            : "");
                }
            }

            if (showPrice)
            {
                CompactTotalLabelCell(table.Cell().ColumnSpan(2), "Tahmini Toplam");
                CompactMoneyCell(table.Cell(), FormatMoney(total), isTotal: true);
            }
        });
    }

    private static List<CompactRequestPdfRow> BuildServiceRows(CreateServicePdfRequest request)
    {
        var rows = new List<CompactRequestPdfRow>();

        if (request.RequestGroups == null || request.RequestGroups.Count == 0)
            return rows;

        foreach (var group in request.RequestGroups)
        {
            var groupTotal = group.Operations?.Sum(x => x.Quantity * x.UnitPrice) ?? 0;

            var noteParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(group.Note))
                noteParts.Add(group.Note.Trim());

            if (group.Operations != null && group.Operations.Count > 0)
            {
                var operationTexts = group.Operations.Select(operation =>
                {
                    var typeText = string.IsNullOrWhiteSpace(operation.TypeText)
                        ? ""
                        : $"{operation.TypeText}: ";

                    var quantityText = operation.Quantity > 1
                        ? $" ({operation.Quantity} adet)"
                        : "";

                    var noteText = string.IsNullOrWhiteSpace(operation.Note)
                        ? ""
                        : $" - {operation.Note.Trim()}";

                    return $"{typeText}{GetValue(operation.Name)}{quantityText}{noteText}";
                });

                noteParts.Add(string.Join(" | ", operationTexts));
            }

            rows.Add(new CompactRequestPdfRow(
                group.Title,
                noteParts.Count > 0 ? string.Join(" | ", noteParts) : null,
                groupTotal > 0 ? groupTotal : null));
        }

        return rows;
    }

    private static void CompactHeaderCell(
        IContainer container,
        string text,
        bool alignRight = false)
    {
        var cell = container
            .Background(Colors.Grey.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(5);

        if (alignRight)
        {
            cell.AlignRight()
                .Text(text)
                .FontSize(8)
                .Bold()
                .FontColor(Colors.Grey.Darken1);
        }
        else
        {
            cell.Text(text)
                .FontSize(8)
                .Bold()
                .FontColor(Colors.Grey.Darken1);
        }
    }

    private static void CompactNoCell(IContainer container, string text)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .AlignCenter()
            .Text(text)
            .FontSize(8)
            .Bold()
            .FontColor(Colors.Grey.Darken1);
    }

    private static void CompactTitleCell(
        IContainer container,
        string? title,
        string? note)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(5)
            .Column(column =>
            {
                column.Item().Text(GetValue(title))
                    .FontSize(8.5f)
                    .Bold()
                    .FontColor(Colors.Grey.Darken4);

                if (!string.IsNullOrWhiteSpace(note))
                {
                    column.Item().PaddingTop(2).Text(note.Trim())
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                }
            });
    }

    private static void CompactMoneyCell(
        IContainer container,
        string text,
        bool isTotal = false)
    {
        var cell = container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(isTotal ? Colors.Grey.Lighten5 : Colors.White)
            .PaddingVertical(4)
            .PaddingHorizontal(5)
            .AlignRight();

        cell.Text(text)
            .FontSize(8.5f)
            .Bold()
            .FontColor(Colors.Grey.Darken4);
    }

    private static void CompactTotalLabelCell(
        IContainer container,
        string text)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .PaddingVertical(4)
            .PaddingHorizontal(5)
            .AlignRight()
            .Text(text)
            .FontSize(8.5f)
            .Bold()
            .FontColor(Colors.Grey.Darken4);
    }

    private static void CompactEmptyCell(
        IContainer container,
        string text)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .AlignCenter()
            .Text(text)
            .FontSize(8)
            .Italic()
            .FontColor(Colors.Grey.Darken1);
    }

    private static void BuildCompactFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item()
                .LineHorizontal(1)
                .LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(
                        "Bu belge ön bilgilendirme amaçlıdır. Kesin teşhis, parça/işçilik ve nihai tutar servis incelemesi sonrası değişebilir.")
                    .FontSize(7)
                    .Bold()
                    .FontColor(Colors.Grey.Darken1);

                row.ConstantItem(150).AlignRight().Text("Sente360 ile oluşturulmuştur.")
                    .FontSize(7)
                    .Bold()
                    .FontColor(Colors.Grey.Darken4);
            });
        });
    }

    private static string GetValue(string? value, string fallback = "-")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string JoinParts(params string?[] values)
    {
        var parts = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim());

        return string.Join(" / ", parts);
    }

    private static string FormatMoney(decimal value)
    {
        return $"{value:N2} TL";
    }

    private sealed class CompactRequestPdfRow
    {
        public CompactRequestPdfRow(string? title, string? note, decimal? amount)
        {
            Title = title;
            Note = note;
            Amount = amount;
        }

        public string? Title { get; }

        public string? Note { get; }

        public decimal? Amount { get; }
    }
}