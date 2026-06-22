using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Account
{
    public class AccountPageViewModel
    {
        public AccountOverviewViewModel Overview { get; set; } = new();
        public AccountEmailFormViewModel EmailForm { get; set; } = new();
        public AccountPhoneFormViewModel PhoneForm { get; set; } = new();
        public ChangePasswordFormViewModel PasswordForm { get; set; } = new();
        public WorkshopProfileFormViewModel? WorkshopProfile { get; set; }
        public MembershipInfoViewModel? Membership { get; set; }
        public bool IsOwner { get; set; }
    }

    public class AccountOverviewViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string WorkshopName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class AccountPhoneFormViewModel
    {
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class AccountEmailFormViewModel
    {
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordFormViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class WorkshopProfileFormViewModel
    {
        public string WorkshopName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LegalTitle { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public List<AccountWorkshopBankAccountViewModel> BankAccounts { get; set; } = new();
    }

    public class AccountWorkshopBankAccountViewModel
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

    public class CreateAccountWorkshopBankAccountViewModel
    {
        [Required(ErrorMessage = "Banka adı zorunludur.")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hesap sahibi zorunludur.")]
        public string AccountHolder { get; set; } = string.Empty;

        [Required(ErrorMessage = "IBAN zorunludur.")]
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

    public class UpdateAccountWorkshopBankAccountViewModel : CreateAccountWorkshopBankAccountViewModel
    {
        public int BankAccountId { get; set; }
    }

    public class MembershipInfoViewModel
    {
        public string WorkshopName { get; set; } = string.Empty;
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
}
