using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace AutoStock.Services.Services;

public class InvoiceEmailService : IInvoiceEmailService
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;

    public InvoiceEmailService(
        AppDbContext context,
        IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    public async Task<ServiceResult<SendInvoiceEmailResponseDto>> SendInvoiceAsync(
        int invoiceId,
        int workshopId,
        SendInvoiceEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .Include(x => x.ServiceRecord)
            .FirstOrDefaultAsync(x =>
                x.Id == invoiceId &&
                x.WorkshopId == workshopId,
                cancellationToken);

        if (invoice is null)
            return ServiceResult<SendInvoiceEmailResponseDto>.Fail("Fatura bulunamadı.");

        var toEmail = !string.IsNullOrWhiteSpace(invoice.CustomerEmail)
            ? invoice.CustomerEmail.Trim()
            : !string.IsNullOrWhiteSpace(invoice.Customer.Email)
                ? invoice.Customer.Email.Trim()
                : request.ToEmail?.Trim();

        if (string.IsNullOrWhiteSpace(toEmail))
            return ServiceResult<SendInvoiceEmailResponseDto>.Fail("Müşteri e-posta adresi yok. Lütfen alıcı e-posta adresi girin.");

        var workshopName = await _context.Workshops
            .AsNoTracking()
            .Where(x => x.Id == workshopId)
            .Select(x => x.Profile != null && !string.IsNullOrWhiteSpace(x.Profile.DisplayName)
                ? x.Profile.DisplayName!
                : x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Sente360 Servis";

        var subject = $"{workshopName} - {invoice.InvoiceNumber} numaralı fatura";

        var html = BuildInvoiceHtml(workshopName, invoice, request.Message);

        var mailResult = await _emailSender.SendAsync(new EmailMessageDto
        {
            ToEmail = toEmail,
            ToName = invoice.CustomerTitle,
            Subject = subject,
            HtmlBody = html,
            TextBody = $"{invoice.InvoiceNumber} numaralı fatura toplamı: {invoice.GrandTotal:N2} TL"
        }, cancellationToken);

        if (!mailResult.IsSuccess)
            return ServiceResult<SendInvoiceEmailResponseDto>.Fail(mailResult.ErrorMessage ?? "Fatura e-postası gönderilemedi.");

        return ServiceResult<SendInvoiceEmailResponseDto>.Success(new SendInvoiceEmailResponseDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ToEmail = toEmail
        });
    }

    private static string BuildInvoiceHtml(
        string workshopName,
        AutoStock.Repositories.Entities.Invoice invoice,
        string? customMessage)
    {
        var statusText = invoice.Status switch
        {
            InvoiceStatus.Draft => "Taslak",
            InvoiceStatus.Issued => "Kesildi",
            InvoiceStatus.Cancelled => "İptal",
            _ => invoice.Status.ToString()
        };

        var itemsBuilder = new StringBuilder();

        foreach (var item in invoice.Items.OrderBy(x => x.Id))
        {
            itemsBuilder.AppendLine($@"
                <tr>
                    <td style='padding:10px;border-bottom:1px solid #e5e7eb'>{WebUtility.HtmlEncode(item.Description)}</td>
                    <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:right'>{item.Quantity:N2}</td>
                    <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:right'>{item.UnitPrice:N2} TL</td>
                    <td style='padding:10px;border-bottom:1px solid #e5e7eb;text-align:right'>{item.LineTotal:N2} TL</td>
                </tr>");
        }

        var safeMessage = string.IsNullOrWhiteSpace(customMessage)
            ? "Fatura bilgilerinizi aşağıda bulabilirsiniz."
            : WebUtility.HtmlEncode(customMessage).Replace("\n", "<br>");

        return $@"
<!doctype html>
<html lang='tr'>
<head>
    <meta charset='utf-8'>
</head>
<body style='margin:0;background:#f6f8fb;font-family:Arial,Helvetica,sans-serif;color:#0f172a'>
    <div style='max-width:760px;margin:0 auto;padding:28px'>
        <div style='background:white;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden'>
            <div style='padding:24px 28px;background:#4f35e8;color:white'>
                <h1 style='margin:0;font-size:24px'>{WebUtility.HtmlEncode(workshopName)}</h1>
                <p style='margin:8px 0 0;opacity:.9'>Fatura Bilgilendirmesi</p>
            </div>

            <div style='padding:28px'>
                <p style='font-size:16px;line-height:1.6;margin-top:0'>{safeMessage}</p>

                <div style='display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:22px 0'>
                    <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:14px'>
                        <small style='color:#64748b;font-weight:bold'>Fatura No</small><br>
                        <strong>{WebUtility.HtmlEncode(invoice.InvoiceNumber)}</strong>
                    </div>
                    <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:14px'>
                        <small style='color:#64748b;font-weight:bold'>Durum</small><br>
                        <strong>{statusText}</strong>
                    </div>
                    <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:14px'>
                        <small style='color:#64748b;font-weight:bold'>Alıcı</small><br>
                        <strong>{WebUtility.HtmlEncode(invoice.CustomerTitle)}</strong>
                    </div>
                    <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:14px'>
                        <small style='color:#64748b;font-weight:bold'>Araç</small><br>
                        <strong>{WebUtility.HtmlEncode(invoice.Plate ?? "-")}</strong>
                    </div>
                </div>

                <table style='width:100%;border-collapse:collapse;margin-top:18px'>
                    <thead>
                        <tr style='background:#f8fafc'>
                            <th style='padding:10px;text-align:left;border-bottom:1px solid #e5e7eb'>Açıklama</th>
                            <th style='padding:10px;text-align:right;border-bottom:1px solid #e5e7eb'>Miktar</th>
                            <th style='padding:10px;text-align:right;border-bottom:1px solid #e5e7eb'>Birim</th>
                            <th style='padding:10px;text-align:right;border-bottom:1px solid #e5e7eb'>Tutar</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsBuilder}
                    </tbody>
                </table>

                <div style='margin-top:24px;text-align:right'>
                    <div style='color:#64748b'>Ara Toplam: <strong>{invoice.Subtotal:N2} TL</strong></div>
                    <div style='color:#64748b'>KDV: <strong>{invoice.VatTotal:N2} TL</strong></div>
                    <div style='font-size:22px;font-weight:bold;margin-top:8px'>Genel Toplam: {invoice.GrandTotal:N2} TL</div>
                </div>

                <p style='margin-top:28px;color:#64748b;font-size:13px;line-height:1.5'>
                    Bu e-posta {WebUtility.HtmlEncode(workshopName)} tarafından Sente360 üzerinden gönderilmiştir.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
