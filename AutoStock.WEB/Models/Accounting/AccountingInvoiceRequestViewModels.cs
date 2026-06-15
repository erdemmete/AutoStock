using AutoStock.Services.Dtos.Accounting;

namespace AutoStock.WEB.Models.Accounting
{
    public class SendAccountingInvoiceRequestViewModel
    {
        public int InvoiceId { get; set; }

        public List<string> RecipientEmails { get; set; } = new();

        public string? NewRecipientEmail { get; set; }

        public string? NewRecipientDisplayName { get; set; }

        public bool SaveNewRecipient { get; set; }

        public string? Message { get; set; }

        public string? PublicBaseUrl { get; set; }

        public SendAccountingInvoiceRequestDto ToDto()
        {
            return new SendAccountingInvoiceRequestDto
            {
                InvoiceId = InvoiceId,
                RecipientEmails = RecipientEmails ?? new List<string>(),
                NewRecipientEmail = NewRecipientEmail,
                NewRecipientDisplayName = NewRecipientDisplayName,
                SaveNewRecipient = SaveNewRecipient,
                Message = Message,
                PublicBaseUrl = PublicBaseUrl
            };
        }
    }

    public class AccountingInvoiceRequestPublicViewModel : AccountingInvoiceRequestPublicDto
    {
        public string UploadApiUrl { get; set; } = null!;

        public string ReturnUrl { get; set; } = null!;

        public bool Uploaded { get; set; }

        public string? UploadError { get; set; }
    }

    public class OfficialInvoiceDownloadResult
    {
        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public List<string> ErrorMessages { get; set; } = new();

        public byte[] Content { get; set; } = Array.Empty<byte>();

        public string FileName { get; set; } = "resmi-fatura.pdf";

        public string ContentType { get; set; } = "application/pdf";

        public static OfficialInvoiceDownloadResult Success(byte[] content, string fileName, string contentType)
        {
            return new OfficialInvoiceDownloadResult
            {
                IsSuccess = true,
                Content = content,
                FileName = fileName,
                ContentType = contentType
            };
        }

        public static OfficialInvoiceDownloadResult Fail(string? errorMessage, IEnumerable<string>? errorMessages = null)
        {
            var messages = (errorMessages ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (!string.IsNullOrWhiteSpace(errorMessage) && !messages.Contains(errorMessage))
                messages.Insert(0, errorMessage);

            if (!messages.Any())
                messages.Add("Fatura dosyası indirilemedi.");

            return new OfficialInvoiceDownloadResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorMessages = messages
            };
        }
    }
}
