using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Accounting;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Dtos.Notifications;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoStock.Services.Services
{
    public class AccountingInvoiceRequestService : IAccountingInvoiceRequestService
    {
        private const int TokenByteLength = 32;
        private const int TokenValidDays = 14;
        private const long MaxOfficialInvoicePdfBytes = 10 * 1024 * 1024;
        private const string OfficialInvoiceUploadsPathConfigKey = "Storage:OfficialInvoiceUploadsPath";

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailSender _emailSender;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountingInvoiceRequestService> _logger;

        public AccountingInvoiceRequestService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IEmailSender emailSender,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<AccountingInvoiceRequestService> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _emailSender = emailSender;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ServiceResult<List<AccountingEmailRecipientDto>>> GetAccountingRecipientsAsync(int workshopId)
        {
            var recipients = await _context.Set<WorkshopEmailRecipient>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.RecipientType == EmailRecipientType.Accounting &&
                    x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.LastUsedAt)
                .ThenBy(x => x.DisplayName)
                .Select(x => new AccountingEmailRecipientDto
                {
                    Id = x.Id,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    IsDefault = x.IsDefault,
                    LastUsedAt = x.LastUsedAt
                })
                .ToListAsync();

            return ServiceResult<List<AccountingEmailRecipientDto>>.Success(recipients);
        }

        public async Task<ServiceResult<AccountingEmailRecipientDto>> SaveAccountingRecipientAsync(
            CreateAccountingEmailRecipientDto request,
            int workshopId)
        {
            if (request is null)
                return ServiceResult<AccountingEmailRecipientDto>.Fail("E-posta bilgisi alınamadı.");

            var normalizedEmailResult = NormalizeEmail(request.Email);

            if (!normalizedEmailResult.IsSuccess)
                return ServiceResult<AccountingEmailRecipientDto>.Fail(normalizedEmailResult.ErrorMessage);

            var email = normalizedEmailResult.Data!;
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? email
                : request.DisplayName.Trim();

            var existing = await _context.Set<WorkshopEmailRecipient>()
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.RecipientType == EmailRecipientType.Accounting &&
                    x.Email == email);

            if (request.IsDefault)
            {
                var defaults = await _context.Set<WorkshopEmailRecipient>()
                    .Where(x =>
                        x.WorkshopId == workshopId &&
                        x.RecipientType == EmailRecipientType.Accounting &&
                        x.IsDefault)
                    .ToListAsync();

                foreach (var item in defaults)
                    item.IsDefault = false;
            }

            if (existing is null)
            {
                existing = new WorkshopEmailRecipient
                {
                    WorkshopId = workshopId,
                    RecipientType = EmailRecipientType.Accounting,
                    DisplayName = displayName,
                    Email = email,
                    IsDefault = request.IsDefault,
                    IsActive = true,
                    CreatedAt = _dateTimeProvider.Now
                };

                _context.Set<WorkshopEmailRecipient>().Add(existing);
            }
            else
            {
                existing.DisplayName = displayName;
                existing.IsDefault = request.IsDefault;
                existing.IsActive = true;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<AccountingEmailRecipientDto>.Success(new AccountingEmailRecipientDto
            {
                Id = existing.Id,
                DisplayName = existing.DisplayName,
                Email = existing.Email,
                IsDefault = existing.IsDefault,
                LastUsedAt = existing.LastUsedAt
            });
        }

        public async Task<ServiceResult<SendAccountingInvoiceRequestResponseDto>> SendAccountingRequestAsync(
            SendAccountingInvoiceRequestDto request,
            int workshopId,
            int requestedByUserId)
        {
            if (request is null)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Gönderim bilgisi alınamadı.");

            if (requestedByUserId <= 0)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Kullanıcı bilgisi bulunamadı.");

            if (request.InvoiceId <= 0)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Geçerli bir hesap özeti seçiniz.");

            if (string.IsNullOrWhiteSpace(request.PublicBaseUrl))
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Fatura yükleme bağlantısı oluşturulamadı.");

            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.InvoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Hesap özeti bulunamadı.");

            if (invoice.Status == InvoiceStatus.Draft)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("Taslak hesap özeti fatura hazırlığına gönderilemez. Önce onaylayınız.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("İptal edilmiş hesap özeti fatura hazırlığına gönderilemez.");

            var recipientEmailsResult = NormalizeRecipients(request);

            if (!recipientEmailsResult.IsSuccess)
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail(recipientEmailsResult.ErrorMessage);

            var recipientEmails = recipientEmailsResult.Data ?? new List<string>();

            if (!recipientEmails.Any())
                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail("En az bir alıcı e-posta adresi seçiniz.");

            if (request.SaveNewRecipient && !string.IsNullOrWhiteSpace(request.NewRecipientEmail))
            {
                await SaveAccountingRecipientAsync(new CreateAccountingEmailRecipientDto
                {
                    Email = request.NewRecipientEmail,
                    DisplayName = request.NewRecipientDisplayName,
                    IsDefault = false
                }, workshopId);
            }

            var now = _dateTimeProvider.Now;
            var workshop = await GetWorkshopInfoAsync(workshopId);
            var accountingEmailFromName = BuildAccountingEmailFromName(workshop.WorkshopName);
            var accountingEmailSubjectPrefix = BuildAccountingEmailSubjectPrefix(workshop.WorkshopName);
            var paidTotal = await GetInvoicePaidTotalAsync(invoice.Id, workshopId);
            var remaining = Math.Max(0m, invoice.GrandTotal - paidTotal);

            var response = new SendAccountingInvoiceRequestResponseDto();
            var emailWorkItems = new List<AccountingInvoiceEmailWorkItem>();
            var existingRequests = await _context.Set<AccountingInvoiceRequest>()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.InvoiceId == invoice.Id &&
                    x.Status == AccountingInvoiceRequestStatus.Pending &&
                    x.ExpiresAt > now &&
                    recipientEmails.Contains(x.AccountantEmail))
                .ToListAsync();

            foreach (var email in recipientEmails)
            {
                var accountingRequest = existingRequests
                    .FirstOrDefault(x => string.Equals(x.AccountantEmail, email, StringComparison.OrdinalIgnoreCase));

                if (accountingRequest is null)
                {
                    accountingRequest = new AccountingInvoiceRequest
                    {
                        WorkshopId = workshopId,
                        InvoiceId = invoice.Id,
                        Token = CreateToken(),
                        AccountantEmail = email,
                        Message = NormalizeNullable(request.Message),
                        RequestedByUserId = requestedByUserId,
                        Status = AccountingInvoiceRequestStatus.Pending,
                        SentAt = now,
                        ExpiresAt = now.AddDays(TokenValidDays),
                        CreatedAt = now
                    };

                    _context.Set<AccountingInvoiceRequest>().Add(accountingRequest);
                }
                else
                {
                    accountingRequest.Message = NormalizeNullable(request.Message);
                }

                var savedRecipient = await _context.Set<WorkshopEmailRecipient>()
                    .FirstOrDefaultAsync(x =>
                        x.WorkshopId == workshopId &&
                        x.RecipientType == EmailRecipientType.Accounting &&
                        x.Email == email &&
                        x.IsActive);

                if (savedRecipient is not null)
                    savedRecipient.LastUsedAt = now;

                var publicLink = BuildPublicLink(request.PublicBaseUrl!, accountingRequest.Token);
                var subject = $"{accountingEmailSubjectPrefix} - Fatura Hazırlık Talebi - {invoice.InvoiceNumber}";
                var htmlBody = BuildAccountingRequestEmailBody(
                    workshop.DisplayName,
                    invoice,
                    paidTotal,
                    remaining,
                    publicLink,
                    request.Message);

                emailWorkItems.Add(new AccountingInvoiceEmailWorkItem(
                    email,
                    invoice.InvoiceNumber,
                    subject,
                    htmlBody,
                    accountingEmailFromName,
                    accountingRequest));
            }

            await _context.SaveChangesAsync();

            var failedEmails = new List<string>();
            var failedEmailErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var successfulWorkItems = new List<AccountingInvoiceEmailWorkItem>();

            foreach (var item in emailWorkItems)
            {
                var emailResult = await _emailSender.SendAsync(new EmailMessageDto
                {
                    ToEmail = item.Email,
                    FromName = item.FromName,
                    Subject = item.Subject,
                    HtmlBody = item.HtmlBody
                });

                if (emailResult.IsSuccess)
                {
                    item.AccountingRequest.SentAt = _dateTimeProvider.Now;
                    response.SentCount++;
                    response.SentEmails.Add(item.Email);
                    successfulWorkItems.Add(item);
                    continue;
                }

                failedEmails.Add(item.Email);
                failedEmailErrors[item.Email] = emailResult.ErrorMessage ?? "E-posta gönderilemedi.";

                _logger.LogWarning(
                    "Accounting invoice request email failed after DB save. InvoiceId: {InvoiceId}, AccountingRequestId: {AccountingRequestId}, Email: {Email}, Error: {ErrorMessage}",
                    item.AccountingRequest.InvoiceId,
                    item.AccountingRequest.Id,
                    item.Email,
                    emailResult.ErrorMessage);
            }

            if (successfulWorkItems.Any())
            {
                try
                {
                    CreateAccountingRequestAuditLogs(successfulWorkItems, requestedByUserId, workshopId, _dateTimeProvider.Now);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Accounting invoice request audit log could not be created. InvoiceId: {InvoiceId}, WorkshopId: {WorkshopId}, RequestedByUserId: {RequestedByUserId}",
                        invoice.Id,
                        workshopId,
                        requestedByUserId);
                }
            }

            if (failedEmails.Any())
            {
                var distinctErrorMessages = failedEmailErrors.Values
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (response.SentCount == 0 && distinctErrorMessages.Count == 1)
                    return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail(distinctErrorMessages[0]);

                var failedEmailText = string.Join(", ", failedEmails.Select(email =>
                    failedEmailErrors.TryGetValue(email, out var error)
                        ? $"{email} ({error})"
                        : email));

                var message = response.SentCount > 0
                    ? $"Fatura hazırlık talebi kaydedildi. Ancak şu adreslere e-posta gönderilemedi: {failedEmailText}."
                    : $"Fatura hazırlık talebi kaydedildi ancak e-posta gönderilemedi: {failedEmailText}.";

                return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Fail(message);
            }

            return ServiceResult<SendAccountingInvoiceRequestResponseDto>.Success(response);
        }

        public async Task<ServiceResult<SendAccountingInvoiceBatchResponseDto>> SendAccountingBatchRequestAsync(
            SendAccountingInvoiceBatchRequestDto request,
            int workshopId,
            int requestedByUserId)
        {
            if (request is null)
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail("Gönderim bilgisi alınamadı.");

            if (requestedByUserId <= 0)
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail("Kullanıcı bilgisi bulunamadı.");

            if (string.IsNullOrWhiteSpace(request.PublicBaseUrl))
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail("Fatura yükleme bağlantısı oluşturulamadı.");

            var emailResult = NormalizeEmail(request.RecipientEmail);

            if (!emailResult.IsSuccess)
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail(emailResult.ErrorMessage);

            var selectedIds = (request.InvoiceIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!selectedIds.Any())
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail("En az bir servis hesap özeti seçiniz.");

            if (selectedIds.Count > 100)
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail("Tek seferde en fazla 100 kayıt gönderilebilir.");

            var now = _dateTimeProvider.Now;
            var invoices = await _context.Invoices
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    selectedIds.Contains(x.Id))
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.InvoiceNumber)
                .ToListAsync();

            var foundIds = invoices.Select(x => x.Id).ToHashSet();
            var missingCount = selectedIds.Count(x => !foundIds.Contains(x));
            var messages = new List<string>();

            if (missingCount > 0)
                messages.Add($"{missingCount} kayıt bulunamadı veya erişilemiyor.");

            var candidateIds = invoices
                .Where(x => x.Status == InvoiceStatus.Issued)
                .Select(x => x.Id)
                .ToList();

            var ineligibleCount = invoices.Count - candidateIds.Count;

            if (ineligibleCount > 0)
                messages.Add($"{ineligibleCount} kayıt uygun durumda olmadığı için atlandı.");

            var alreadyRequestedIds = await _context.Set<AccountingInvoiceRequest>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    candidateIds.Contains(x.InvoiceId) &&
                    x.Status == AccountingInvoiceRequestStatus.Pending &&
                    x.ExpiresAt > now)
                .Select(x => x.InvoiceId)
                .Distinct()
                .ToListAsync();

            if (alreadyRequestedIds.Any())
                messages.Add($"{alreadyRequestedIds.Count} kayıt zaten aktif bir fatura hazırlık gönderiminde.");

            var alreadyDocumentedIds = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    candidateIds.Contains(x.InvoiceId))
                .Select(x => x.InvoiceId)
                .Distinct()
                .ToListAsync();

            if (alreadyDocumentedIds.Any())
                messages.Add($"{alreadyDocumentedIds.Count} kayıt için fatura daha önce yüklenmiş.");

            var eligibleInvoices = invoices
                .Where(x =>
                    candidateIds.Contains(x.Id) &&
                    !alreadyRequestedIds.Contains(x.Id) &&
                    !alreadyDocumentedIds.Contains(x.Id))
                .ToList();

            if (!eligibleInvoices.Any())
            {
                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail(
                    messages.Any()
                        ? string.Join(" ", messages)
                        : "Seçilen kayıtlar fatura hazırlığına göndermek için uygun değil.");
            }

            var workshop = await GetWorkshopInfoAsync(workshopId);
            var batchToken = CreateToken();
            var recipientEmail = emailResult.Data!;
            var expiresAt = now.AddDays(TokenValidDays);
            var requests = eligibleInvoices.Select(invoice => new AccountingInvoiceRequest
            {
                WorkshopId = workshopId,
                InvoiceId = invoice.Id,
                Token = CreateToken(),
                BatchToken = batchToken,
                AccountantEmail = recipientEmail,
                Message = NormalizeNullable(request.Message),
                RequestedByUserId = requestedByUserId,
                Status = AccountingInvoiceRequestStatus.Pending,
                SentAt = now,
                ExpiresAt = expiresAt,
                CreatedAt = now
            }).ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.Set<AccountingInvoiceRequest>().AddRange(requests);

            var savedRecipient = await _context.Set<WorkshopEmailRecipient>()
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.RecipientType == EmailRecipientType.Accounting &&
                    x.Email == recipientEmail &&
                    x.IsActive);

            if (savedRecipient is not null)
                savedRecipient.LastUsedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var uploadUrl = BuildBatchPublicLink(request.PublicBaseUrl!, batchToken);
            var subject = $"{BuildAccountingEmailSubjectPrefix(workshop.WorkshopName)} - {requests.Count} Fatura Hazırlık Talebi";
            var htmlBody = BuildAccountingBatchRequestEmailBody(
                workshop.DisplayName,
                requests.Count,
                uploadUrl,
                request.Message);

            var sendResult = await _emailSender.SendAsync(new EmailMessageDto
            {
                ToEmail = recipientEmail,
                FromName = BuildAccountingEmailFromName(workshop.WorkshopName),
                Subject = subject,
                HtmlBody = htmlBody
            });

            if (!sendResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Accounting invoice batch email failed after DB save. BatchToken: {BatchToken}, WorkshopId: {WorkshopId}, Error: {ErrorMessage}",
                    batchToken,
                    workshopId,
                    sendResult.ErrorMessage);

                return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Fail(
                    sendResult.ErrorMessage ?? "Fatura hazırlık e-postası gönderilemedi.");
            }

            try
            {
                CreateAccountingRequestAuditLogs(
                    requests.Select(x => new AccountingInvoiceEmailWorkItem(
                        recipientEmail,
                        eligibleInvoices.First(i => i.Id == x.InvoiceId).InvoiceNumber,
                        subject,
                        htmlBody,
                        BuildAccountingEmailFromName(workshop.WorkshopName),
                        x)),
                    requestedByUserId,
                    workshopId,
                    _dateTimeProvider.Now);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Accounting invoice batch audit log could not be created. BatchToken: {BatchToken}, WorkshopId: {WorkshopId}",
                    batchToken,
                    workshopId);
            }

            return ServiceResult<SendAccountingInvoiceBatchResponseDto>.Success(new SendAccountingInvoiceBatchResponseDto
            {
                BatchToken = batchToken,
                RecipientEmail = recipientEmail,
                RequestedCount = selectedIds.Count,
                SentCount = requests.Count,
                SkippedCount = selectedIds.Count - requests.Count,
                UploadUrl = uploadUrl,
                Messages = messages
            });
        }

        public async Task<ServiceResult<AccountingInvoiceRequestPublicDto>> GetPublicRequestAsync(string token)
        {
            token = NormalizeToken(token);

            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult<AccountingInvoiceRequestPublicDto>.Fail("Bağlantı geçersiz.");

            var request = await _context.Set<AccountingInvoiceRequest>()
                .AsNoTracking()
                .Include(x => x.Invoice)
                    .ThenInclude(x => x.Items)
                .Include(x => x.Invoice)
                    .ThenInclude(x => x.ServiceRecord)
                        .ThenInclude(x => x!.Vehicle)
                            .ThenInclude(x => x.VehicleVariant)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (request is null)
                return ServiceResult<AccountingInvoiceRequestPublicDto>.Fail("Fatura hazırlık talebi bulunamadı.");

            var invoice = request.Invoice;
            var workshop = await GetWorkshopInfoAsync(request.WorkshopId);
            var officialDocument = await GetLatestOfficialInvoiceDocumentAsync(invoice.Id, request.WorkshopId);
            var paidTotal = await GetInvoicePaidTotalAsync(invoice.Id, request.WorkshopId);
            var remaining = Math.Max(0m, invoice.GrandTotal - paidTotal);
            var now = _dateTimeProvider.Now;
            var isExpired = request.ExpiresAt < now || request.Status == AccountingInvoiceRequestStatus.Expired;
            var canUpload = !isExpired && request.Status != AccountingInvoiceRequestStatus.Cancelled;

            var dto = new AccountingInvoiceRequestPublicDto
            {
                Token = request.Token,
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                StatusText = GetAccountingRequestStatusText(request.Status, isExpired),
                CanUpload = canUpload,
                ExpiresAt = request.ExpiresAt,
                WorkshopName = workshop.DisplayName,
                WorkshopTaxOffice = workshop.TaxOffice,
                WorkshopTaxNumber = workshop.TaxNumber,
                CustomerTitle = invoice.CustomerTitle,
                CustomerTaxOffice = invoice.CustomerTaxOffice,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                CustomerTckn = invoice.CustomerTckn,
                CustomerAddress = invoice.CustomerAddress,
                Plate = invoice.Plate,
                VehicleBrandName = invoice.VehicleBrandName,
                VehicleModelName = invoice.VehicleModelName,
                VehicleVariantName = invoice.ServiceRecord?.Vehicle?.VehicleVariant?.Name,
                VehicleModelYear = invoice.VehicleModelYear ?? invoice.ServiceRecord?.Vehicle?.ModelYear,
                Mileage = invoice.Mileage ?? invoice.ServiceRecord?.MileageSnapshot ?? invoice.ServiceRecord?.Vehicle?.Mileage,
                ChassisNumber = FirstText(invoice.ChassisNumber, invoice.ServiceRecord?.Vehicle?.ChassisNumber),
                VehicleText = JoinText(
                    invoice.VehicleBrandName,
                    invoice.VehicleModelName,
                    invoice.ServiceRecord?.Vehicle?.VehicleVariant?.Name),
                Subtotal = invoice.Subtotal,
                DiscountTotal = invoice.DiscountTotal,
                VatTotal = invoice.VatTotal,
                GrandTotal = invoice.GrandTotal,
                PaidTotal = paidTotal,
                RemainingAmount = remaining,
                OfficialInvoiceDocument = officialDocument,
                Items = invoice.Items
                    .OrderBy(x => x.Id)
                    .Select(x => new AccountingInvoiceRequestItemDto
                    {
                        ItemType = (int)x.ItemType,
                        ItemTypeText = GetInvoiceItemTypeText(x.ItemType),
                        Description = x.Description,
                        Quantity = x.Quantity,
                        Unit = x.Unit,
                        UnitPrice = x.UnitPrice,
                        DiscountRate = x.DiscountRate,
                        DiscountAmount = x.DiscountAmount,
                        VatRate = x.VatRate,
                        VatAmount = x.VatAmount,
                        LineTotal = x.LineTotal
                    })
                    .ToList()
            };

            return ServiceResult<AccountingInvoiceRequestPublicDto>.Success(dto);
        }

        public async Task<ServiceResult<AccountingInvoiceBatchPublicDto>> GetPublicBatchRequestAsync(string batchToken)
        {
            batchToken = NormalizeToken(batchToken);

            if (string.IsNullOrWhiteSpace(batchToken))
                return ServiceResult<AccountingInvoiceBatchPublicDto>.Fail("Bağlantı geçersiz.");

            var requests = await _context.Set<AccountingInvoiceRequest>()
                .AsNoTracking()
                .Include(x => x.Invoice)
                    .ThenInclude(x => x.ServiceRecord)
                        .ThenInclude(x => x!.Vehicle)
                            .ThenInclude(x => x.VehicleVariant)
                .Where(x => x.BatchToken == batchToken)
                .OrderBy(x => x.Invoice.InvoiceDate)
                .ThenBy(x => x.Invoice.InvoiceNumber)
                .ToListAsync();

            if (!requests.Any())
                return ServiceResult<AccountingInvoiceBatchPublicDto>.Fail("Fatura yükleme bağlantısı bulunamadı.");

            var first = requests.First();
            var now = _dateTimeProvider.Now;
            var isExpired = requests.All(x => x.ExpiresAt < now || x.Status == AccountingInvoiceRequestStatus.Expired);
            var canUpload = !isExpired && requests.Any(x => x.Status != AccountingInvoiceRequestStatus.Cancelled);
            var workshop = await GetWorkshopInfoAsync(first.WorkshopId);
            var requestIds = requests.Select(x => x.Id).ToList();
            var documents = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .Where(x => x.AccountingInvoiceRequestId.HasValue && requestIds.Contains(x.AccountingInvoiceRequestId.Value))
                .GroupBy(x => x.AccountingInvoiceRequestId!.Value)
                .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
                .ToDictionaryAsync(x => x.AccountingInvoiceRequestId!.Value, x => x);

            var items = requests.Select(accountingRequest =>
            {
                documents.TryGetValue(accountingRequest.Id, out var document);

                return new AccountingInvoiceBatchItemDto
                {
                    RequestId = accountingRequest.Id,
                    InvoiceId = accountingRequest.InvoiceId,
                    InvoiceNumber = accountingRequest.Invoice.InvoiceNumber,
                    InvoiceDate = accountingRequest.Invoice.InvoiceDate,
                    CustomerTitle = accountingRequest.Invoice.CustomerTitle,
                    Plate = accountingRequest.Invoice.Plate,
                    VehicleBrandName = accountingRequest.Invoice.VehicleBrandName,
                    VehicleModelName = accountingRequest.Invoice.VehicleModelName,
                    VehicleVariantName = accountingRequest.Invoice.ServiceRecord?.Vehicle?.VehicleVariant?.Name,
                    VehicleModelYear = accountingRequest.Invoice.VehicleModelYear
                        ?? accountingRequest.Invoice.ServiceRecord?.Vehicle?.ModelYear,
                    Mileage = accountingRequest.Invoice.Mileage
                        ?? accountingRequest.Invoice.ServiceRecord?.MileageSnapshot
                        ?? accountingRequest.Invoice.ServiceRecord?.Vehicle?.Mileage,
                    ChassisNumber = FirstText(
                        accountingRequest.Invoice.ChassisNumber,
                        accountingRequest.Invoice.ServiceRecord?.Vehicle?.ChassisNumber),
                    VehicleText = JoinText(
                        accountingRequest.Invoice.VehicleBrandName,
                        accountingRequest.Invoice.VehicleModelName,
                        accountingRequest.Invoice.ServiceRecord?.Vehicle?.VehicleVariant?.Name),
                    GrandTotal = accountingRequest.Invoice.GrandTotal,
                    StatusText = document is null ? "Fatura bekliyor" : "Fatura yüklendi",
                    CanUpload = canUpload && accountingRequest.Status != AccountingInvoiceRequestStatus.Cancelled,
                    OfficialInvoiceDocument = document is null ? null : ToDocumentDto(document)
                };
            }).ToList();

            var uploadedCount = items.Count(x => x.OfficialInvoiceDocument is not null);
            var pendingCount = Math.Max(0, items.Count - uploadedCount);

            return ServiceResult<AccountingInvoiceBatchPublicDto>.Success(new AccountingInvoiceBatchPublicDto
            {
                BatchToken = batchToken,
                WorkshopName = workshop.DisplayName,
                RecipientEmail = first.AccountantEmail,
                Message = first.Message,
                SentAt = first.SentAt,
                ExpiresAt = requests.Max(x => x.ExpiresAt),
                CanUpload = canUpload,
                TotalCount = items.Count,
                UploadedCount = uploadedCount,
                PendingCount = pendingCount,
                StatusText = pendingCount == 0 ? "Tamamlandı" : uploadedCount > 0 ? "Kısmen yüklendi" : "Fatura bekleniyor",
                Items = items
            });
        }

        public async Task<ServiceResult<OfficialInvoiceDocumentDto>> UploadOfficialInvoiceAsync(
            string token,
            UploadOfficialInvoiceDto request)
        {
            token = NormalizeToken(token);

            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bağlantı geçersiz.");

            if (request is null)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Yükleme bilgisi alınamadı.");

            if (string.IsNullOrWhiteSpace(request.OfficialInvoiceNumber))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura numarası zorunludur.");

            if (request.OfficialInvoiceDate == default)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura tarihi zorunludur.");

            if (string.IsNullOrWhiteSpace(request.UploadedByEmail))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Yükleyen e-posta adresi zorunludur.");

            var uploadedByResult = NormalizeEmail(request.UploadedByEmail);

            if (!uploadedByResult.IsSuccess)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(uploadedByResult.ErrorMessage);

            if (request.FileContent is null || request.FileSizeBytes <= 0)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("PDF dosyası seçiniz.");

            if (request.FileSizeBytes > MaxOfficialInvoicePdfBytes)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("PDF dosyası en fazla 10 MB olabilir.");

            if (!IsPdfFile(request.FileName, request.ContentType))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Sadece PDF dosyası yükleyebilirsiniz.");

            var accountingRequest = await _context.Set<AccountingInvoiceRequest>()
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (accountingRequest is null)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura hazırlık talebi bulunamadı.");

            if (accountingRequest.ExpiresAt < _dateTimeProvider.Now)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bu bağlantının süresi dolmuş.");

            if (accountingRequest.Status == AccountingInvoiceRequestStatus.Cancelled)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bu talep iptal edilmiş.");

            var now = _dateTimeProvider.Now;
            var hadOfficialInvoiceDocument = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .AnyAsync(x => x.AccountingInvoiceRequestId == accountingRequest.Id);
            var safeOriginalFileName = SanitizeFileName(request.FileName);
            var storedFileName = $"official-invoice-{accountingRequest.InvoiceId}-{Guid.NewGuid():N}.pdf";
            var relativePath = Path.Combine(
                "official-invoices",
                accountingRequest.WorkshopId.ToString(),
                accountingRequest.InvoiceId.ToString(),
                storedFileName);

            var fullPath = BuildOfficialInvoiceFullPath(relativePath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                await using var output = File.Create(fullPath);
                await request.FileContent.CopyToAsync(output);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(
                    ex,
                    "Official invoice upload path is not writable. InvoiceId: {InvoiceId}, WorkshopId: {WorkshopId}, StorageRoot: {StorageRoot}",
                    accountingRequest.InvoiceId,
                    accountingRequest.WorkshopId,
                    GetUploadRootPath());

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura PDF dosyası sunucuya kaydedilemedi. Lütfen sistem yöneticisiyle iletişime geçin.");
            }
            catch (IOException ex)
            {
                _logger.LogError(
                    ex,
                    "Official invoice file write failed. InvoiceId: {InvoiceId}, WorkshopId: {WorkshopId}",
                    accountingRequest.InvoiceId,
                    accountingRequest.WorkshopId);

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura PDF dosyası şu anda kaydedilemedi. Lütfen tekrar deneyin.");
            }

            var document = new OfficialInvoiceDocument
            {
                WorkshopId = accountingRequest.WorkshopId,
                InvoiceId = accountingRequest.InvoiceId,
                AccountingInvoiceRequestId = accountingRequest.Id,
                OfficialInvoiceNumber = request.OfficialInvoiceNumber.Trim(),
                OfficialInvoiceDate = request.OfficialInvoiceDate.Date,
                EttnOrUuid = NormalizeNullable(request.EttnOrUuid),
                OriginalFileName = safeOriginalFileName,
                StoredFileName = storedFileName,
                RelativePath = relativePath.Replace('\\', '/'),
                ContentType = "application/pdf",
                FileSizeBytes = request.FileSizeBytes,
                UploadedAt = now,
                UploadedByEmail = uploadedByResult.Data!,
                Note = NormalizeNullable(request.Note),
                ShareToken = CreateToken()
            };

            _context.Set<OfficialInvoiceDocument>().Add(document);

            accountingRequest.Status = AccountingInvoiceRequestStatus.Uploaded;
            accountingRequest.CompletedAt = now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TryDeleteFile(fullPath);

                _logger.LogError(
                    ex,
                    "Official invoice document DB save failed after file write. FilePath: {FilePath}, InvoiceId: {InvoiceId}, WorkshopId: {WorkshopId}",
                    fullPath,
                    accountingRequest.InvoiceId,
                    accountingRequest.WorkshopId);

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura dosyası yüklendi ancak kayıt bilgileri kaydedilemedi. Lütfen tekrar deneyin.");
            }

            await NotifyWorkshopOfficialInvoiceUploadedAsync(
                accountingRequest,
                document,
                hadOfficialInvoiceDocument);

            return ServiceResult<OfficialInvoiceDocumentDto>.Success(ToDocumentDto(document));
        }

        public async Task<ServiceResult<OfficialInvoiceDocumentDto>> UploadOfficialInvoiceForBatchItemAsync(
            string batchToken,
            int accountingRequestId,
            UploadOfficialInvoiceDto request)
        {
            batchToken = NormalizeToken(batchToken);

            if (string.IsNullOrWhiteSpace(batchToken))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bağlantı geçersiz.");

            if (accountingRequestId <= 0)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Kayıt bilgisi alınamadı.");

            if (request is null)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Yükleme bilgisi alınamadı.");

            if (string.IsNullOrWhiteSpace(request.OfficialInvoiceNumber))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura numarası zorunludur.");

            if (request.OfficialInvoiceDate == default)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura tarihi zorunludur.");

            if (string.IsNullOrWhiteSpace(request.UploadedByEmail))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Yükleyen e-posta adresi zorunludur.");

            var uploadedByResult = NormalizeEmail(request.UploadedByEmail);

            if (!uploadedByResult.IsSuccess)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(uploadedByResult.ErrorMessage);

            if (request.FileContent is null || request.FileSizeBytes <= 0)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("PDF dosyası seçiniz.");

            if (request.FileSizeBytes > MaxOfficialInvoicePdfBytes)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("PDF dosyası en fazla 10 MB olabilir.");

            if (!IsPdfFile(request.FileName, request.ContentType))
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Sadece PDF dosyası yükleyebilirsiniz.");

            var accountingRequest = await _context.Set<AccountingInvoiceRequest>()
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x =>
                    x.Id == accountingRequestId &&
                    x.BatchToken == batchToken);

            if (accountingRequest is null)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura hazırlık kaydı bulunamadı.");

            if (accountingRequest.ExpiresAt < _dateTimeProvider.Now)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bu bağlantının süresi dolmuş.");

            if (accountingRequest.Status == AccountingInvoiceRequestStatus.Cancelled)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Bu talep iptal edilmiş.");

            var now = _dateTimeProvider.Now;
            var safeOriginalFileName = SanitizeFileName(request.FileName);
            var storedFileName = $"official-invoice-{accountingRequest.InvoiceId}-{Guid.NewGuid():N}.pdf";
            var relativePath = Path.Combine(
                "official-invoices",
                accountingRequest.WorkshopId.ToString(),
                accountingRequest.InvoiceId.ToString(),
                storedFileName);

            var fullPath = BuildOfficialInvoiceFullPath(relativePath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                await using var output = File.Create(fullPath);
                await request.FileContent.CopyToAsync(output);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(
                    ex,
                    "Batch official invoice upload path is not writable. BatchToken: {BatchToken}, AccountingRequestId: {AccountingRequestId}, StorageRoot: {StorageRoot}",
                    batchToken,
                    accountingRequestId,
                    GetUploadRootPath());

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura PDF dosyası sunucuya kaydedilemedi. Lütfen sistem yöneticisiyle iletişime geçin.");
            }
            catch (IOException ex)
            {
                _logger.LogError(
                    ex,
                    "Batch official invoice file write failed. BatchToken: {BatchToken}, AccountingRequestId: {AccountingRequestId}",
                    batchToken,
                    accountingRequestId);

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura PDF dosyası şu anda kaydedilemedi. Lütfen tekrar deneyin.");
            }

            var document = new OfficialInvoiceDocument
            {
                WorkshopId = accountingRequest.WorkshopId,
                InvoiceId = accountingRequest.InvoiceId,
                AccountingInvoiceRequestId = accountingRequest.Id,
                OfficialInvoiceNumber = request.OfficialInvoiceNumber.Trim(),
                OfficialInvoiceDate = request.OfficialInvoiceDate.Date,
                EttnOrUuid = NormalizeNullable(request.EttnOrUuid),
                OriginalFileName = safeOriginalFileName,
                StoredFileName = storedFileName,
                RelativePath = relativePath.Replace('\\', '/'),
                ContentType = "application/pdf",
                FileSizeBytes = request.FileSizeBytes,
                UploadedAt = now,
                UploadedByEmail = uploadedByResult.Data!,
                Note = NormalizeNullable(request.Note),
                ShareToken = CreateToken()
            };

            _context.Set<OfficialInvoiceDocument>().Add(document);

            accountingRequest.Status = AccountingInvoiceRequestStatus.Uploaded;
            accountingRequest.CompletedAt = now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TryDeleteFile(fullPath);

                _logger.LogError(
                    ex,
                    "Batch official invoice document DB save failed after file write. BatchToken: {BatchToken}, AccountingRequestId: {AccountingRequestId}",
                    batchToken,
                    accountingRequestId);

                return ServiceResult<OfficialInvoiceDocumentDto>.Fail(
                    "Fatura dosyası yüklendi ancak kayıt bilgileri kaydedilemedi. Lütfen tekrar deneyin.");
            }

            return ServiceResult<OfficialInvoiceDocumentDto>.Success(ToDocumentDto(document));
        }

        public async Task<ServiceResult<CompleteAccountingInvoiceBatchUploadResponseDto>> CompleteBatchUploadAsync(string batchToken)
        {
            batchToken = NormalizeToken(batchToken);

            if (string.IsNullOrWhiteSpace(batchToken))
                return ServiceResult<CompleteAccountingInvoiceBatchUploadResponseDto>.Fail("Bağlantı geçersiz.");

            var requests = await _context.Set<AccountingInvoiceRequest>()
                .Include(x => x.Invoice)
                .Where(x => x.BatchToken == batchToken)
                .ToListAsync();

            if (!requests.Any())
                return ServiceResult<CompleteAccountingInvoiceBatchUploadResponseDto>.Fail("Fatura yükleme bağlantısı bulunamadı.");

            var requestIds = requests.Select(x => x.Id).ToList();
            var uploadedRequestIds = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .Where(x => x.AccountingInvoiceRequestId.HasValue && requestIds.Contains(x.AccountingInvoiceRequestId.Value))
                .Select(x => x.AccountingInvoiceRequestId!.Value)
                .Distinct()
                .ToListAsync();

            var now = _dateTimeProvider.Now;
            var uploadedCount = uploadedRequestIds.Count;
            var totalCount = requests.Count;
            var pendingCount = Math.Max(0, totalCount - uploadedCount);

            var shouldNotify = requests.Any(x =>
                uploadedRequestIds.Contains(x.Id) &&
                x.BatchCompletedAt is null);

            foreach (var accountingRequest in requests.Where(x => uploadedRequestIds.Contains(x.Id)))
            {
                accountingRequest.BatchCompletedAt = now;
            }

            await _context.SaveChangesAsync();

            if (shouldNotify)
                await NotifyWorkshopBatchOfficialInvoicesUploadedAsync(requests, uploadedCount, pendingCount);

            var message = pendingCount == 0
                ? $"{uploadedCount} fatura yüklendi."
                : $"{totalCount} faturadan {uploadedCount}'ü yüklendi. {pendingCount} fatura bekliyor.";

            return ServiceResult<CompleteAccountingInvoiceBatchUploadResponseDto>.Success(new CompleteAccountingInvoiceBatchUploadResponseDto
            {
                TotalCount = totalCount,
                UploadedCount = uploadedCount,
                PendingCount = pendingCount,
                Message = message
            });
        }

        public async Task<ServiceResult<InvoiceAccountingStatusDto>> GetInvoiceAccountingStatusAsync(
            int invoiceId,
            int workshopId)
        {
            var invoiceExists = await _context.Invoices
                .AsNoTracking()
                .AnyAsync(x => x.Id == invoiceId && x.WorkshopId == workshopId);

            if (!invoiceExists)
                return ServiceResult<InvoiceAccountingStatusDto>.Fail("Hesap özeti bulunamadı.");

            var latestRequest = await _context.Set<AccountingInvoiceRequest>()
                .AsNoTracking()
                .Where(x => x.InvoiceId == invoiceId && x.WorkshopId == workshopId)
                .OrderByDescending(x => x.SentAt)
                .FirstOrDefaultAsync();

            var latestOfficialDocument = await GetLatestOfficialInvoiceDocumentAsync(invoiceId, workshopId);
            var hasPendingRequest = latestRequest is not null &&
                                    latestRequest.Status == AccountingInvoiceRequestStatus.Pending &&
                                    latestRequest.ExpiresAt >= _dateTimeProvider.Now;

            var statusText = latestOfficialDocument is not null
                ? "Fatura yüklendi"
                : hasPendingRequest
                    ? "Fatura hazırlığına gönderildi"
                    : "Fatura bekleniyor";

            var dto = new InvoiceAccountingStatusDto
            {
                InvoiceId = invoiceId,
                HasPendingRequest = hasPendingRequest,
                HasOfficialInvoice = latestOfficialDocument is not null,
                StatusText = statusText,
                LastSentAt = latestRequest?.SentAt,
                LastSentToEmail = latestRequest?.AccountantEmail,
                LatestOfficialInvoice = latestOfficialDocument
            };

            return ServiceResult<InvoiceAccountingStatusDto>.Success(dto);
        }

        public async Task<ServiceResult<OfficialInvoiceFileDto>> GetOfficialInvoiceFileAsync(
            int officialInvoiceDocumentId,
            int workshopId)
        {
            var document = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == officialInvoiceDocumentId &&
                    x.WorkshopId == workshopId);

            if (document is null)   
                return ServiceResult<OfficialInvoiceFileDto>.Fail("Fatura dosyası bulunamadı.");

            var fullPath = BuildOfficialInvoiceFullPath(document.RelativePath);

            if (!File.Exists(fullPath))
                return ServiceResult<OfficialInvoiceFileDto>.Fail("Fatura dosyası sunucuda bulunamadı.");

            var content = await File.ReadAllBytesAsync(fullPath);

            return ServiceResult<OfficialInvoiceFileDto>.Success(new OfficialInvoiceFileDto
            {
                FileName = document.OriginalFileName,
                ContentType = document.ContentType,
                Content = content
            });
        }

        public async Task<ServiceResult<OfficialInvoiceFileDto>> GetOfficialInvoiceFileByShareTokenAsync(string shareToken)
        {
            shareToken = NormalizeToken(shareToken);

            if (string.IsNullOrWhiteSpace(shareToken))
                return ServiceResult<OfficialInvoiceFileDto>.Fail("Bağlantı geçersiz.");

            var document = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .Include(x => x.Workshop)
                .FirstOrDefaultAsync(x =>
                    x.ShareToken == shareToken &&
                    x.Workshop.IsActive);

            if (document is null)
                return ServiceResult<OfficialInvoiceFileDto>.Fail("Fatura dosyası bulunamadı.");

            var fullPath = BuildOfficialInvoiceFullPath(document.RelativePath);

            if (!File.Exists(fullPath))
                return ServiceResult<OfficialInvoiceFileDto>.Fail("Fatura dosyası sunucuda bulunamadı.");

            var content = await File.ReadAllBytesAsync(fullPath);

            return ServiceResult<OfficialInvoiceFileDto>.Success(new OfficialInvoiceFileDto
            {
                FileName = document.OriginalFileName,
                ContentType = document.ContentType,
                Content = content
            });
        }

        public async Task<ServiceResult<OfficialInvoiceDocumentDto>> MarkOfficialInvoiceDeliveredAsync(
            int officialInvoiceDocumentId,
            int workshopId,
            int userId,
            MarkOfficialInvoiceDeliveredDto request)
        {
            if (officialInvoiceDocumentId <= 0)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura bilgisi alınamadı.");

            var document = await _context.Set<OfficialInvoiceDocument>()
                .FirstOrDefaultAsync(x =>
                    x.Id == officialInvoiceDocumentId &&
                    x.WorkshopId == workshopId);

            if (document is null)
                return ServiceResult<OfficialInvoiceDocumentDto>.Fail("Fatura dosyası bulunamadı.");

            var channel = NormalizeDeliveryChannel(request?.Channel);

            document.CustomerDeliveredAt = _dateTimeProvider.Now;
            document.CustomerDeliveredByUserId = userId;
            document.CustomerDeliveryChannel = channel;

            _context.AuditLogs.Add(new AuditLog
            {
                WorkshopId = workshopId,
                UserId = userId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.AccountingInvoiceRequest,
                EntityId = document.AccountingInvoiceRequestId,
                Description = $"Fatura müşteriye iletildi olarak işaretlendi. Kanal: {channel}",
                NewValuesJson = $$"""
                {"officialInvoiceDocumentId":{{document.Id}},"channel":"{{JsonEscape(channel)}}"}
                """,
                CreatedAt = _dateTimeProvider.Now
            });

            await _context.SaveChangesAsync();

            return ServiceResult<OfficialInvoiceDocumentDto>.Success(ToDocumentDto(document));
        }

        private async Task<OfficialInvoiceDocumentDto?> GetLatestOfficialInvoiceDocumentAsync(int invoiceId, int workshopId)
        {
            var document = await _context.Set<OfficialInvoiceDocument>()
                .AsNoTracking()
                .Where(x => x.InvoiceId == invoiceId && x.WorkshopId == workshopId)
                .OrderByDescending(x => x.UploadedAt)
                .FirstOrDefaultAsync();

            return document is null ? null : ToDocumentDto(document);
        }

        private static OfficialInvoiceDocumentDto ToDocumentDto(OfficialInvoiceDocument document)
        {
            return new OfficialInvoiceDocumentDto
            {
                Id = document.Id,
                InvoiceId = document.InvoiceId,
                OfficialInvoiceNumber = document.OfficialInvoiceNumber,
                OfficialInvoiceDate = document.OfficialInvoiceDate,
                EttnOrUuid = document.EttnOrUuid,
                OriginalFileName = document.OriginalFileName,
                FileSizeBytes = document.FileSizeBytes,
                UploadedAt = document.UploadedAt,
                UploadedByEmail = document.UploadedByEmail,
                Note = document.Note,
                ShareToken = document.ShareToken,
                CustomerDeliveredAt = document.CustomerDeliveredAt,
                CustomerDeliveryChannel = document.CustomerDeliveryChannel
            };
        }

        private async Task<decimal> GetInvoicePaidTotalAsync(int invoiceId, int workshopId)
        {
            var movements = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.InvoiceId == invoiceId &&
                    x.Type != CurrentAccountTransactionType.InvoiceDebit &&
                    x.Type != CurrentAccountTransactionType.Cancel)
                .Select(x => new
                {
                    x.Debit,
                    x.Credit
                })
                .ToListAsync();

            return Math.Max(0m, movements.Sum(x => x.Credit - x.Debit));
        }

        private async Task<WorkshopInfo> GetWorkshopInfoAsync(int workshopId)
        {
            var profile = await _context.Set<WorkshopProfile>()
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .Select(x => new
                {
                    x.DisplayName,
                    x.LegalTitle,
                    x.Email,
                    x.TaxOffice,
                    x.TaxNumber
                })
                .FirstOrDefaultAsync();

            var workshop = await _context.Workshops
                .AsNoTracking()
                .Where(x => x.Id == workshopId)
                .Select(x => new
                {
                    x.Name
                })
                .FirstOrDefaultAsync();

            var displayName = !string.IsNullOrWhiteSpace(profile?.LegalTitle)
                ? profile!.LegalTitle!
                : !string.IsNullOrWhiteSpace(profile?.DisplayName)
                    ? profile!.DisplayName!
                    : workshop?.Name ?? "Servis İşletmesi";

            return new WorkshopInfo
            {
                WorkshopName = workshop?.Name,
                DisplayName = displayName,
                NotificationEmail = profile?.Email,
                TaxOffice = profile?.TaxOffice,
                TaxNumber = profile?.TaxNumber
            };
        }

        private async Task NotifyWorkshopOfficialInvoiceUploadedAsync(
            AccountingInvoiceRequest accountingRequest,
            OfficialInvoiceDocument document,
            bool isReplacement)
        {
            try
            {
                var workshop = await GetWorkshopInfoAsync(accountingRequest.WorkshopId);

                if (!string.IsNullOrWhiteSpace(workshop.NotificationEmail))
                {
                    var subject = $"Sente360 - Fatura yüklendi - {accountingRequest.Invoice.InvoiceNumber}";
                    var htmlBody = $@"
<div style='font-family:Arial,sans-serif;font-size:14px;color:#0f172a;line-height:1.55'>
  <h2 style='margin:0 0 12px'>Fatura hazır</h2>
  <p>{HtmlEncode(accountingRequest.AccountantEmail)} tarafından <strong>{HtmlEncode(accountingRequest.Invoice.InvoiceNumber)}</strong> hesap özeti için fatura PDF'i yüklendi.</p>
  <p><strong>Fatura No:</strong> {HtmlEncode(document.OfficialInvoiceNumber)}<br />
  <strong>Fatura Tarihi:</strong> {document.OfficialInvoiceDate:dd.MM.yyyy}</p>
  <p>Sente360 panelinden ilgili hesap özetine girerek PDF'i indirebilir ve müşteriye iletebilirsiniz.</p>
</div>";

                    var emailResult = await _emailSender.SendAsync(new EmailMessageDto
                    {
                        ToEmail = workshop.NotificationEmail,
                        Subject = subject,
                        HtmlBody = htmlBody
                    });

                    if (!emailResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Official invoice upload email notification failed. AccountingRequestId: {AccountingRequestId}, InvoiceId: {InvoiceId}, Error: {ErrorMessage}",
                            accountingRequest.Id,
                            accountingRequest.InvoiceId,
                            emailResult.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Official invoice upload email notification threw after successful upload. AccountingRequestId: {AccountingRequestId}, InvoiceId: {InvoiceId}",
                    accountingRequest.Id,
                    accountingRequest.InvoiceId);
            }

            try
            {
                await CreateOfficialInvoiceUploadNotificationAsync(accountingRequest, document, isReplacement);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Official invoice upload app notification failed after successful upload. AccountingRequestId: {AccountingRequestId}, InvoiceId: {InvoiceId}",
                    accountingRequest.Id,
                    accountingRequest.InvoiceId);
            }
        }

        private async Task NotifyWorkshopBatchOfficialInvoicesUploadedAsync(
            List<AccountingInvoiceRequest> requests,
            int uploadedCount,
            int pendingCount)
        {
            try
            {
                var first = requests.FirstOrDefault();

                if (first is null)
                    return;

                var requesterUserIds = requests
                    .Where(x => x.RequestedByUserId.HasValue && x.RequestedByUserId.Value > 0)
                    .Select(x => x.RequestedByUserId!.Value)
                    .Distinct()
                    .ToList();

                if (!requesterUserIds.Any())
                {
                    var requestIds = requests.Select(x => x.Id).ToList();
                    requesterUserIds = await _context.AuditLogs
                        .AsNoTracking()
                        .Where(x =>
                            x.EntityType == AuditEntityType.AccountingInvoiceRequest &&
                            x.EntityId.HasValue &&
                            requestIds.Contains(x.EntityId.Value) &&
                            x.ActionType == AuditActionType.Create &&
                            x.UserId.HasValue &&
                            x.UserId.Value > 0)
                        .Select(x => x.UserId!.Value)
                        .Distinct()
                        .ToListAsync();
                }

                var totalCount = requests.Count;
                var message = pendingCount == 0
                    ? $"{uploadedCount} fatura yüklendi."
                    : $"{totalCount} faturadan {uploadedCount}'ü yüklendi. {pendingCount} fatura bekliyor.";

                var notificationResult = await _notificationService.CreateForWorkshopOwnersAndUsersAsync(
                    first.WorkshopId,
                    requesterUserIds,
                    new CreateNotificationDto
                    {
                        WorkshopId = first.WorkshopId,
                        Type = NotificationType.InvoiceDocumentUploaded,
                        Title = "Faturalar yüklendi",
                        Message = message,
                        RelatedEntityType = NotificationRelatedEntityType.AccountingInvoiceRequest,
                        RelatedEntityId = first.Id,
                        ActionUrl = "/Invoices/Export?Tab=customer"
                    });

                if (notificationResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Accounting invoice batch upload notification returned failure. BatchToken: {BatchToken}, Error: {ErrorMessage}",
                        first.BatchToken,
                        notificationResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Accounting invoice batch upload notification failed.");
            }
        }

        private void CreateAccountingRequestAuditLogs(
            IEnumerable<AccountingInvoiceEmailWorkItem> emailWorkItems,
            int requestedByUserId,
            int workshopId,
            DateTime createdAt)
        {
            foreach (var item in emailWorkItems)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    WorkshopId = workshopId,
                    UserId = requestedByUserId,
                    ActionType = AuditActionType.Create,
                    EntityType = AuditEntityType.AccountingInvoiceRequest,
                    EntityId = item.AccountingRequest.Id,
                    Description = $"Fatura hazırlık talebi gönderildi. Belge: {item.InvoiceNumber}, Alıcı: {item.Email}",
                    NewValuesJson = $$"""
                    {"invoiceId":{{item.AccountingRequest.InvoiceId}},"accountantEmail":"{{JsonEscape(item.Email)}}"}
                    """,
                    CreatedAt = createdAt
                });
            }
        }

        private async Task CreateOfficialInvoiceUploadNotificationAsync(
            AccountingInvoiceRequest accountingRequest,
            OfficialInvoiceDocument document,
            bool isReplacement)
        {
            var requesterUserIds = await GetAccountingRequestRequesterUserIdsAsync(accountingRequest.Id);
            var title = isReplacement
                ? "Fatura PDF'i güncellendi"
                : "Fatura PDF'i yüklendi";
            var actionText = isReplacement
                ? "güncellendi"
                : "yüklendi";
            var invoiceText = string.IsNullOrWhiteSpace(document.OfficialInvoiceNumber)
                ? accountingRequest.Invoice.InvoiceNumber
                : document.OfficialInvoiceNumber;

            var notificationResult = await _notificationService.CreateForWorkshopOwnersAndUsersAsync(
                accountingRequest.WorkshopId,
                requesterUserIds,
                new CreateNotificationDto
                {
                    WorkshopId = accountingRequest.WorkshopId,
                    Type = isReplacement
                        ? NotificationType.InvoiceDocumentReuploaded
                        : NotificationType.InvoiceDocumentUploaded,
                    Title = title,
                    Message = $"{invoiceText} için fatura PDF'i sisteme {actionText}.",
                    RelatedEntityType = NotificationRelatedEntityType.AccountingInvoiceRequest,
                    RelatedEntityId = accountingRequest.Id,
                    ActionUrl = $"/Invoices/Detail/{accountingRequest.InvoiceId}"
                });

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Official invoice upload app notification returned failure. AccountingRequestId: {AccountingRequestId}, InvoiceId: {InvoiceId}, Error: {ErrorMessage}",
                    accountingRequest.Id,
                    accountingRequest.InvoiceId,
                    notificationResult.ErrorMessage);
            }
        }

        private async Task<List<int>> GetAccountingRequestRequesterUserIdsAsync(int accountingRequestId)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(x =>
                    x.EntityType == AuditEntityType.AccountingInvoiceRequest &&
                    x.EntityId == accountingRequestId &&
                    x.ActionType == AuditActionType.Create &&
                    x.UserId.HasValue &&
                    x.UserId.Value > 0)
                .Select(x => x.UserId!.Value)
                .Distinct()
                .ToListAsync();
        }

        private static string BuildAccountingRequestEmailBody(
            string workshopName,
            Invoice invoice,
            decimal paidTotal,
            decimal remaining,
            string publicLink,
            string? customMessage)
        {
            var noteHtml = string.IsNullOrWhiteSpace(customMessage)
                ? string.Empty
                : $"<p style='padding:12px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px'><strong>Servis notu:</strong><br>{HtmlEncode(customMessage)}</p>";

            return $@"
<div style='font-family:Arial,sans-serif;font-size:14px;color:#0f172a;line-height:1.55'>
  <h2 style='margin:0 0 12px'>Fatura hazırlık talebi</h2>
  <p><strong>{HtmlEncode(workshopName)}</strong> tarafından aşağıdaki servis hesap özeti için fatura hazırlanması talep edilmiştir.</p>
  <table cellpadding='6' cellspacing='0' style='border-collapse:collapse;border:1px solid #e2e8f0;margin:14px 0;width:100%;max-width:620px'>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Belge No</strong></td><td style='border:1px solid #e2e8f0'>{HtmlEncode(invoice.InvoiceNumber)}</td></tr>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Müşteri</strong></td><td style='border:1px solid #e2e8f0'>{HtmlEncode(invoice.CustomerTitle)}</td></tr>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Plaka</strong></td><td style='border:1px solid #e2e8f0'>{HtmlEncode(invoice.Plate ?? "-")}</td></tr>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Toplam</strong></td><td style='border:1px solid #e2e8f0'>{invoice.GrandTotal:N2} TL</td></tr>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Tahsil</strong></td><td style='border:1px solid #e2e8f0'>{paidTotal:N2} TL</td></tr>
    <tr><td style='border:1px solid #e2e8f0;background:#f8fafc'><strong>Kalan</strong></td><td style='border:1px solid #e2e8f0'>{remaining:N2} TL</td></tr>
  </table>
  {noteHtml}
  <p>
    <a href='{HtmlEncode(publicLink)}' style='display:inline-block;padding:12px 18px;background:#4f46e5;color:#fff;text-decoration:none;border-radius:12px;font-weight:bold'>Bilgileri Görüntüle ve Fatura Yükle</a>
  </p>
  <p style='font-size:12px;color:#64748b'>Bu bağlantı yalnızca ilgili hesap özeti için geçerlidir ve süresi dolduğunda yükleme yapılamaz.</p>
</div>";
        }

        private static string BuildAccountingBatchRequestEmailBody(
            string workshopName,
            int invoiceCount,
            string publicLink,
            string? customMessage)
        {
            var noteHtml = string.IsNullOrWhiteSpace(customMessage)
                ? string.Empty
                : $"<p style='padding:12px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px'><strong>Not:</strong><br>{HtmlEncode(customMessage)}</p>";

            return $@"
<div style='font-family:Arial,sans-serif;font-size:14px;color:#0f172a;line-height:1.55'>
  <h2 style='margin:0 0 12px'>Fatura hazırlık talebi</h2>
  <p><strong>{HtmlEncode(workshopName)}</strong> tarafından {invoiceCount} servis hesap özeti için fatura yükleme bağlantısı oluşturuldu.</p>
  {noteHtml}
  <p>
    <a href='{HtmlEncode(publicLink)}' style='display:inline-block;padding:12px 18px;background:#2563eb;color:#fff;text-decoration:none;border-radius:12px;font-weight:bold'>Faturaları Yükle</a>
  </p>
  <p style='font-size:12px;color:#64748b'>Bağlantı yalnızca bu gönderimdeki kayıtları gösterir ve süresi dolduğunda yükleme yapılamaz.</p>
</div>";
        }

        private static ServiceResult<string> NormalizeEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ServiceResult<string>.Fail("E-posta adresi zorunludur.");

            var email = value.Trim().ToLowerInvariant();

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return ServiceResult<string>.Fail("Geçerli bir e-posta adresi giriniz.");
            }

            if (email.Length > 256)
                return ServiceResult<string>.Fail("E-posta adresi çok uzun.");

            return ServiceResult<string>.Success(email);
        }

        private static ServiceResult<List<string>> NormalizeRecipients(SendAccountingInvoiceRequestDto request)
        {
            var emails = new List<string>();

            foreach (var email in request.RecipientEmails ?? new List<string>())
            {
                var result = NormalizeEmail(email);
                if (!result.IsSuccess)
                    return ServiceResult<List<string>>.Fail(result.ErrorMessage);
                emails.Add(result.Data!);
            }

            if (!string.IsNullOrWhiteSpace(request.NewRecipientEmail))
            {
                var result = NormalizeEmail(request.NewRecipientEmail);
                if (!result.IsSuccess)
                    return ServiceResult<List<string>>.Fail(result.ErrorMessage);
                emails.Add(result.Data!);
            }

            return ServiceResult<List<string>>.Success(emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        private static string BuildPublicLink(string publicBaseUrl, string token)
        {
            return $"{publicBaseUrl.TrimEnd('/')}/Accounting/InvoiceRequest/{token}";
        }

        private static string BuildBatchPublicLink(string publicBaseUrl, string batchToken)
        {
            return $"{publicBaseUrl.TrimEnd('/')}/Accounting/InvoiceUpload/{batchToken}";
        }

        private static string CreateToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenByteLength)).ToLowerInvariant();
        }

        private static string NormalizeToken(string? token)
        {
            return Regex.Replace(token ?? string.Empty, "[^a-fA-F0-9]", string.Empty).ToLowerInvariant();
        }

        private static bool IsPdfFile(string fileName, string? contentType)
        {
            return Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
                   (string.IsNullOrWhiteSpace(contentType) ||
                    contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase));
        }

        private static string SanitizeFileName(string fileName)
        {
            var name = Path.GetFileName(fileName);
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '-');

            return string.IsNullOrWhiteSpace(name) ? "official-invoice.pdf" : name;
        }

        private string BuildOfficialInvoiceFullPath(string relativePath)
        {
            var rootPath = GetUploadRootPath();
            var normalizedRootPath = EnsureTrailingDirectorySeparator(rootPath);
            var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelativePath));

            if (!fullPath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Fatura dosya yolu geçersiz.");

            return fullPath;
        }

        private string GetUploadRootPath()
        {
            var configuredPath = _configuration[OfficialInvoiceUploadsPathConfigKey];
            var rootPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(AppContext.BaseDirectory, "Uploads")
                : configuredPath.Trim();

            if (!Path.IsPathRooted(rootPath))
                rootPath = Path.Combine(AppContext.BaseDirectory, rootPath);

            return Path.GetFullPath(rootPath);
        }

        private void TryDeleteFile(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Official invoice orphan file cleanup failed. FilePath: {FilePath}",
                    fullPath);
            }
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeDeliveryChannel(string? value)
        {
            var channel = string.IsNullOrWhiteSpace(value)
                ? "Diğer"
                : value.Trim();

            return channel switch
            {
                "WhatsApp" => "WhatsApp",
                "E-posta" => "E-posta",
                "Elden / Diğer" => "Elden / Diğer",
                _ => "Diğer"
            };
        }

        private static string JoinText(params string?[] values)
        {
            return string.Join(" / ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
        }

        private static string? FirstText(params string?[] values)
        {
            return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
        }

        private static string GetAccountingRequestStatusText(AccountingInvoiceRequestStatus status, bool isExpired)
        {
            if (isExpired)
                return "Süresi doldu";

            return status switch
            {
                AccountingInvoiceRequestStatus.Pending => "Fatura bekleniyor",
                AccountingInvoiceRequestStatus.Uploaded => "Fatura yüklendi",
                AccountingInvoiceRequestStatus.Cancelled => "İptal edildi",
                AccountingInvoiceRequestStatus.Expired => "Süresi doldu",
                _ => "Bilinmiyor"
            };
        }

        private static string GetInvoiceItemTypeText(InvoiceItemType type)
        {
            return type switch
            {
                InvoiceItemType.Labor => "İşçilik",
                InvoiceItemType.Part => "Parça",
                _ => "Diğer"
            };
        }

        private static string HtmlEncode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string BuildAccountingEmailFromName(string? workshopName)
        {
            var safeWorkshopName = SanitizeEmailHeaderText(workshopName);

            return string.IsNullOrWhiteSpace(safeWorkshopName)
                ? "Sente360 Muhasebe"
                : $"{safeWorkshopName} | Sente360";
        }

        private static string BuildAccountingEmailSubjectPrefix(string? workshopName)
        {
            return SanitizeEmailHeaderText(workshopName) ?? "Sente360 Muhasebe";
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

        private static string JsonEscape(string? value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private sealed class WorkshopInfo
        {
            public string? WorkshopName { get; set; }

            public string DisplayName { get; set; } = null!;

            public string? NotificationEmail { get; set; }

            public string? TaxOffice { get; set; }

            public string? TaxNumber { get; set; }
        }

        private sealed record AccountingInvoiceEmailWorkItem(
            string Email,
            string InvoiceNumber,
            string Subject,
            string HtmlBody,
            string FromName,
            AccountingInvoiceRequest AccountingRequest);
    }
}
