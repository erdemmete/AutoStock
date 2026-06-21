using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO.Compression;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoStock.Services.Services;

public class InvoiceExportService : IInvoiceExportService
{
    private const string PaymentCancellationDocumentPrefix = "PAY-CANCEL-";
    private const int MaxExportDayRange = 370;
    private const int MaxSelectedInvoiceExportCount = 100;
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;

    public InvoiceExportService(
        AppDbContext context,
        IDateTimeProvider dateTimeProvider,
        IEmailSender emailSender,
        IAuditLogService auditLogService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
    }

    public async Task<ServiceResult<InvoiceExportPreviewDto>> GetPreviewAsync(InvoiceExportQueryDto query, int workshopId)
    {
        var periodResult = ResolvePeriod(query);

        if (!periodResult.IsSuccess)
            return ServiceResult<InvoiceExportPreviewDto>.Fail(periodResult.ErrorMessage);

        var period = periodResult.Data!;

        var invoices = await GetInvoicesAsync(period.StartDate, period.EndDate, query.IncludeCancelled, workshopId);
        var workflow = await GetWorkflowSnapshotAsync(invoices.Select(x => x.Id).ToList(), workshopId);
        var tab = NormalizeWorkflowTab(query.Tab);
        var filteredInvoices = FilterInvoicesByWorkflowTab(invoices, workflow, tab);
        var preview = await BuildPreviewAsync(filteredInvoices, period.StartDate, period.EndDate, query.IncludeCancelled, workshopId, tab, workflow);

        return ServiceResult<InvoiceExportPreviewDto>.Success(preview);
    }

    public async Task<ServiceResult<InvoiceExportFileDto>> CreateZipAsync(InvoiceExportQueryDto query, int workshopId)
    {
        var selectionResult = await ResolveInvoiceSelectionAsync(query, workshopId);

        if (!selectionResult.IsSuccess || selectionResult.Data is null)
            return ServiceResult<InvoiceExportFileDto>.Fail(selectionResult.ErrorMessage);

        var selection = selectionResult.Data;

        if (!selection.Invoices.Any())
            return ServiceResult<InvoiceExportFileDto>.Fail(selection.IsSelectedExport
                ? "Seçilen faturalar muhasebe aktarımı için uygun değil."
                : "Seçilen tarih aralığında aktarılacak fatura bulunamadı.");

        var preview = await BuildPreviewAsync(
            selection.Invoices,
            selection.StartDate,
            selection.EndDate,
            selection.IncludeCancelled,
            workshopId,
            "prepare",
            await GetWorkflowSnapshotAsync(selection.Invoices.Select(x => x.Id).ToList(), workshopId));
        var workshop = await GetWorkshopInfoAsync(workshopId);
        var zipContent = BuildZip(selection.Invoices, preview, workshop);

        return ServiceResult<InvoiceExportFileDto>.Success(new InvoiceExportFileDto
        {
            FileName = BuildExportZipFileName(workshop.DisplayName, selection.StartDate, selection.EndDate),
            ContentType = "application/zip",
            Content = zipContent
        });
    }

    public async Task<ServiceResult<SendInvoiceExportEmailResponseDto>> SendEmailAsync(
        SendInvoiceExportEmailRequestDto request,
        int workshopId,
        int requestedByUserId)
    {
        if (request is null)
            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail("Gönderim bilgisi alınamadı.");

        if (string.IsNullOrWhiteSpace(request.ToEmail))
            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail("Alıcı e-posta adresi zorunludur.");

        try
        {
            _ = new MailAddress(request.ToEmail.Trim());
        }
        catch
        {
            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail("Geçerli bir e-posta adresi giriniz.");
        }

        var selectionResult = await ResolveInvoiceSelectionAsync(request, workshopId);

        if (!selectionResult.IsSuccess || selectionResult.Data is null)
            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail(selectionResult.ErrorMessage ?? "Servis hesap özeti dosyaları oluşturulamadı.");

        var selection = selectionResult.Data;

        if (!selection.Invoices.Any())
            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail(selection.IsSelectedExport
                ? BuildNoSelectedInvoiceMessage(selection)
                : "Servis hesap özeti dosyaları oluşturulamadı.");

        var preview = await BuildPreviewAsync(
            selection.Invoices,
            selection.StartDate,
            selection.EndDate,
            selection.IncludeCancelled,
            workshopId,
            "prepare",
            await GetWorkflowSnapshotAsync(selection.Invoices.Select(x => x.Id).ToList(), workshopId));
        var workshop = await GetWorkshopInfoAsync(workshopId);
        var zipContent = BuildZip(selection.Invoices, preview, workshop);
        var fileName = BuildExportZipFileName(workshop.DisplayName, selection.StartDate, selection.EndDate);
        var periodText = preview.PeriodText;
        var subject = $"{periodText} Servis Hesap Özetleri - {workshop.DisplayName}";
        var htmlBody = BuildEmailBody(workshop.DisplayName, periodText, request.Message);
        var response = new SendInvoiceExportEmailResponseDto
        {
            RequestedCount = selection.RequestedCount,
            SkippedCount = selection.SkippedCount,
            Messages = selection.Messages.ToList()
        };

        var emailResult = await _emailSender.SendAsync(new EmailMessageDto
        {
            ToEmail = request.ToEmail.Trim(),
            FromName = BuildAccountingEmailFromName(workshop.WorkshopName),
            Subject = subject,
            HtmlBody = htmlBody,
            Attachments = new List<EmailAttachmentDto>
            {
                new()
                {
                    FileName = fileName,
                    ContentType = "application/zip",
                    Content = zipContent
                }
            }
        });

        if (!emailResult.IsSuccess)
        {
            response.FailedCount = preview.InvoiceCount;
            response.SummaryMessage = "Servis hesap özeti e-postası gönderilemedi.";

            await WriteExportAuditAsync(
                workshopId,
                requestedByUserId,
                request.ToEmail.Trim(),
                response,
                false);

            return ServiceResult<SendInvoiceExportEmailResponseDto>.Fail(emailResult.ErrorMessage ?? "E-posta gönderilemedi.");
        }

        response.SentCount = preview.InvoiceCount;
        response.SummaryMessage = BuildSendSummaryMessage(response);

        await WriteExportAuditAsync(
            workshopId,
            requestedByUserId,
            request.ToEmail.Trim(),
            response,
            true);

        return ServiceResult<SendInvoiceExportEmailResponseDto>.Success(response);
    }

    private async Task<List<Invoice>> GetInvoicesAsync(
        DateTime startDate,
        DateTime endDate,
        bool includeCancelled,
        int workshopId)
    {
        var endExclusive = endDate.Date.AddDays(1);

        var query = _context.Invoices
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x =>
                x.WorkshopId == workshopId &&
                x.InvoiceDate >= startDate.Date &&
                x.InvoiceDate < endExclusive &&
                (
                    x.Status == InvoiceStatus.Issued ||
                    (includeCancelled && x.Status == InvoiceStatus.Cancelled)
                ));

