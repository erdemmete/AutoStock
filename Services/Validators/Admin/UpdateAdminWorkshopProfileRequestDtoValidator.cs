using AutoStock.Services.Dtos.Admin.Workshops;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class UpdateAdminWorkshopProfileRequestDtoValidator : AbstractValidator<UpdateAdminWorkshopProfileRequestDto>
{
    public UpdateAdminWorkshopProfileRequestDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Görünen ad en fazla 200 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));

        RuleFor(x => x.LegalTitle)
            .MaximumLength(250).WithMessage("Yasal ünvan en fazla 250 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.LegalTitle));

        RuleFor(x => x.TaxOffice)
            .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxOffice));

        RuleFor(x => x.TaxNumber)
            .Must(BeNumeric).WithMessage("Vergi numarası sadece rakamlardan oluşmalıdır.")
            .Length(10, 11).WithMessage("Vergi numarası 10 veya 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

        RuleFor(x => x.TradeRegistryNumber)
            .MaximumLength(50).WithMessage("Ticaret sicil numarası en fazla 50 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TradeRegistryNumber));

        RuleFor(x => x.MersisNumber)
            .Must(BeNumeric).WithMessage("MERSİS numarası sadece rakamlardan oluşmalıdır.")
            .Length(16).WithMessage("MERSİS numarası 16 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.MersisNumber));

        RuleFor(x => x.Email)
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(25).WithMessage("Telefon numarası en fazla 25 karakter olabilir.")
            .Must(BeValidPhone).WithMessage("Geçerli bir telefon numarası giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.FaxNumber)
            .MaximumLength(25).WithMessage("Faks numarası en fazla 25 karakter olabilir.")
            .Must(BeValidPhone).WithMessage("Geçerli bir faks numarası giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.FaxNumber));

        RuleFor(x => x.Website)
            .MaximumLength(200).WithMessage("Web sitesi en fazla 200 karakter olabilir.")
            .Must(BeValidWebsite).WithMessage("Geçerli bir web sitesi adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.AddressLine)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.AddressLine));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("İl en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("İlçe en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.District));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Posta kodu en fazla 20 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Ülke en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Country));
    }

    private static bool BeNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.All(char.IsDigit);
    }

    private static bool BeValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        return digits.Length is >= 10 and <= 15;
    }

    private static bool BeValidWebsite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"https://{value}";

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && !string.IsNullOrWhiteSpace(uri.Host)
               && uri.Host.Contains('.');
    }
}