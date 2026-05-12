using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoStock.Services.Services;

public class PdfService : IPdfService
{
    public byte[] CreateServicePdf(CreateServicePdfRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var total = request.RequestGroups
    .SelectMany(x => x.Operations)
    .Sum(x => x.Quantity * x.UnitPrice);

        var documentNo = string.IsNullOrWhiteSpace(request.RecordNumber)
            ? $"SVX-{DateTime.Now:yyyyMMdd-HHmm}"
            : request.RecordNumber;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => BuildHeader(c, documentNo, request));
                page.Content().Element(c => BuildContent(c, request, total));
                page.Footer().Element(BuildFooter);
            });
        });

        return pdf.GeneratePdf();
    }

    private static void BuildHeader(IContainer container, string documentNo, CreateServicePdfRequest request)
    {
        var brandName = string.IsNullOrWhiteSpace(request.WorkshopName)
            ? "Servix"
            : request.WorkshopName;

        var qrText = $"{brandName} | Belge No: {documentNo} | Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}";
        var qrBytes = GenerateQrCode(qrText);

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(brandName)
                        .FontSize(30)
                        .Bold()
                        .FontColor(Colors.Blue.Darken4);

                    

                    if (!string.IsNullOrWhiteSpace(request.StatusText))
                    {
                        left.Item().PaddingTop(8).Text(request.StatusText)
                         .FontSize(10)
                         .Bold()
                         .FontColor(GetStatusColor(request.StatusText));
                    }
                });

                row.ConstantItem(155).AlignRight().Column(right =>
                {
                    right.Item().Text("Belge No")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    right.Item().Text(documentNo)
                        .FontSize(13)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    right.Item().PaddingTop(6).Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(75)
                    .AlignRight()
                    .Image(qrBytes);
            });

            column.Item()
                .PaddingTop(18)
                .LineHorizontal(1)
                .LineColor(Colors.Grey.Lighten2);
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
                    ("Telefon", request.CustomerPhone),
                    ("Mail", request.CustomerEmail)));

                row.ConstantItem(18);

                row.RelativeItem().Element(c => InfoBox(c, "Araç Bilgileri",
                    ("Plaka", request.Plate),
                    ("Marka", request.Brand),
                    ("Model", request.Model),
                    ("Model Yılı", request.ModelYear)));
            });

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                column.Item().Element(c =>
                {
                    c.Background(Colors.Grey.Lighten4)
                     .Border(1)
                     .BorderColor(Colors.Grey.Lighten2)
                     .Padding(12)
                     .Column(note =>
                     {
                         note.Item().Text("Servis Kabul Notu")
                             .Bold()
                             .FontSize(11)
                             .FontColor(Colors.Grey.Darken4);

                         note.Item().PaddingTop(4).Text(request.Note)
                             .FontSize(10)
                             .FontColor(Colors.Grey.Darken2);
                     });
                });
            }

            column.Item().Text("Şikayet / Talepler ve Yapılan İşlemler")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Grey.Darken4);

            if (request.RequestGroups.Count == 0)
            {
                column.Item().Element(c =>
                {
                    c.Background(Colors.Grey.Lighten5)
                     .Border(1)
                     .BorderColor(Colors.Grey.Lighten2)
                     .Padding(14)
                     .Text("Bu servis kaydı için talep girilmemiş.")
                     .FontSize(10)
                     .FontColor(Colors.Grey.Darken2);
                });
            }
            else
            {
                foreach (var group in request.RequestGroups)
                {
                    column.Item().Element(c => BuildRequestGroup(c, group));
                }
            }

            column.Item().AlignRight().Width(220).Element(c =>
            {
                c.Background(Colors.Blue.Lighten5)
                 .Border(1)
                 .BorderColor(Colors.Blue.Lighten3)
                 .Padding(16)
                 .Column(totalBox =>
                 {
                     totalBox.Item().Text("GENEL TOPLAM")
                         .FontSize(10)
                         .FontColor(Colors.Grey.Darken1);

                     totalBox.Item().PaddingTop(4).Text(FormatMoney(total))
                         .FontSize(24)
                         .Bold()
                         .FontColor(Colors.Blue.Darken4);
                 });
            });
        });
    }
    private static void BuildRequestGroup(IContainer container, ServicePdfRequestGroupDto group)
    {
        var groupTotal = group.Operations.Sum(x => x.Quantity * x.UnitPrice);

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(GetValue(group.Title))
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Blue.Darken4);

                        if (!string.IsNullOrWhiteSpace(group.Note))
                        {
                            left.Item().PaddingTop(3).Text(group.Note)
                                .FontSize(9)
                                .Italic()
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    row.ConstantItem(120).AlignRight().Text(FormatMoney(groupTotal))
                        .FontSize(11)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);
                });

                column.Item().PaddingTop(10);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(55);
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(90);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Tür");
                        HeaderCell(header.Cell(), "İşlem");
                        HeaderCell(header.Cell(), "Adet");
                        HeaderCell(header.Cell(), "Birim Fiyat");
                        HeaderCell(header.Cell(), "Toplam");
                    });

                    if (group.Operations.Count == 0)
                    {
                        BodyCell(table.Cell().ColumnSpan(5), "Bu talep için işlem eklenmedi.");
                    }
                    else
                    {
                        foreach (var item in group.Operations)
                        {
                            var itemTotal = item.Quantity * item.UnitPrice;

                            BodyCell(table.Cell(), GetValue(item.TypeText));

                            table.Cell().Element(c =>
                            {
                                c.BorderBottom(1)
                                 .BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(9)
                                 .PaddingHorizontal(7)
                                 .Column(col =>
                                 {
                                     col.Item().Text(GetValue(item.Name))
                                         .FontSize(10)
                                         .FontColor(Colors.Grey.Darken3);

                                     if (!string.IsNullOrWhiteSpace(item.Note))
                                     {
                                         col.Item().PaddingTop(3).Text(item.Note)
                                             .FontSize(8)
                                             .Italic()
                                             .FontColor(Colors.Grey.Darken1);
                                     }
                                 });
                            });

                            BodyCell(table.Cell(), item.Quantity.ToString());
                            BodyCell(table.Cell(), FormatMoney(item.UnitPrice));
                            BodyCell(table.Cell(), FormatMoney(itemTotal));
                        }
                    }
                });
            });
    }

    private static string GetStatusColor(string? statusText)
    {
        return statusText switch
        {
            "Açık Kayıt" => Colors.Blue.Darken3,
            "İşlemde" => Colors.Orange.Darken3,
            "Tamamlandı" => Colors.Green.Darken3,
            "İptal Edildi" => Colors.Red.Darken3,
            _ => Colors.Grey.Darken2
        };
    }

    private static void BuildSectionTitle(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Yapılan İşlemler")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Grey.Darken4);

            row.ConstantItem(120).AlignRight().Text("Servis Özeti")
                .FontSize(10)
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static void BuildFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text("Bu belge AutoStock tarafından otomatik oluşturulmuştur.")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void InfoBox(IContainer container, string title, params (string Label, string? Value)[] rows)
    {
        container
            .Background(Colors.Grey.Lighten5)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(14)
            .Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(13)
                    .Bold()
                    .FontColor(Colors.Blue.Darken4);

                column.Item().PaddingTop(10);

                foreach (var row in rows)
                {
                    column.Item().PaddingBottom(4).Row(r =>
                    {
                        r.ConstantItem(75).Text(row.Label)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        r.RelativeItem().Text(GetValue(row.Value))
                            .FontSize(10)
                            .Bold()
                            .FontColor(Colors.Grey.Darken4);
                    });
                }
            });
    }



    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Blue.Darken4)
            .PaddingVertical(9)
            .PaddingHorizontal(7)
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
            .PaddingVertical(9)
            .PaddingHorizontal(7)
            .Text(text)
            .FontSize(10)
            .FontColor(Colors.Grey.Darken3);
    }

    private static string GetValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string FormatMoney(decimal value)
    {
        return $"{value:N2} TL";
    }
    private static byte[] GenerateQrCode(string text)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);

        return pngQrCode.GetGraphic(20);
    }
}