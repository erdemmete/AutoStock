using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AutoStock.Services.Calculations;

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
            ? $"SKF-{now:yyyyMMdd-HHmm}"
            : request.RecordNumber.Trim();

        var groups = request.RequestGroups ?? new List<ServicePdfRequestGroupDto>();
        var isMasked = request.IsPublicMasked;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(26);

                page.DefaultTextStyle(x => x
                    .FontSize(8.4f)
                    .FontFamily("Arial")
                    .FontColor(Colors.Grey.Darken4));

                page.Header().Element(c => BuildSkfHeader(c, request, documentNo, now, isMasked));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Element(c => BuildSkfInfoArea(c, request, isMasked));

                    if (!string.IsNullOrWhiteSpace(request.Note))
                    {
                        column.Item().Element(c => BuildSoftNote(c, "Servis Notu", request.Note));
                    }

                    column.Item().Element(c => BuildSkfSectionTitle(c, "Servis Kayıt Detayı", "Müşteri talepleri ve servis tarafından uygulanan işlemler"));

                    if (groups.Count == 0)
                    {
                        column.Item().Element(c => BuildEmptyState(c, "Bu servis kaydı için işlem/talep detayı bulunmuyor."));
                    }
                    else
                    {
                        for (var i = 0; i < groups.Count; i++)
                        {
                            var group = groups[i];
                            column.Item().Element(c => BuildRequestGroup(c, group, i + 1, isMasked));
                        }
                    }

                    // Alt alanı aynı sayfanın altına yaslar.
                    // Ayrı ExtendVertical + sonrasında yeni item yaparsak QuestPDF boş ikinci sayfa üretebilir.
                    column.Item()
                        .ExtendVertical()
                        .AlignBottom()
                        .ShowEntire()
                        .Element(c => BuildSkfBottomArea(c, request, groups));
                });

                page.Footer().Element(c => BuildSkfFooter(c, isMasked));
            });
        });

        return pdf.GeneratePdf();
    }

    public byte[] CreateQuickOfferPdf(CreateQuickOfferPdfRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var now = _dateTimeProvider.Now;
        var documentNo = $"ONK-{now:yyyyMMdd-HHmm}";

        var requestRows = request.RequestItems
            .Select(x => new QuickOfferRow(
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

                page.Header().Element(c => BuildQuickOfferHeader(c, documentNo, request.WorkshopName, now));

                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Element(c => BuildQuickOfferInfo(c, request));

                    if (!string.IsNullOrWhiteSpace(request.Note))
                    {
                        column.Item().Element(c => BuildSoftNote(c, "Not", request.Note));
                    }

                    column.Item().Element(c => BuildSkfSectionTitle(c, "Servis Ön Kabul", "Müşteri talepleri ve ilk kabul bilgileri"));
                    column.Item().Element(c => BuildQuickOfferTable(c, requestRows));
                });

                page.Footer().Element(c => BuildQuickOfferFooter(c));
            });
        });

        return pdf.GeneratePdf();
    }

    private static void BuildSkfHeader(
        IContainer container,
        CreateServicePdfRequest request,
        string documentNo,
        DateTime now,
        bool isMasked)
    {
        var workshopName = GetValue(request.WorkshopName, "Servis İşletmesi");

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(44)
                    .Height(44)
                    .Background(Colors.Blue.Lighten5)
                    .Border(1)
                    .BorderColor(Colors.Blue.Lighten3)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("S")
                    .FontSize(19)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                row.ConstantItem(12);

                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(workshopName)
                        .FontSize(16.5f)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    if (!string.IsNullOrWhiteSpace(request.WorkshopAddress))
                    {
                        left.Item().PaddingTop(3).Text(request.WorkshopAddress.Trim())
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrWhiteSpace(request.WorkshopPhone))
                    {
                        left.Item().Text($"Tel: {request.WorkshopPhone.Trim()}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(210).AlignRight().Column(right =>
                {
                    right.Item().Text("SERVİS KAYIT FORMU")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    right.Item().PaddingTop(4).Text($"Belge No: {documentNo}")
                        .FontSize(8.5f)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    right.Item().Text($"Tarih: {now:dd.MM.yyyy HH:mm}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1.2f).LineColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildSkfInfoArea(IContainer container, CreateServicePdfRequest request, bool isMasked)
    {
        var customerName = isMasked
            ? MaskName(request.CustomerName)
            : request.CustomerName;

        var plate = isMasked
            ? MaskPlate(request.Plate)
            : request.Plate;

        var brandModel = JoinParts(request.Brand, request.Model);

        var engineSummary = BuildEngineSummary(
            request.EngineCapacityCc,
            request.EnginePowerHp,
            request.EngineCode);

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .PaddingVertical(12)
            .PaddingHorizontal(14)
            .Row(row =>
            {
                row.RelativeItem(0.85f).Column(left =>
                {
                    left.Spacing(7);

                    left.Item().Text("Müşteri Bilgisi")
                        .FontSize(8.5f)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    left.Item().Element(c => BuildInfoLine(c, "Müşteri", customerName));

                    if (!isMasked && !string.IsNullOrWhiteSpace(request.CustomerPhone))
                    {
                        left.Item().Element(c => BuildInfoLine(c, "Telefon", request.CustomerPhone));
                    }
                });

                row.ConstantItem(28);

                row.RelativeItem(1.8f).Column(vehicle =>
                {
                    vehicle.Spacing(7);

                    vehicle.Item().Text("Araç Bilgisi")
                        .FontSize(8.5f)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    vehicle.Item().Row(vehicleRows =>
                    {
                        vehicleRows.RelativeItem().Column(col =>
                        {
                            col.Spacing(6);

                            col.Item().Element(c => BuildInfoLine(c, "Plaka", plate));

                            if (!string.IsNullOrWhiteSpace(brandModel))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Marka / Model", brandModel));
                            }

                            if (!string.IsNullOrWhiteSpace(request.VehicleVariantName))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Versiyon / Motor", request.VehicleVariantName));
                            }

                            if (!isMasked && !string.IsNullOrWhiteSpace(request.ModelYear))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Model Yılı", request.ModelYear));
                            }
                        });

                        vehicleRows.ConstantItem(24);

                        vehicleRows.RelativeItem().Column(col =>
                        {
                            col.Spacing(6);

                            if (!string.IsNullOrWhiteSpace(request.FuelType))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Yakıt Tipi", request.FuelType));
                            }

                            if (!string.IsNullOrWhiteSpace(request.TransmissionType))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Şanzıman", request.TransmissionType));
                            }

                            if (!string.IsNullOrWhiteSpace(request.BodyType))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Kasa Tipi", request.BodyType));
                            }

                            if (!string.IsNullOrWhiteSpace(engineSummary))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Motor", engineSummary));
                            }

                            if (!isMasked && !string.IsNullOrWhiteSpace(request.ChassisNumber))
                            {
                                col.Item().Element(c => BuildInfoLine(c, "Şasi No", request.ChassisNumber));
                            }
                        });
                    });
                });
            });
    }

    private static void BuildInfoPanel(
        IContainer container,
        string title,
        IReadOnlyCollection<(string Label, string? Value)> items)
    {
        var visibleItems = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToList();

        container.Column(column =>
        {
            column.Item().Text(title)
                .FontSize(8)
                .Bold()
                .FontColor(Colors.Blue.Darken3);

            column.Item().PaddingTop(5).Column(lines =>
            {
                lines.Spacing(4);

                if (visibleItems.Count == 0)
                {
                    lines.Item().Text("Bilgi bulunmuyor")
                        .FontSize(7.6f)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);

                    return;
                }

                foreach (var item in visibleItems)
                {
                    lines.Item().Element(c => BuildInfoLine(c, item.Label, item.Value));
                }
            });
        });
    }

    private static void BuildInfoLine(IContainer container, string label, string? value)
    {
        container.Row(row =>
        {
            row.ConstantItem(70).Text(label)
                .FontSize(7.1f)
                .Bold()
                .FontColor(Colors.Grey.Darken1);

            row.RelativeItem().Text(GetValue(value))
                .FontSize(8.2f)
                .Bold()
                .FontColor(Colors.Grey.Darken4);
        });
    }

    private static void AddInfoRow(
        ColumnDescriptor column,
        params (string Label, string? Value, float Weight)[] items)
    {
        var visibleItems = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToList();

        if (visibleItems.Count == 0)
            return;

        column.Item().Row(row =>
        {
            for (var i = 0; i < visibleItems.Count; i++)
            {
                var item = visibleItems[i];

                if (i > 0)
                    row.ConstantItem(10);

                InfoCell(row.RelativeItem(item.Weight), item.Label, item.Value);
            }
        });
    }

    private static void InfoCell(IContainer container, string label, string? value)
    {
        container.Column(column =>
        {
            column.Item().Text(label)
                .FontSize(7.2f)
                .Bold()
                .FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(2).Text(GetValue(value))
                .FontSize(8.3f)
                .Bold()
                .FontColor(Colors.Grey.Darken4);
        });
    }

    private static void BuildRequestGroup(
        IContainer container,
        ServicePdfRequestGroupDto group,
        int groupNumber,
        bool isMasked)
    {
        var operations = group.Operations ?? new List<ServicePdfItemDto>();
        var groupTotal = CalculateGroupTotal(group);

        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Column(card =>
            {
                card.Item()
                    .Background(Colors.Grey.Lighten5)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(7)
                    .PaddingHorizontal(9)
                    .Row(row =>
                    {
                        row.ConstantItem(28)
                            .Height(24)
                            .Background(Colors.Blue.Lighten5)
                            .Border(1)
                            .BorderColor(Colors.Blue.Lighten3)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(groupNumber.ToString())
                            .FontSize(9)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        row.ConstantItem(8);

                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text(GetValue(group.Title, "Servis talebi"))
                                .FontSize(9.3f)
                                .Bold()
                                .FontColor(Colors.Grey.Darken4);

                            if (!string.IsNullOrWhiteSpace(group.Note))
                            {
                                title.Item().PaddingTop(2).Text(group.Note.Trim())
                                    .FontSize(7.8f)
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });

                        if (groupTotal > 0)
                        {
                            row.ConstantItem(96).AlignRight().AlignMiddle().Text(FormatMoney(groupTotal))
                                .FontSize(9)
                                .Bold()
                                .FontColor(Colors.Grey.Darken4);
                        }
                    });

                if (operations.Count == 0)
                {
                    card.Item().Padding(10).Text("Bu talep için henüz işlem detayı kaydedilmedi.")
                        .FontSize(7.8f)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    card.Item().Element(c => BuildOperationTable(c, operations, isMasked));
                }
            });
    }

    private static void BuildOperationTable(IContainer container, List<ServicePdfItemDto> operations, bool isMasked)
    {
        var showPrice = operations.Any(operation =>
        {
            var amount = CalculateOperationTotal(operation);

            return amount > 0 || operation.UnitPrice > 0;
        });

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(2.2f);

                if (showPrice)
                {
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(72);
                    columns.ConstantColumn(78);
                }
            });

            table.Header(header =>
            {
                TableHeader(header.Cell(), "İşlem / Parça");
                TableHeader(header.Cell(), "Açıklama");

                if (showPrice)
                {
                    TableHeader(header.Cell(), "Miktar", alignRight: true);
                    TableHeader(header.Cell(), "Birim", alignRight: true);
                    TableHeader(header.Cell(), "Tutar", alignRight: true);
                }
            });

            foreach (var operation in operations)
            {
                var operationName = JoinParts(operation.TypeText, operation.Name).Replace(" / ", ": ");
                var amount = CalculateOperationTotal(operation);

                TableCell(table.Cell(), GetValue(operationName), bold: true);
                TableCell(table.Cell(), operation.Note);

                if (showPrice)
                {
                    TableCell(table.Cell(), operation.Quantity > 0 ? operation.Quantity.ToString("N0") : "-", alignRight: true);
                    TableCell(table.Cell(), operation.UnitPrice > 0 ? FormatMoney(operation.UnitPrice) : "-", alignRight: true);
                    TableCell(table.Cell(), amount > 0 ? FormatMoney(amount) : "-", alignRight: true, bold: true);
                }
            }
        });
    }

    private static void TableHeader(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .Background(Colors.Grey.Lighten4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(6);

        if (alignRight)
        {
            cell.AlignRight().Text(text).FontSize(7.4f).Bold().FontColor(Colors.Grey.Darken1);
            return;
        }

        cell.Text(text).FontSize(7.4f).Bold().FontColor(Colors.Grey.Darken1);
    }

    private static void TableCell(IContainer container, string? text, bool alignRight = false, bool bold = false)
    {
        var cell = container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(5)
            .PaddingHorizontal(6);

        var value = GetValue(text);

        if (alignRight)
        {
            var descriptor = cell.AlignRight().Text(value).FontSize(7.8f).FontColor(Colors.Grey.Darken4);
            if (bold) descriptor.Bold();
            return;
        }

        var textDescriptor = cell.Text(value).FontSize(7.8f).FontColor(Colors.Grey.Darken4);
        if (bold) textDescriptor.Bold();
    }

    private static void BuildSkfBottomArea(
        IContainer container,
        CreateServicePdfRequest request,
        List<ServicePdfRequestGroupDto> groups)
    {
        var total = CalculateGrandTotal(groups);
        var hasBankAccounts = request.BankAccounts != null && request.BankAccounts.Any(x => !string.IsNullOrWhiteSpace(x.Iban));

        container.Column(column =>
        {
            column.Spacing(8);

            if (hasBankAccounts)
            {
                column.Item().Element(c => BuildPaymentInfo(c, request.BankAccounts!));
            }

            if (total > 0)
            {
                column.Item().Element(c => BuildGrandTotal(c, total));
            }

            if (request.VehicleQrPngBytes is { Length: > 0 } &&
                !string.IsNullOrWhiteSpace(request.VehicleQrPublicUrl))
            {
                column.Item().Element(c => BuildVehicleQrInfo(c, request.VehicleQrPngBytes, request.VehicleQrPublicUrl));
            }

            column.Item().Element(BuildLegalBox);
        });
    }

    private static void BuildVehicleQrInfo(IContainer container, byte[] qrBytes, string publicUrl)
    {
        container.Border(1)
            .BorderColor(Colors.Blue.Lighten4)
            .Background(Colors.Blue.Lighten5)
            .Padding(9)
            .Row(row =>
            {
                row.ConstantItem(58)
                    .Height(58)
                    .Background(Colors.White)
                    .Padding(4)
                    .Image(qrBytes)
                    .FitArea();

                row.ConstantItem(10);

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Araç servis geçmişi")
                        .FontSize(8.8f)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    column.Item().PaddingTop(3).Text("Bu QR kod ile aracın public servis geçmişi görüntülenebilir.")
                        .FontSize(7.4f)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().PaddingTop(2).Text(publicUrl)
                        .FontSize(6.8f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
    }

    private static void BuildGrandTotal(IContainer container, decimal total)
    {
        container.AlignRight()
            .Width(245)
            .Border(1)
            .BorderColor(Colors.Blue.Lighten4)
            .Background(Colors.Blue.Lighten5)
            .PaddingVertical(9)
            .PaddingHorizontal(11)
            .Row(row =>
            {
                row.RelativeItem().Text("Servis Toplamı")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                row.ConstantItem(118).AlignRight().Text(FormatMoney(total))
                    .FontSize(10.2f)
                    .Bold()
                    .FontColor(Colors.Grey.Darken4);
            });
    }

    private static void BuildPaymentInfo(IContainer container, List<ServicePdfBankAccountDto> bankAccounts)
    {
        var accounts = bankAccounts
            .Where(x => !string.IsNullOrWhiteSpace(x.Iban))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.BankName)
            .Take(3)
            .ToList();

        if (accounts.Count == 0) return;

        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text("Ödeme Bilgileri")
                    .FontSize(8.4f)
                    .Bold()
                    .FontColor(Colors.Grey.Darken3);

                column.Item().PaddingTop(4).Column(lines =>
                {
                    lines.Spacing(2);

                    foreach (var account in accounts)
                    {
                        lines.Item().Text(text =>
                        {
                            text.Span(GetValue(account.BankName)).FontSize(7.4f).Bold().FontColor(Colors.Grey.Darken4);
                            text.Span("  •  ").FontSize(7.4f).FontColor(Colors.Grey.Darken1);
                            text.Span(GetValue(account.Iban)).FontSize(7.4f).Bold().FontColor(Colors.Grey.Darken4);

                            if (!string.IsNullOrWhiteSpace(account.CurrencyCode))
                            {
                                text.Span($"  {account.CurrencyCode.Trim()}").FontSize(7.2f).Bold().FontColor(Colors.Grey.Darken1);
                            }

                            if (!string.IsNullOrWhiteSpace(account.AccountHolder))
                            {
                                text.Span("  •  Alıcı: ").FontSize(7.2f).FontColor(Colors.Grey.Darken1);
                                text.Span(account.AccountHolder.Trim()).FontSize(7.2f).FontColor(Colors.Grey.Darken2);
                            }
                        });
                    }
                });
            });
    }

    private static void BuildLegalBox(IContainer container)
    {
        const string text = "Bu belge, araç üzerinde yapılan servis işlemlerini ve işlem detaylarını gösteren servis kayıt formudur. Resmi mali belge veya fatura yerine geçmez.";

        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .PaddingVertical(7)
            .PaddingHorizontal(9)
            .Text(text)
            .FontSize(7.3f)
            .FontColor(Colors.Grey.Darken1);
    }

    private static void BuildSkfFooter(IContainer container, bool isMasked)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(" ")
                .FontSize(6)
                .FontColor(Colors.Grey.Lighten1);

            row.ConstantItem(170).AlignRight().Text("Sente360 ile oluşturulmuştur.")
                .FontSize(7)
                .Bold()
                .FontColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildSkfSectionTitle(IContainer container, string title, string subtitle)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(11).Bold().FontColor(Colors.Grey.Darken4);
            column.Item().Text(subtitle).FontSize(7.8f).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildSoftNote(IContainer container, string label, string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;

        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(7)
            .Text(text =>
            {
                text.Span($"{label}: ").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                text.Span(note.Trim()).FontSize(8).FontColor(Colors.Grey.Darken3);
            });
    }

    private static void BuildEmptyState(IContainer container, string text)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(14)
            .AlignCenter()
            .Text(text)
            .FontSize(8)
            .Italic()
            .FontColor(Colors.Grey.Darken1);
    }

    private static void BuildQuickOfferHeader(IContainer container, string documentNo, string? workshopName, DateTime now)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(GetValue(workshopName, "Servis İşletmesi"))
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Grey.Darken4);

                row.ConstantItem(210).AlignRight().Column(right =>
                {
                    right.Item().Text("SERVİS ÖN KABUL FORMU")
                        .FontSize(15)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    right.Item().Text($"No: {documentNo}  •  {now:dd.MM.yyyy}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(8).LineHorizontal(1.2f).LineColor(Colors.Grey.Darken3);
        });
    }

    private static void BuildQuickOfferInfo(IContainer container, CreateQuickOfferPdfRequest request)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"Müşteri: {GetValue(request.CustomerName)}    Tel: {GetValue(request.CustomerPhone)}").FontSize(8).Bold();
            column.Item().Text($"Araç: {GetValue(request.Plate)}    Marka/Model: {GetValue(JoinParts(request.Brand, request.Model))}    KM: {GetValue(request.Mileage)}").FontSize(8).Bold();
        });
    }

    private static void BuildQuickOfferTable(IContainer container, List<QuickOfferRow> rows)
    {
        var showPrice = rows.Any(x => x.Amount.HasValue && x.Amount.Value > 0);

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(32);
                columns.RelativeColumn();
                if (showPrice) columns.ConstantColumn(90);
            });

            table.Header(header =>
            {
                TableHeader(header.Cell(), "No");
                TableHeader(header.Cell(), "Talep");
                if (showPrice) TableHeader(header.Cell(), "Tahmini", alignRight: true);
            });

            if (rows.Count == 0)
            {
                table.Cell().ColumnSpan(showPrice ? 3u : 2u).Padding(12).AlignCenter().Text("Talep belirtilmedi.").FontSize(8).Italic();
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                TableCell(table.Cell(), (i + 1).ToString(), bold: true);
                TableCell(table.Cell(), JoinParts(row.Title, row.Note));
                if (showPrice) TableCell(table.Cell(), row.Amount.HasValue ? FormatMoney(row.Amount.Value) : "", alignRight: true, bold: true);
            }
        });
    }

    private static void BuildQuickOfferFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(5).Text("Bu belge ön bilgilendirme amaçlıdır. Nihai servis işlemleri inceleme sonrası değişebilir. Sente360 ile oluşturulmuştur.")
                .FontSize(7)
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static decimal CalculateGrandTotal(List<ServicePdfRequestGroupDto> groups)
    {
        return groups.Sum(CalculateGroupTotal);
    }

    private static decimal CalculateGroupTotal(ServicePdfRequestGroupDto group)
    {
        var operations = group.Operations ?? new List<ServicePdfItemDto>();
        var operationTotal = operations.Sum(CalculateOperationTotal);

        if (operationTotal > 0) return operationTotal;

        return group.EstimatedAmount.HasValue && group.EstimatedAmount.Value > 0
            ? group.EstimatedAmount.Value
            : 0;
    }

    private static decimal CalculateOperationTotal(ServicePdfItemDto operation)
    {
        var quantity = operation.Quantity > 0 ? operation.Quantity : 1;
        var unitPrice = operation.UnitPrice;

        if (unitPrice <= 0 && operation.TotalPrice > 0)
        {
            unitPrice = operation.TotalPrice / quantity;
        }

        return ServiceRecordTotalsCalculator.CalculateLine(new ServiceFinancialLine(
            quantity,
            unitPrice,
            operation.DiscountRate,
            operation.VatRate)).GrandTotal;
    }

    private static string GetValue(string? value, string fallback = "-")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string JoinParts(params string?[] values)
    {
        return string.Join(" / ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }

    private static string FormatMoney(decimal value)
    {
        return $"{value:N2} TL";
    }

    private static string? BuildEngineSummary(int? engineCapacityCc, int? enginePowerHp, string? engineCode)
    {
        var parts = new List<string>();

        if (engineCapacityCc.HasValue && engineCapacityCc.Value > 0)
            parts.Add($"{engineCapacityCc.Value:N0} cc");

        if (enginePowerHp.HasValue && enginePowerHp.Value > 0)
            parts.Add($"{enginePowerHp.Value} hp");

        if (!string.IsNullOrWhiteSpace(engineCode))
            parts.Add(engineCode.Trim());

        return parts.Count > 0 ? string.Join(" / ", parts) : null;
    }

    private static string MaskName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Select(part => part.Length <= 2 ? part[0] + "*" : part[0] + new string('*', Math.Min(4, part.Length - 1))));
    }

    private static string MaskPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 7) return "***";
        return $"{digits[..4]} *** ** {digits[^2..]}";
    }

    private static string MaskPlate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        if (value.Contains('*')) return value.Trim();
        var clean = value.Trim().ToUpperInvariant().Replace(" ", "");
        if (clean.Length <= 4) return clean[0] + "***";
        return $"{clean[..Math.Min(4, clean.Length)]}***";
    }

    private sealed class QuickOfferRow
    {
        public QuickOfferRow(string? title, string? note, decimal? amount)
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
