using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Account
{
    public class AccountOverviewDto
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string WorkshopName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateAccountEmailRequestDto
    {
        public string? Email { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class UpdateAccountPhoneRequestDto
    {
        public string? PhoneNumber { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class WorkshopProfileManagementDto
    {
        public string WorkshopName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LegalTitle { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public List<AccountWorkshopBankAccountDto> BankAccounts { get; set; } = new();
    }

    public class UpdateWorkshopProfileManagementRequestDto
    {
        public string? DisplayName { get; set; }
        public string? LegalTitle { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    public class AccountWorkshopBankAccountDto
    {
        public int Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "TRY";
        public string? BranchName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public bool ShowOnInvoices { get; set; } = true;
        public bool ShowOnServiceForms { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateAccountWorkshopBankAccountRequestDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "TRY";
        public string? BranchName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public bool ShowOnInvoices { get; set; } = true;
        public bool ShowOnServiceForms { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateAccountWorkshopBankAccountRequestDto : CreateAccountWorkshopBankAccountRequestDto
    {
        public bool IsActive { get; set; } = true;
    }

    public class MembershipInfoDto
    {
        public string WorkshopName { get; set; } = string.Empty;
        public WorkshopSubscriptionStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string SummaryText { get; set; } = string.Empty;
        public DateTime SubscriptionStartDate { get; set; }
        public string SubscriptionStartDateText { get; set; } = string.Empty;
        public DateTime? SubscriptionEndDate { get; set; }
        public string ValidityText { get; set; } = string.Empty;
        public string? RemainingText { get; set; }
        public int? RemainingDays { get; set; }
        public bool IsTrial { get; set; }
        public int ActiveUserCount { get; set; }
        public string ActiveUserCountText { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string ResetUrlBase { get; set; } = string.Empty;
    }

    public class RequestEmailConfirmationDto
    {
        public string ConfirmationUrlBase { get; set; } = string.Empty;
    }

    public class ConfirmEmailDto
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
