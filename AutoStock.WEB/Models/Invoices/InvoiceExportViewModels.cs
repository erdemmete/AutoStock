using AutoStock.Services.Dtos.Invoices;

namespace AutoStock.WEB.Models.Invoices;

public class InvoiceExportQueryViewModel
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Preset { get; set; }

    public bool IncludeCancelled { get; set; }

    public string? ToEmail { get; set; }

    public string? Message { get; set; }

    public void Normalize()
    {
        var today = DateTime.Today;
        var preset = string.IsNullOrWhiteSpace(Preset)
            ? "this-month"
            : Preset.Trim().ToLowerInvariant();

        if (!StartDate.HasValue || !EndDate.HasValue)
        {
            (StartDate, EndDate) = preset switch
            {
                "this-week" => GetThisWeek(today),
                "last-week" => GetLastWeek(today),
                "last-month" => GetLastMonth(today),
                "last-30-days" => (today.AddDays(-29), today),
                "today" => (today, today),
                _ => (new DateTime(today.Year, today.Month, 1), today)
            };
        }

        StartDate = StartDate.Value.Date;
        EndDate = EndDate.Value.Date;
        Preset = preset;
    }

    public InvoiceExportQueryDto ToDto()
    {
        Normalize();

        return new InvoiceExportQueryDto
        {
            StartDate = StartDate,
            EndDate = EndDate,
            Preset = Preset,
            IncludeCancelled = IncludeCancelled
        };
    }

    public SendInvoiceExportEmailRequestDto ToEmailDto()
    {
        Normalize();

        return new SendInvoiceExportEmailRequestDto
        {
            StartDate = StartDate,
            EndDate = EndDate,
            Preset = Preset,
            IncludeCancelled = IncludeCancelled,
            ToEmail = ToEmail ?? string.Empty,
            Message = Message
        };
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
        return (thisWeekStart.AddDays(-7), thisWeekStart.AddDays(-1));
    }

    private static (DateTime Start, DateTime End) GetLastMonth(DateTime today)
    {
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
        return (firstDayLastMonth, firstDayThisMonth.AddDays(-1));
    }
}

public class InvoiceExportIndexViewModel
{
    public InvoiceExportQueryViewModel Query { get; set; } = new();

    public InvoiceExportPreviewDto Preview { get; set; } = new();
}

public class InvoiceExportDownloadResult
{
    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }

    public List<string> ErrorMessages { get; set; } = new();

    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string FileName { get; set; } = "fatura-aktarim.zip";

    public string ContentType { get; set; } = "application/zip";

    public static InvoiceExportDownloadResult Success(byte[] content, string fileName, string contentType)
    {
        return new InvoiceExportDownloadResult
        {
            IsSuccess = true,
            Content = content,
            FileName = fileName,
            ContentType = contentType
        };
    }

    public static InvoiceExportDownloadResult Fail(string? errorMessage, IEnumerable<string>? errorMessages = null)
    {
        var messages = (errorMessages ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        if (!string.IsNullOrWhiteSpace(errorMessage) && !messages.Contains(errorMessage))
            messages.Insert(0, errorMessage);

        if (!messages.Any())
            messages.Add("Fatura aktarım paketi indirilemedi.");

        return new InvoiceExportDownloadResult
        {
            IsSuccess = false,
            ErrorMessage = messages.FirstOrDefault(),
            ErrorMessages = messages
        };
    }
}
