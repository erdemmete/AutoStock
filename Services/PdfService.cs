using AutoStock.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class PdfService : IPdfService
{
    public byte[] CreateServicePdf(CreateServicePdfRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var total = request.Items.Sum(x => x.Price * x.Quantity);
        var documentNo = $"SF-{DateTime.Now:yyyyMMdd-HHmm}";

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => BuildHeader(c, documentNo));
                page.Content().Element(c => BuildContent(c, request, total));
                page.Footer().Element(BuildFooter);
            });
        });

        return pdf.GeneratePdf();
    }

    private static void BuildHeader(IContainer container, string documentNo)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("SERVİS FİŞİ")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    left.Item().Text("Araç bakım / onarım işlem özeti")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(170).AlignRight().Column(right =>
                {
                    right.Item().Text(documentNo)
                        .FontSize(12)
                        .Bold();

                    right.Item().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void BuildContent(IContainer container, CreateServicePdfRequest request, decimal total)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(18);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => InfoBox(c, "Müşteri Bilgileri",
                    ("Ad Soyad", request.CustomerName),
                    ("Telefon", request.CustomerPhone)));

                row.ConstantItem(20);

                row.RelativeItem().Element(c => InfoBox(c, "Araç Bilgileri",
                    ("Plaka", request.PlateNumber),
                    ("Araç", $"{request.VehicleBrand} {request.VehicleModel}".Trim())));
            });

            column.Item().Text("Yapılan İşlemler")
                .FontSize(15)
                .Bold()
                .FontColor(Colors.Grey.Darken3);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(90);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "İşlem");
                    HeaderCell(header.Cell(), "Adet");
                    HeaderCell(header.Cell(), "Birim Fiyat");
                    HeaderCell(header.Cell(), "Toplam");
                });

                foreach (var item in request.Items)
                {
                    var itemTotal = item.Price * item.Quantity;

                    BodyCell(table.Cell(), item.Description);
                    BodyCell(table.Cell(), item.Quantity.ToString());
                    BodyCell(table.Cell(), $"{item.Price:N2} TL");
                    BodyCell(table.Cell(), $"{itemTotal:N2} TL");
                }
            });

            column.Item().AlignRight().Background(Colors.Blue.Lighten5).Padding(15).Column(totalBox =>
            {
                totalBox.Item().Text("GENEL TOPLAM")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);

                totalBox.Item().Text($"{total:N2} TL")
                    .FontSize(22)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);
            });

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                column.Item().Element(c =>
                {
                    c.Background(Colors.Grey.Lighten4)
                     .Padding(12)
                     .Column(note =>
                     {
                         note.Item().Text("Not")
                             .Bold()
                             .FontSize(11);

                         note.Item().PaddingTop(4).Text(request.Note)
                             .FontSize(10)
                             .FontColor(Colors.Grey.Darken2);
                     });
                });
            }
        });
    }

    private static void BuildFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(8).AlignCenter().Text(text =>
            {
                text.Span("Bu belge bilgilendirme amacıyla oluşturulmuştur. ")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                text.Span("AutoStock")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);
            });
        });
    }

    private static void InfoBox(IContainer container, string title, params (string Label, string? Value)[] rows)
    {
        container
            .Background(Colors.Grey.Lighten5)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(13)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                column.Item().PaddingTop(8);

                foreach (var row in rows)
                {
                    column.Item().Text($"{row.Label}: {GetValue(row.Value)}")
                        .FontSize(10);
                }
            });
    }

    private static string GetValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Blue.Darken3)
            .PaddingVertical(8)
            .PaddingHorizontal(6)
            .Text(text)
            .FontColor(Colors.White)
            .Bold()
            .FontSize(10);
    }

    private static void BodyCell(IContainer container, string text)
    {
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(8)
            .PaddingHorizontal(6)
            .Text(text)
            .FontSize(10);
    }
}