        return await query
            .OrderBy(x => x.InvoiceDate)
            .ThenBy(x => x.InvoiceNumber)
            .ToListAsync();
    }

    private async Task<ServiceResult<InvoiceExportSelection>> ResolveInvoiceSelectionAsync(
        InvoiceExportQueryDto query,
        int workshopId)
    {
        var periodResult = ResolvePeriod(query);

        if (!periodResult.IsSuccess || periodResult.Data is null)
            return ServiceResult<InvoiceExportSelection>.Fail(periodResult.ErrorMessage);

        var period = periodResult.Data;
        var selectedIds = NormalizeSelectedInvoiceIds(query.InvoiceIds);

        if (!selectedIds.Any())
        {
            var invoices = await GetInvoicesAsync(
                period.StartDate,
                period.EndDate,
                query.IncludeCancelled,
                workshopId);

            return ServiceResult<InvoiceExportSelection>.Success(new InvoiceExportSelection
            {
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IncludeCancelled = query.IncludeCancelled,
                RequestedCount = invoices.Count,
                Invoices = invoices
            });
        }

        if (selectedIds.Count > MaxSelectedInvoiceExportCount)
        {
            return ServiceResult<InvoiceExportSelection>.Fail(
                $"Tek seferde en fazla {MaxSelectedInvoiceExportCount} fatura seçilebilir.");
        }

        var endExclusive = period.EndDate.Date.AddDays(1);

        var foundInvoices = await _context.Invoices
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x =>
                x.WorkshopId == workshopId &&
                selectedIds.Contains(x.Id) &&
                x.InvoiceDate >= period.StartDate.Date &&
                x.InvoiceDate < endExclusive)
            .OrderBy(x => x.InvoiceDate)
            .ThenBy(x => x.InvoiceNumber)
            .ToListAsync();

        var foundIds = foundInvoices.Select(x => x.Id).ToHashSet();
        var inaccessibleCount = selectedIds.Count(x => !foundIds.Contains(x));
        var skippedMessages = new List<string>();

        if (inaccessibleCount > 0)
            skippedMessages.Add($"{inaccessibleCount} kayıt bulunamadı veya erişilemiyor.");

        var eligibleInvoices = foundInvoices
            .Where(x => x.Status == InvoiceStatus.Issued)
            .ToList();

        var ineligibleCount = foundInvoices.Count - eligibleInvoices.Count;

        if (ineligibleCount > 0)
            skippedMessages.Add($"{ineligibleCount} kayıt uygun durumda olmadığı için atlandı.");

        return ServiceResult<InvoiceExportSelection>.Success(new InvoiceExportSelection
        {
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IncludeCancelled = false,
            IsSelectedExport = true,
            RequestedCount = selectedIds.Count,
            SkippedCount = inaccessibleCount + ineligibleCount,
            Messages = skippedMessages,
            Invoices = eligibleInvoices
        });
    }

    private async Task<InvoiceExportPreviewDto> BuildPreviewAsync(
        List<Invoice> invoices,
        DateTime startDate,
        DateTime endDate,
        bool includeCancelled,
        int workshopId,
        string tab,
        Dictionary<int, InvoiceWorkflowInfo> workflow)
    {
        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var paidTotals = invoiceIds.Any()
            ? await GetInvoicePaidTotalsAsync(invoiceIds, workshopId)
            : new Dictionary<int, decimal>();

        var items = invoices.Select(invoice =>
        {
            paidTotals.TryGetValue(invoice.Id, out var paidTotal);
            paidTotal = Math.Max(0m, paidTotal);

            var isCancelled = invoice.Status == InvoiceStatus.Cancelled;
            var remaining = isCancelled
                ? 0m
                : Math.Max(0m, invoice.GrandTotal - paidTotal);

            return new InvoiceExportItemDto
            {
                InvoiceId = invoice.Id,
                ServiceRecordId = invoice.ServiceRecordId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CustomerTitle = invoice.CustomerTitle,
                TaxNumber = ResolveTaxNumber(invoice),
                Plate = invoice.Plate,
                Status = (int)invoice.Status,
                StatusText = GetInvoiceStatusText(invoice.Status),
                Subtotal = invoice.Subtotal,
                VatTotal = invoice.VatTotal,
                GrandTotal = invoice.GrandTotal,
                PaidTotal = isCancelled ? 0m : paidTotal,
                RemainingAmount = remaining,
                AccountingRequestId = workflow.TryGetValue(invoice.Id, out var info) ? info.AccountingRequestId : null,
                BatchToken = workflow.TryGetValue(invoice.Id, out info) ? info.BatchToken : null,
                OfficialInvoiceDocumentId = workflow.TryGetValue(invoice.Id, out info) ? info.OfficialInvoiceDocumentId : null,
                OfficialInvoiceNumber = workflow.TryGetValue(invoice.Id, out info) ? info.OfficialInvoiceNumber : null,
                OfficialInvoiceShareToken = workflow.TryGetValue(invoice.Id, out info) ? info.OfficialInvoiceShareToken : null,
                CustomerDeliveredAt = workflow.TryGetValue(invoice.Id, out info) ? info.CustomerDeliveredAt : null,
                RecipientEmail = workflow.TryGetValue(invoice.Id, out info) ? info.RecipientEmail : null,
                AccountingSentAt = workflow.TryGetValue(invoice.Id, out info) ? info.AccountingSentAt : null
            };
        }).ToList();

        var allWorkflowValues = workflow.Values.ToList();

        return new InvoiceExportPreviewDto
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            PeriodText = BuildPeriodText(startDate, endDate),
            IncludeCancelled = includeCancelled,
            Tab = tab,
            PrepareCount = allWorkflowValues.Count(x => x.Stage == "prepare"),
            WaitingCount = allWorkflowValues.Count(x => x.Stage == "waiting"),
            CustomerShareCount = allWorkflowValues.Count(x => x.Stage == "customer"),
            CompletedCount = allWorkflowValues.Count(x => x.Stage == "completed"),
            InvoiceCount = items.Count,
            IssuedInvoiceCount = items.Count(x => x.Status == (int)InvoiceStatus.Issued),
            CancelledInvoiceCount = items.Count(x => x.Status == (int)InvoiceStatus.Cancelled),
            Subtotal = items.Sum(x => x.Subtotal),
            VatTotal = items.Sum(x => x.VatTotal),
            GrandTotal = items.Sum(x => x.GrandTotal),
            PaidTotal = items.Sum(x => x.PaidTotal),
            RemainingTotal = items.Sum(x => x.RemainingAmount),
            Items = items
        };
    }

    private async Task<Dictionary<int, decimal>> GetInvoicePaidTotalsAsync(List<int> invoiceIds, int workshopId)
    {
        var movements = await _context.CurrentAccountTransactions
            .AsNoTracking()
            .Where(x =>
                x.WorkshopId == workshopId &&
                x.InvoiceId.HasValue &&
                invoiceIds.Contains(x.InvoiceId.Value) &&
                (
                    x.Type == CurrentAccountTransactionType.Payment ||
                    (
                        x.Type == CurrentAccountTransactionType.Cancel &&
                        x.Debit > 0 &&
                        x.DocumentNumber != null &&
                        x.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix)
                    )
                ))
            .Select(x => new
            {
                InvoiceId = x.InvoiceId!.Value,
                x.Type,
                x.Debit,
                x.Credit
            })
            .ToListAsync();

        return movements
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(
                g => g.Key,
                g => Math.Max(0m,
                    g.Where(x => x.Type == CurrentAccountTransactionType.Payment).Sum(x => x.Credit) -
                    g.Where(x => x.Type == CurrentAccountTransactionType.Cancel).Sum(x => x.Debit)));
    }

    private async Task<Dictionary<int, InvoiceWorkflowInfo>> GetWorkflowSnapshotAsync(List<int> invoiceIds, int workshopId)
    {
        if (!invoiceIds.Any())
            return new Dictionary<int, InvoiceWorkflowInfo>();

        var now = _dateTimeProvider.Now;
        var latestRequests = await _context.Set<AccountingInvoiceRequest>()
            .AsNoTracking()
            .Where(x =>
                x.WorkshopId == workshopId &&
                invoiceIds.Contains(x.InvoiceId))
            .GroupBy(x => x.InvoiceId)
            .Select(g => g.OrderByDescending(x => x.SentAt).First())
            .ToListAsync();

        var latestDocuments = await _context.Set<OfficialInvoiceDocument>()
            .AsNoTracking()
            .Where(x =>
                x.WorkshopId == workshopId &&
                invoiceIds.Contains(x.InvoiceId))
            .GroupBy(x => x.InvoiceId)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToListAsync();

        var requestsByInvoice = latestRequests.ToDictionary(x => x.InvoiceId, x => x);
        var documentsByInvoice = latestDocuments.ToDictionary(x => x.InvoiceId, x => x);
        var result = new Dictionary<int, InvoiceWorkflowInfo>();

        foreach (var invoiceId in invoiceIds)
        {
            requestsByInvoice.TryGetValue(invoiceId, out var request);
            documentsByInvoice.TryGetValue(invoiceId, out var document);

            var hasActiveRequest = request is not null &&
                                   request.Status == AccountingInvoiceRequestStatus.Pending &&
                                   request.ExpiresAt > now;
            var hasDocument = document is not null;
            var isDelivered = document?.CustomerDeliveredAt is not null;
            var stage = hasDocument
                ? isDelivered ? "completed" : "customer"
                : hasActiveRequest ? "waiting" : "prepare";

            result[invoiceId] = new InvoiceWorkflowInfo
            {
                Stage = stage,
                AccountingRequestId = request?.Id,
                BatchToken = request?.BatchToken,
                RecipientEmail = request?.AccountantEmail,
                AccountingSentAt = request?.SentAt,
                OfficialInvoiceDocumentId = document?.Id,
                OfficialInvoiceNumber = document?.OfficialInvoiceNumber,
                OfficialInvoiceShareToken = document?.ShareToken,
                CustomerDeliveredAt = document?.CustomerDeliveredAt
            };
        }

        return result;
    }

    private static List<Invoice> FilterInvoicesByWorkflowTab(
        List<Invoice> invoices,
        Dictionary<int, InvoiceWorkflowInfo> workflow,
        string tab)
    {
        return invoices
            .Where(invoice =>
                workflow.TryGetValue(invoice.Id, out var info)
                    ? info.Stage == tab
                    : tab == "prepare")
            .ToList();
    }

    private byte[] BuildZip(List<Invoice> invoices, InvoiceExportPreviewDto preview, WorkshopExportInfo workshop)
    {
        using var memory = new MemoryStream();

        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
        {
            var rootFolder = BuildExportFolderName(preview.StartDate, preview.EndDate);

            AddTextEntry(
                archive,
                $"{rootFolder}/belge-ozeti.csv",
                BuildCsv(preview),
                "text/csv");

            AddTextEntry(
                archive,
                $"{rootFolder}/belge-ozeti.html",
                BuildHtmlSummary(preview, workshop),
                "text/html");

            AddTextEntry(
                archive,
                $"{rootFolder}/aktarim-bilgisi.txt",
                BuildInfoText(preview, workshop),
                "text/plain");

            foreach (var invoice in invoices)
            {
                var item = preview.Items.First(x => x.InvoiceId == invoice.Id);
                var pdf = BuildInvoicePdf(invoice, item, workshop, preview.PeriodText);
                var fileName = BuildInvoicePdfFileName(invoice);

                AddBinaryEntry(
                    archive,
                    $"{rootFolder}/servis-hesap-ozetleri/{fileName}",
                    pdf,
                    "application/pdf");
            }
        }

        return memory.ToArray();
    }

    private static void AddTextEntry(ZipArchive archive, string path, string content, string contentType)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.Write(content);
    }

    private static void AddBinaryEntry(ZipArchive archive, string path, byte[] content, string contentType)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }

    private byte[] BuildInvoicePdf(
        Invoice invoice,
        InvoiceExportItemDto summary,
        WorkshopExportInfo workshop,
        string periodText)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(12);
                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                   .FontSize(9.0f)
                    .SemiBold()
                    .LineHeight(.98f)
                    .FontColor(Colors.Grey.Darken4));

                page.Content().Element(c => BuildInvoiceExportBody(c, invoice, summary, workshop));
                page.Footer().Element(c => BuildInvoiceExportFooter(c, workshop));
            });
        });

        return document.GeneratePdf();
    }

    private static void BuildInvoiceExportBody(
        IContainer container,
        Invoice invoice,
        InvoiceExportItemDto summary,
        WorkshopExportInfo workshop)
    {
        container.Column(column =>
        {
            column.Spacing(4.4f);
            column.Item().Height(.9f).Background(Colors.Grey.Darken4);
            column.Item().Element(c => BuildInvoiceExportHeader(c, invoice, workshop));
            column.Item().Height(.75f).Background(Colors.Grey.Darken4);
            column.Item().Element(c => BuildInvoiceExportPartyAndVehicle(c, invoice));
            column.Item().Height(.75f).Background(Colors.Grey.Darken4);
            column.Item().PaddingTop(1.5f).Element(c => BuildInvoiceExportItemsTable(c, invoice));
            column.Item().ShowEntire().PaddingTop(3.0f).Element(c => BuildInvoiceExportTotalsAndNotes(c, invoice, summary));
        });
    }

    private static void BuildInvoiceExportHeader(IContainer container, Invoice invoice, WorkshopExportInfo workshop)
    {
        container.Row(row =>
        {
            row.RelativeItem(1.45f).Column(left =>
            {
                left.Spacing(1.7f);
                left.Item().Text(GetValue(workshop.LegalTitle, GetValue(workshop.DisplayName, "Servis İşletmesi")).ToUpperInvariant())
                    .FontSize(13.2f)
                    .Bold()
                    .LineHeight(.98f);

                AddExportInfoLine(left, workshop.Address);
                AddExportInfoLine(left, JoinParts(
                    string.IsNullOrWhiteSpace(workshop.PhoneNumber) ? null : $"Tel: {workshop.PhoneNumber}",
                    string.IsNullOrWhiteSpace(workshop.Email) ? null : $"E-Posta: {workshop.Email}"));
                AddExportInfoLine(left, JoinParts(
                    string.IsNullOrWhiteSpace(workshop.TaxOffice) ? null : $"Vergi Dairesi: {workshop.TaxOffice}",
                    string.IsNullOrWhiteSpace(workshop.TaxNumber) ? null : $"VKN/TCKN: {workshop.TaxNumber}"));
                AddExportInfoLine(left, JoinParts(
                    string.IsNullOrWhiteSpace(workshop.TradeRegistryNumber) ? null : $"Ticaret Sicil No: {workshop.TradeRegistryNumber}",
                    string.IsNullOrWhiteSpace(workshop.MersisNumber) ? null : $"MERSİS No: {workshop.MersisNumber}"));
            });

            row.ConstantItem(14);

            row.RelativeItem(.82f).Column(right =>
            {
                right.Spacing(2.4f);
                right.Item().AlignRight().Text("SERVİS HESAP ÖZETİ")
                    .FontSize(18.0f)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                right.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn();
                    });

                    AddExportMetaRow(table, "No", invoice.InvoiceNumber);
                    AddExportMetaRow(table, "Tarih", invoice.InvoiceDate.ToString("dd.MM.yyyy", TrCulture));
                    AddExportMetaRow(table, "Durum", GetInvoiceStatusText(invoice.Status));
                });
            });
        });
    }

    private static void BuildInvoiceExportPartyAndVehicle(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(3.8f).Row(row =>
        {
            row.RelativeItem(1.05f).Column(left =>
            {
                left.Spacing(1.7f);
                AddExportSectionTitle(left, "ALICI");
                left.Item().Text(GetValue(invoice.CustomerTitle).ToUpperInvariant())
                    .FontSize(11.0f)
                    .Bold()
                    .LineHeight(.98f);

                if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                    left.Item().Text(invoice.CustomerAddress.Trim())
                        .FontSize(8.8f)
                        .LineHeight(1.02f)
                        .FontColor(Colors.Grey.Darken3);

                var customerTaxLine = JoinParts(
                    string.IsNullOrWhiteSpace(invoice.CustomerTckn) ? null : $"TCKN: {invoice.CustomerTckn}",
                    string.IsNullOrWhiteSpace(invoice.CustomerTaxNumber) ? null : $"VKN: {invoice.CustomerTaxNumber}",
                    string.IsNullOrWhiteSpace(invoice.CustomerTaxOffice) ? null : $"VD: {invoice.CustomerTaxOffice}");

                if (!string.IsNullOrWhiteSpace(customerTaxLine))
                    left.Item().Text(customerTaxLine).FontSize(9.2f).FontColor(Colors.Grey.Darken3);
            });

            row.ConstantItem(14);

            row.RelativeItem(1.0f).Column(right =>
            {
                right.Spacing(1.4f);
                AddExportSectionTitle(right, "ARAÇ BİLGİLERİ");

                right.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(32);
                        columns.RelativeColumn();
                        columns.ConstantColumn(28);
                        columns.RelativeColumn();
                    });

                    AddExportVehicleRow(table, "Plaka", invoice.Plate, "KM", invoice.Mileage.HasValue ? invoice.Mileage.Value.ToString("N0", TrCulture) : null);
                    AddExportVehicleRow(table, "Şasi", invoice.ChassisNumber, "Yıl", invoice.VehicleModelYear?.ToString());
                    AddExportVehicleFullRow(table, "Araç", JoinParts(invoice.VehicleBrandName, invoice.VehicleModelName));
                });
            });
        });
    }

    private static void BuildInvoiceExportItemsTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(18);
                columns.RelativeColumn(5.3f);
                columns.ConstantColumn(48);
                columns.ConstantColumn(58);
                columns.ConstantColumn(46);
                columns.ConstantColumn(58);
                columns.ConstantColumn(66);
            });

            table.Header(header =>
            {
                ExportTableHeaderCell(header.Cell(), "#", true);
                ExportTableHeaderCell(header.Cell(), "Mal / Hizmet");
                ExportTableHeaderCell(header.Cell(), "Miktar", true);
                ExportTableHeaderCell(header.Cell(), "Birim", true);
                ExportTableHeaderCell(header.Cell(), "İsk.", true);
                ExportTableHeaderCell(header.Cell(), "KDV", true);
                ExportTableHeaderCell(header.Cell(), "Tutar", true);
            });

            if (invoice.Items == null || !invoice.Items.Any())
            {
                ExportTableBodyCell(table.Cell().ColumnSpan(7), "Fatura kalemi bulunmuyor.", false, true, true);
                return;
            }

            var index = 1;
            foreach (var item in invoice.Items.OrderBy(x => x.Id))
            {
                var itemType = GetItemTypeText((int)item.ItemType);
                var description = $"{itemType}: {GetValue(item.Description)}";
                var discountText = item.DiscountAmount > 0
                    ? $"{FormatMoney(item.DiscountAmount)} (%{item.DiscountRate.ToString("N2", TrCulture)})"
                    : "-";
                var vatText = $"{FormatMoney(item.VatAmount)} (%{item.VatRate.ToString("N2", TrCulture)})";

                ExportTableBodyCell(table.Cell(), index.ToString(), true, false, true);
                ExportTableBodyCell(table.Cell(), description, false, false, true);
                ExportTableBodyCell(table.Cell(), $"{item.Quantity.ToString("N2", TrCulture)} {GetValue(item.Unit)}", true);
                ExportTableBodyCell(table.Cell(), FormatMoney(item.UnitPrice), true);
                ExportTableBodyCell(table.Cell(), discountText, true);
                ExportTableBodyCell(table.Cell(), vatText, true);
                ExportTableBodyCell(table.Cell(), FormatMoney(item.LineTotal), true, false, true);

                index++;
            }
        });
    }

    private static void BuildInvoiceExportTotalsAndNotes(IContainer container, Invoice invoice, InvoiceExportItemDto summary)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    left.Item().Border(.45f).BorderColor(Colors.Grey.Darken3).Padding(2.2f).Column(note =>
                    {
                        note.Spacing(.5f);
                        note.Item().Text("Not:").FontSize(9.2f).Bold();
                        note.Item().Text(invoice.Notes.Trim()).FontSize(8.2f).LineHeight(1.0f).FontColor(Colors.Grey.Darken3);
                    });
                }
            });

            row.ConstantItem(14);

            row.ConstantItem(205).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(90);
                });

                AddExportTotalRow(table, "Mal Hizmet Toplamı", invoice.Subtotal);
                AddExportTotalRow(table, "Toplam İskonto", invoice.DiscountTotal);
                AddExportTotalRow(table, "Hesaplanan KDV", invoice.VatTotal);
                AddExportTotalRow(table, "Vergiler Dahil Toplam", invoice.GrandTotal);
                AddExportTotalRow(table, "Tahsil", summary.PaidTotal);
                AddExportTotalRow(table, "Kalan / Ödenecek", summary.RemainingAmount > 0 ? summary.RemainingAmount : 0m, true);
            });
        });
    }

    private static void BuildInvoiceExportFooter(IContainer container, WorkshopExportInfo workshop)
    {
        container.PaddingTop(2).Column(column =>
        {
            column.Spacing(2.4f);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(sig =>
                {
                    sig.Item().Height(.65f).Background(Colors.Grey.Darken4);
                    sig.Item().AlignCenter().PaddingTop(1.2f).Text("Yetkili İmza / Kaşe").FontSize(8.8f).FontColor(Colors.Grey.Darken2).SemiBold();
                });

                row.ConstantItem(90);

                row.RelativeItem().Column(sig =>
                {
                    sig.Item().Height(.65f).Background(Colors.Grey.Darken4);
                    sig.Item().AlignCenter().PaddingTop(1.2f).Text("Müşteri İmza").FontSize(8.8f).FontColor(Colors.Grey.Darken2).SemiBold();
                });
            });

            if (workshop.BankAccounts.Any())
                column.Item().PaddingTop(1.5f).Element(c => BuildInvoiceExportPaymentLine(c, workshop));

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Bu belge servis ödeme takibi ve ön muhasebe amacıyla hazırlanmıştır; e-Arşiv/e-Fatura veya resmi mali belge yerine geçmez.")
                    .FontSize(7.3f)
                    .FontColor(Colors.Grey.Darken2)
                    .LineHeight(1.0f);

                row.ConstantItem(48).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7.3f).FontColor(Colors.Grey.Darken2));
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }

    private static void BuildInvoiceExportPaymentLine(IContainer container, WorkshopExportInfo workshop)
    {
        var accounts = workshop.BankAccounts
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.BankName)
            .Take(3)
            .ToList();

        if (!accounts.Any())
            return;

        container.Border(.45f).BorderColor(Colors.Grey.Darken3).PaddingVertical(1.4f).PaddingHorizontal(3.0f).Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(7.7f).SemiBold().LineHeight(1.02f).FontColor(Colors.Grey.Darken4));
            text.Span("Ödeme: ").Bold();

            for (var i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];

                if (i > 0)
                    text.Span("  |  ");

                text.Span(GetValue(account.BankName)).Bold();
                text.Span(" ");
                text.Span(FormatIban(account.Iban)).Bold();

                if (!string.IsNullOrWhiteSpace(account.CurrencyCode))
                    text.Span($" {account.CurrencyCode}");

                if (!string.IsNullOrWhiteSpace(account.AccountHolder))
                    text.Span($" Alıcı: {account.AccountHolder}");
            }
        });
    }

    private static void AddExportInfoLine(ColumnDescriptor column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        column.Item().Text(value.Trim()).FontSize(8.45f).LineHeight(1.0f).FontColor(Colors.Grey.Darken3);
    }

    private static void AddExportSectionTitle(ColumnDescriptor column, string title)
    {
        column.Item()
            .PaddingBottom(.7f)
            .BorderBottom(.55f)
            .BorderColor(Colors.Grey.Darken4)
            .Text(title)
            .FontSize(9.2f)
            .Bold()
            .FontColor(Colors.Grey.Darken3);
    }

    private static void AddExportMetaRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().Element(ExportLabelCell).Text(label).FontSize(8.45f).Bold().FontColor(Colors.Grey.Darken2);
        table.Cell().Element(ExportValueCell).Text(GetValue(value)).FontSize(8.45f).Bold();
    }

    private static void AddExportVehicleRow(TableDescriptor table, string label1, string? value1, string label2, string? value2)
    {
        table.Cell().Element(ExportLabelCell).Text(label1).FontSize(8.45f).Bold().FontColor(Colors.Grey.Darken2);
        table.Cell().Element(ExportValueCell).Text(GetValue(value1)).FontSize(8.45f).Bold();
        table.Cell().Element(ExportLabelCell).Text(label2).FontSize(8.45f).Bold().FontColor(Colors.Grey.Darken2);
        table.Cell().Element(ExportValueCell).Text(GetValue(value2)).FontSize(8.45f).Bold();
    }

    private static void AddExportVehicleFullRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().Element(ExportLabelCell).Text(label).FontSize(8.45f).Bold().FontColor(Colors.Grey.Darken2);
        table.Cell().ColumnSpan(3).Element(ExportValueCell).Text(GetValue(value)).FontSize(8.45f).Bold();
    }

    private static IContainer ExportLabelCell(IContainer container)
    {
        return container
            .Border(.45f)
            .BorderColor(Colors.Grey.Darken3)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(2.1f)
            .PaddingHorizontal(3.0f);
    }

    private static IContainer ExportValueCell(IContainer container)
    {
        return container
            .Border(.45f)
            .BorderColor(Colors.Grey.Darken3)
            .PaddingVertical(2.1f)
            .PaddingHorizontal(3.0f)
            .AlignRight();
    }

    private static void ExportTableHeaderCell(IContainer container, string text, bool centerOrRight = false)
    {
        var cell = container
            .Border(.55f)
            .BorderColor(Colors.Grey.Darken4)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(2.1f)
            .PaddingHorizontal(3.0f);

        if (centerOrRight)
        {
            cell.AlignCenter().Text(text).FontSize(8.45f).Bold();
            return;
        }

        cell.Text(text).FontSize(8.45f).Bold();
    }

    private static void ExportTableBodyCell(IContainer container, string? text, bool alignRight = false, bool center = false, bool bold = false)
    {
        var cell = container
            .Border(.55f)
            .BorderColor(Colors.Grey.Darken4)
            .PaddingVertical(2.1f)
            .PaddingHorizontal(3.0f);

        var target = center
            ? cell.AlignCenter()
            : alignRight
                ? cell.AlignRight()
                : cell;

        var descriptor = target.Text(GetValue(text)).FontSize(8.45f).LineHeight(1.0f).FontColor(Colors.Grey.Darken4);
        if (bold)
            descriptor.Bold();
    }

    private static void AddExportTotalRow(TableDescriptor table, string label, decimal value, bool grand = false)
    {
        table.Cell()
            .Element(c => c.Border(.55f).BorderColor(Colors.Grey.Darken4).Background(grand ? Colors.Grey.Lighten4 : Colors.White).PaddingVertical(2.1f).PaddingHorizontal(3.0f).AlignRight())
            .Text(label)
           .FontSize(8.45f)
            .Bold();

        table.Cell()
            .Element(c => c.Border(.55f).BorderColor(Colors.Grey.Darken4).Background(grand ? Colors.Grey.Lighten4 : Colors.White).PaddingVertical(2.1f).PaddingHorizontal(3.0f).AlignRight())
            .Text(FormatMoney(value))
           .FontSize(8.45f)
            .Bold();
    }

    private static string GetItemTypeText(int type)
    {
        return type switch
        {
            1 => "İşçilik",
            2 => "Parça",
            3 => "Diğer",
            _ => "Diğer"
        };
    }

    private static string FormatIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return "-";

        var clean = new string(iban.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(clean))
            return "-";

        var groups = Enumerable.Range(0, (clean.Length + 3) / 4)
            .Select(i => clean.Substring(i * 4, Math.Min(4, clean.Length - i * 4)));

        return string.Join(" ", groups);
    }


    private static string BuildCsv(InvoiceExportPreviewDto preview)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Fatura No;Tarih;Müşteri;Vergi/TCKN;Plaka;Durum;Ara Toplam;KDV;Genel Toplam;Tahsil Edilen;Kalan;Servis Kaydı");

        foreach (var item in preview.Items)
        {
            var values = new[]
            {
                item.InvoiceNumber,
                item.InvoiceDate.ToString("dd.MM.yyyy", TrCulture),
                item.CustomerTitle,
                item.TaxNumber,
                item.Plate,
                item.StatusText,
                FormatCsvMoney(item.Subtotal),
                FormatCsvMoney(item.VatTotal),
                FormatCsvMoney(item.GrandTotal),
                FormatCsvMoney(item.PaidTotal),
                FormatCsvMoney(item.RemainingAmount),
                item.ServiceRecordId.HasValue ? item.ServiceRecordId.Value.ToString() : string.Empty
            };

            builder.AppendLine(string.Join(";", values.Select(EscapeCsv)));
        }

        builder.AppendLine();
        builder.AppendLine($"Dönem;{EscapeCsv(preview.PeriodText)}");
        builder.AppendLine($"Fatura Sayısı;{preview.InvoiceCount}");
        builder.AppendLine($"Kesilmiş Fatura;{preview.IssuedInvoiceCount}");
        builder.AppendLine($"İptal Fatura;{preview.CancelledInvoiceCount}");
        builder.AppendLine($"Genel Toplam;{FormatCsvMoney(preview.GrandTotal)}");
        builder.AppendLine($"Tahsil Edilen;{FormatCsvMoney(preview.PaidTotal)}");
        builder.AppendLine($"Kalan;{FormatCsvMoney(preview.RemainingTotal)}");

        return builder.ToString();
    }


    private static string BuildHtmlSummary(InvoiceExportPreviewDto preview, WorkshopExportInfo workshop)
    {
        var builder = new StringBuilder();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"tr\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\" />");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"<title>{WebUtilityHtmlEncode(preview.PeriodText)} Fatura Özeti</title>");
        builder.AppendLine(@"<style>
            body{margin:0;background:#f1f5f9;color:#0f172a;font-family:Arial,Helvetica,sans-serif;}
            .page{max-width:1180px;margin:32px auto;padding:0 18px 42px;}
            .hero{border-radius:24px;background:linear-gradient(135deg,#07111f,#111827 52%,#4f46e5);color:#fff;padding:26px 28px;box-shadow:0 20px 54px rgba(15,23,42,.18);}
            .eyebrow{display:inline-flex;padding:6px 10px;border-radius:999px;background:rgba(255,255,255,.14);font-size:12px;font-weight:900;letter-spacing:.2px;margin-bottom:10px;}
            h1{margin:0;font-size:34px;letter-spacing:-1px;}
            .muted{margin:8px 0 0;color:rgba(255,255,255,.72);font-weight:700;}
            .cards{display:grid;grid-template-columns:repeat(5,1fr);gap:14px;margin:18px 0;}
            .card{background:#fff;border:1px solid #e2e8f0;border-radius:18px;padding:17px;box-shadow:0 10px 28px rgba(15,23,42,.055);}
            .card span{display:block;color:#64748b;font-size:12px;font-weight:900;text-transform:uppercase;letter-spacing:.35px;margin-bottom:7px;}
            .card strong{display:block;font-size:20px;font-weight:950;letter-spacing:-.3px;}
            .panel{background:#fff;border:1px solid #dbe3ef;border-radius:22px;overflow:hidden;box-shadow:0 14px 34px rgba(15,23,42,.06);}
            table{width:100%;border-collapse:collapse;}
            th{background:#f8fafc;color:#64748b;font-size:12px;text-align:left;text-transform:uppercase;letter-spacing:.35px;padding:14px 16px;border-bottom:1px solid #e2e8f0;}
            td{padding:14px 16px;border-bottom:1px solid #edf2f7;font-size:14px;font-weight:750;vertical-align:middle;}
            tr:last-child td{border-bottom:none;}
            .right{text-align:right;}
            .status{display:inline-flex;padding:6px 10px;border-radius:999px;font-size:12px;font-weight:950;}
            .issued{background:#dcfce7;color:#15803d;}
            .cancelled{background:#fee2e2;color:#b91c1c;}
            .footer{margin-top:16px;color:#64748b;font-size:13px;font-weight:700;line-height:1.45;}
            @media(max-width:980px){.cards{grid-template-columns:1fr 1fr}.panel{overflow:auto}h1{font-size:28px}}
        </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body><main class=\"page\">");
        builder.AppendLine("<section class=\"hero\">");
        builder.AppendLine("<div class=\"eyebrow\">SENTE360</div>");
        builder.AppendLine($"<h1>{WebUtilityHtmlEncode(workshop.DisplayName)} - Fatura Özeti</h1>");
        builder.AppendLine($"<p class=\"muted\">Dönem: {WebUtilityHtmlEncode(preview.PeriodText)} · Oluşturulan muhasebe aktarım paketinin tarayıcıdan açılabilen özetidir.</p>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"cards\">");
        AddHtmlSummaryCard(builder, "Fatura", preview.InvoiceCount.ToString());
        AddHtmlSummaryCard(builder, "Kesilmiş", preview.IssuedInvoiceCount.ToString());
        AddHtmlSummaryCard(builder, "Genel Toplam", FormatMoney(preview.GrandTotal));
        AddHtmlSummaryCard(builder, "Tahsil Edilen", FormatMoney(preview.PaidTotal));
        AddHtmlSummaryCard(builder, "Kalan", FormatMoney(preview.RemainingTotal));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\"><table>");
        builder.AppendLine("<thead><tr><th>Fatura No</th><th>Tarih</th><th>Müşteri</th><th>Vergi/TCKN</th><th>Plaka</th><th>Durum</th><th class=\"right\">Genel Toplam</th><th class=\"right\">Tahsil</th><th class=\"right\">Kalan</th></tr></thead>");
        builder.AppendLine("<tbody>");

        foreach (var item in preview.Items)
        {
            var statusClass = item.StatusText == "İptal" ? "cancelled" : "issued";
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{WebUtilityHtmlEncode(item.InvoiceNumber)}</td>");
            builder.AppendLine($"<td>{item.InvoiceDate.ToString("dd.MM.yyyy", TrCulture)}</td>");
            builder.AppendLine($"<td>{WebUtilityHtmlEncode(item.CustomerTitle)}</td>");
            builder.AppendLine($"<td>{WebUtilityHtmlEncode(item.TaxNumber)}</td>");
            builder.AppendLine($"<td>{WebUtilityHtmlEncode(item.Plate)}</td>");
            builder.AppendLine($"<td><span class=\"status {statusClass}\">{WebUtilityHtmlEncode(item.StatusText)}</span></td>");
            builder.AppendLine($"<td class=\"right\">{FormatMoney(item.GrandTotal)}</td>");
            builder.AppendLine($"<td class=\"right\">{FormatMoney(item.PaidTotal)}</td>");
            builder.AppendLine($"<td class=\"right\">{FormatMoney(item.RemainingAmount)}</td>");
            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</tbody></table></section>");
        builder.AppendLine("<p class=\"footer\">CSV dosyası Excel, LibreOffice Calc veya Google Sheets ile açılabilir. Bu HTML özet dosyası ise Excel gerektirmeden doğrudan tarayıcıda görüntülenir.</p>");
        builder.AppendLine("</main></body></html>");

        return builder.ToString();
    }

    private static void AddHtmlSummaryCard(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"<div class=\"card\"><span>{WebUtilityHtmlEncode(label)}</span><strong>{WebUtilityHtmlEncode(value)}</strong></div>");
    }

    private string BuildInfoText(InvoiceExportPreviewDto preview, WorkshopExportInfo workshop)
    {
        var builder = new StringBuilder();

        builder.AppendLine("SENTE360 SERVİS HESAP ÖZETLERİ");
        builder.AppendLine("================================");
        builder.AppendLine($"Servis: {workshop.DisplayName}");
        builder.AppendLine($"Dönem: {preview.PeriodText}");
        builder.AppendLine($"Oluşturma Tarihi: {_dateTimeProvider.Now:dd.MM.yyyy HH:mm}");
        builder.AppendLine($"İptal faturalar dahil: {(preview.IncludeCancelled ? "Evet" : "Hayır")}");
        builder.AppendLine();
        builder.AppendLine($"Fatura Sayısı: {preview.InvoiceCount}");
        builder.AppendLine($"Hazırlanan Özet: {preview.IssuedInvoiceCount}");
        builder.AppendLine($"İptal Edilen Özet: {preview.CancelledInvoiceCount}");
        builder.AppendLine($"Genel Toplam: {FormatMoney(preview.GrandTotal)}");
        builder.AppendLine($"Tahsil Edilen: {FormatMoney(preview.PaidTotal)}");
        builder.AppendLine($"Kalan: {FormatMoney(preview.RemainingTotal)}");
        builder.AppendLine();
        builder.AppendLine("Not: Bu dosya Sente360 içindeki servis hesap özeti kayıtlarından oluşturulmuştur. Fatura entegrasyonu değildir.");

        return builder.ToString();
    }

    private static string BuildEmailBody(string workshopName, string periodText, string? message)
    {
        var safeWorkshopName = WebUtilityHtmlEncode(GetValue(workshopName, "Servis İşletmesi"));
        var safePeriodText = WebUtilityHtmlEncode(periodText);
        var safeMessage = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : $"<p style=\"margin:0 0 14px;color:#334155;line-height:1.55;\">{WebUtilityHtmlEncode(message.Trim())}</p>";

        return $@"
<div style=""font-family:Arial,sans-serif;background:#f8fafc;padding:24px;color:#0f172a;"">
  <div style=""max-width:620px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:18px;padding:24px;"">
    <div style=""font-size:12px;font-weight:800;color:#2563eb;letter-spacing:.2px;margin-bottom:8px;"">SENTE360</div>
    <h2 style=""margin:0 0 8px;font-size:22px;color:#0f172a;"">Servis hesap özetleri</h2>
    <p style=""margin:0 0 16px;color:#64748b;line-height:1.55;"">{safeWorkshopName} işletmesine ait {safePeriodText} servis hesap özeti dosyaları ekte yer almaktadır.</p>
    {safeMessage}
    <div style=""padding:14px 16px;border-radius:14px;background:#f1f5f9;color:#334155;font-size:13px;line-height:1.5;"">
      Ek dosyada servis hesap özeti PDF'leri, özet HTML/CSV dosyaları ve dönem bilgisi bulunur.
    </div>
    <p style=""margin:18px 0 0;color:#94a3b8;font-size:12px;"">Bu e-posta Sente360 üzerinden oluşturulmuştur.</p>
  </div>
</div>";
    }

    private async Task<WorkshopExportInfo> GetWorkshopInfoAsync(int workshopId)
    {
        var profile = await _context.Set<WorkshopProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WorkshopId == workshopId);

        var workshopName = await _context.Workshops
            .AsNoTracking()
            .Where(x => x.Id == workshopId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();

        var displayName = !string.IsNullOrWhiteSpace(profile?.DisplayName)
            ? profile.DisplayName
            : !string.IsNullOrWhiteSpace(profile?.LegalTitle)
                ? profile.LegalTitle
                : workshopName ?? "Servis İşletmesi";

        var addressParts = new[]
        {
            profile?.AddressLine,
            profile?.District,
            profile?.City,
            profile?.PostalCode,
            profile?.Country
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var bankAccounts = await _context.Set<WorkshopBankAccount>()
            .AsNoTracking()
            .Where(x =>
                x.WorkshopId == workshopId &&
                x.IsActive &&
                x.ShowOnInvoices &&
                !string.IsNullOrWhiteSpace(x.Iban))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.BankName)
            .Select(x => new BankAccountExportInfo
            {
                BankName = x.BankName,
                AccountHolder = x.AccountHolder,
                Iban = x.Iban,
                CurrencyCode = x.CurrencyCode,
                Description = x.Description,
                IsDefault = x.IsDefault,
                SortOrder = x.SortOrder
            })
            .ToListAsync();

        return new WorkshopExportInfo
        {
            WorkshopName = workshopName,
            DisplayName = displayName,
            LegalTitle = profile?.LegalTitle,
            TaxOffice = profile?.TaxOffice,
            TaxNumber = profile?.TaxNumber,
            TradeRegistryNumber = profile?.TradeRegistryNumber,
            MersisNumber = profile?.MersisNumber,
            PhoneNumber = profile?.PhoneNumber,
            FaxNumber = profile?.FaxNumber,
            Email = profile?.Email,
            Website = profile?.Website,
            Address = string.Join(" / ", addressParts),
            BankAccounts = bankAccounts
        };
    }

    private ServiceResult<ExportPeriod> ResolvePeriod(InvoiceExportQueryDto? query)
    {
        query ??= new InvoiceExportQueryDto();

        var now = _dateTimeProvider.Now.Date;
        var preset = NormalizeNullable(query.Preset)?.ToLowerInvariant();

        DateTime startDate;
        DateTime endDate;

        if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            startDate = query.StartDate.Value.Date;
            endDate = query.EndDate.Value.Date;
        }
        else
        {
            (startDate, endDate) = preset switch
            {
                "this-week" => GetThisWeek(now),
                "last-week" => GetLastWeek(now),
                "last-month" => GetLastMonth(now),
                "last-30-days" => (now.AddDays(-29), now),
                "today" => (now, now),
                _ => GetThisMonth(now)
            };
        }

        if (endDate < startDate)
            return ServiceResult<ExportPeriod>.Fail("Bitiş tarihi başlangıç tarihinden küçük olamaz.");

        if ((endDate - startDate).TotalDays > MaxExportDayRange)
            return ServiceResult<ExportPeriod>.Fail($"Tek seferde en fazla {MaxExportDayRange} günlük servis hesap özeti alınabilir.");

        return ServiceResult<ExportPeriod>.Success(new ExportPeriod(startDate, endDate));
    }

    private static (DateTime Start, DateTime End) GetThisWeek(DateTime today)
    {
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var start = today.AddDays(-diff);
        return (start, today);
    }

    private static (DateTime Start, DateTime End) GetLastWeek(DateTime today)
    {
        var (thisWeekStart, _) = GetThisWeek(today);
        var start = thisWeekStart.AddDays(-7);
        var end = thisWeekStart.AddDays(-1);
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetThisMonth(DateTime today)
    {
        return (new DateTime(today.Year, today.Month, 1), today);
    }

    private static (DateTime Start, DateTime End) GetLastMonth(DateTime today)
    {
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
        var lastDayLastMonth = firstDayThisMonth.AddDays(-1);
        return (firstDayLastMonth, lastDayLastMonth);
    }

    private static string BuildPeriodText(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date == endDate.Date)
            return startDate.ToString("dd MMMM yyyy", TrCulture);

        return $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
    }

    private static string BuildExportFolderName(DateTime startDate, DateTime endDate)
    {
        return $"Sente360-Fatura-Aktarim-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";
    }

    private static string BuildExportZipFileName(string? workshopName, DateTime startDate, DateTime endDate)
    {
        var safeWorkshop = SafeFilePart(workshopName ?? "Servis");
        return $"{safeWorkshop}-Belgeler-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.zip";
    }

    private static string BuildInvoicePdfFileName(Invoice invoice)
    {
        var date = invoice.InvoiceDate.ToString("yyyy-MM-dd");
        var invoiceNo = SafeFilePart(invoice.InvoiceNumber);
        var customer = SafeFilePart(invoice.CustomerTitle, 42);
        var plate = SafeFilePart(invoice.Plate ?? "Plakasiz", 18);

        return $"{date}_{invoiceNo}_{customer}_{plate}.pdf";
    }

    private static string SafeFilePart(string value, int maxLength = 60)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Belge";

        var normalized = value.Trim();
        normalized = Regex.Replace(normalized, "[^a-zA-Z0-9ığüşöçİĞÜŞÖÇ_-]+", "-");
        normalized = Regex.Replace(normalized, "-+", "-").Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "Belge";

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength].Trim('-');
    }

    private static string ResolveTaxNumber(Invoice invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.CustomerTaxNumber))
            return invoice.CustomerTaxNumber.Trim();

        if (!string.IsNullOrWhiteSpace(invoice.CustomerTckn))
            return invoice.CustomerTckn.Trim();

        return string.Empty;
    }

    private static string GetInvoiceStatusText(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Draft => "Taslak",
            InvoiceStatus.Issued => "Hazırlandı",
            InvoiceStatus.Cancelled => "İptal",
            _ => status.ToString()
        };
    }

    private static string NormalizeWorkflowTab(string? value)
    {
        var tab = NormalizeNullable(value)?.ToLowerInvariant();

        return tab switch
        {
            "waiting" => "waiting",
            "customer" => "customer",
            "completed" => "completed",
            _ => "prepare"
        };
    }

    private static string FormatMoney(decimal value)
    {
        return $"{value.ToString("N2", TrCulture)} TL";
    }

    private static string FormatCsvMoney(decimal value)
    {
        return value.ToString("N2", TrCulture);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();

        if (text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            text = text.Replace("\"", "\"\"");
            return $"\"{text}\"";
        }

        return text;
    }

    private static string JoinParts(params string?[] values)
    {
        return string.Join(" / ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }

    private static string GetValue(string? value, string fallback = "-")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string WebUtilityHtmlEncode(string? value)
    {
        return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static List<int> NormalizeSelectedInvoiceIds(IEnumerable<int>? invoiceIds)
    {
        return (invoiceIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();
    }

    private static string BuildNoSelectedInvoiceMessage(InvoiceExportSelection selection)
    {
        if (selection.Messages.Any())
            return string.Join(" ", selection.Messages);

        return "Seçilen faturalar muhasebe aktarımı için uygun değil.";
    }

    private static string BuildSendSummaryMessage(SendInvoiceExportEmailResponseDto response)
    {
        if (response.SkippedCount > 0)
        {
            var message = $"{response.RequestedCount} faturadan {response.SentCount}'i muhasebeye gönderildi. {response.SkippedCount} kayıt atlandı.";

            if (response.Messages.Any())
                message += " " + string.Join(" ", response.Messages);

            return message;
        }

        return response.SentCount == 1
            ? "1 fatura muhasebeye gönderildi."
            : $"{response.SentCount} fatura muhasebeye gönderildi.";
    }

    private async Task WriteExportAuditAsync(
        int workshopId,
        int requestedByUserId,
        string toEmail,
        SendInvoiceExportEmailResponseDto response,
        bool isSuccess)
    {
        await _auditLogService.AddAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            UserId = requestedByUserId,
            ActionType = AuditActionType.Update,
            EntityType = AuditEntityType.Invoice,
            Description = isSuccess
                ? "Servis hesap özeti dosyaları e-posta ile gönderildi."
                : "Servis hesap özeti dosyaları e-posta ile gönderilemedi.",
            NewValues = new
            {
                ToEmail = MaskEmail(toEmail),
                response.RequestedCount,
                response.SentCount,
                response.SkippedCount,
                response.FailedCount,
                response.Messages
            }
        });
    }

    private static string BuildAccountingEmailFromName(string? workshopName)
    {
        var safeWorkshopName = SanitizeEmailHeaderText(workshopName);

        return string.IsNullOrWhiteSpace(safeWorkshopName)
            ? "Sente360 Muhasebe"
            : $"{safeWorkshopName} | Sente360";
    }

    private static string? SanitizeEmailHeaderText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sanitized = value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Trim();

        sanitized = Regex.Replace(sanitized, @"\s{2,}", " ");

        if (string.IsNullOrWhiteSpace(sanitized))
            return null;

        return sanitized.Length <= 80
            ? sanitized
            : sanitized[..80].Trim();
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];

        var maskedLocal = local.Length <= 2
            ? new string('*', local.Length)
            : $"{local[0]}***{local[^1]}";

        return $"{maskedLocal}@{domain}";
    }

    private sealed class InvoiceExportSelection
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IncludeCancelled { get; set; }

        public bool IsSelectedExport { get; set; }

        public int RequestedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> Messages { get; set; } = new();

        public List<Invoice> Invoices { get; set; } = new();
    }

    private sealed class InvoiceWorkflowInfo
    {
        public string Stage { get; set; } = "prepare";
        public int? AccountingRequestId { get; set; }
        public string? BatchToken { get; set; }
        public int? OfficialInvoiceDocumentId { get; set; }
        public string? OfficialInvoiceNumber { get; set; }
        public string? OfficialInvoiceShareToken { get; set; }
        public DateTime? CustomerDeliveredAt { get; set; }
        public string? RecipientEmail { get; set; }
        public DateTime? AccountingSentAt { get; set; }
    }

    private sealed record ExportPeriod(DateTime StartDate, DateTime EndDate);

    private sealed class WorkshopExportInfo
    {
        public string? WorkshopName { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? LegalTitle { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? TradeRegistryNumber { get; set; }
        public string? MersisNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public List<BankAccountExportInfo> BankAccounts { get; set; } = new();
    }

    private sealed class BankAccountExportInfo
    {
        public string? BankName { get; set; }
        public string? AccountHolder { get; set; }
        public string? Iban { get; set; }
        public string? CurrencyCode { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
    }
}
