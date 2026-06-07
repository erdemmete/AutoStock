using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using FluentValidation;

namespace AutoStock.Services.Validators.ServiceRecords;

public class CreateServiceRecordRequestValidator : AbstractValidator<CreateServiceRecordRequest>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateServiceRecordRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
      
    }
    public CreateServiceRecordRequestValidator()
    {
        RuleFor(x => x.CustomerPhoneNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .MaximumLength(25).WithMessage("Telefon numarası en fazla 25 karakter olabilir.")
            .Must(BeValidTurkishMobilePhone).WithMessage("Geçerli bir cep telefonu numarası giriniz.");

        RuleFor(x => x.CustomerName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Müşteri adı zorunludur.")
            .MaximumLength(150).WithMessage("Müşteri adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.CustomerEmail)
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));

        RuleFor(x => x.Plate)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Plaka zorunludur.")
            .MaximumLength(20).WithMessage("Plaka en fazla 20 karakter olabilir.")
            .Must(HasLetterOrDigit).WithMessage("Geçerli bir plaka giriniz.");

        RuleFor(x => x.VehicleBrandId)
            .GreaterThan(0).WithMessage("Geçerli bir araç markası seçiniz.")
            .When(x => x.VehicleBrandId.HasValue);

        RuleFor(x => x.VehicleModelId)
            .GreaterThan(0).WithMessage("Geçerli bir araç modeli seçiniz.")
            .When(x => x.VehicleModelId.HasValue);

        RuleFor(x => x.ModelYear)
            .InclusiveBetween(1950, _dateTimeProvider.Now.Year + 1)
            .WithMessage("Model yılı geçerli aralıkta olmalıdır.")
            .When(x => x.ModelYear.HasValue);

        RuleFor(x => x.Mileage)
            .InclusiveBetween(0, 2_000_000)
            .WithMessage("Kilometre 0 ile 2.000.000 arasında olmalıdır.")
            .When(x => x.Mileage.HasValue);

        RuleFor(x => x.ChassisNumber)
            .MaximumLength(50).WithMessage("Şasi numarası en fazla 50 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.ChassisNumber));

        RuleFor(x => x.CustomerComplaint)
            .MaximumLength(1000).WithMessage("Müşteri şikayeti en fazla 1000 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerComplaint));

        RuleFor(x => x.ServiceReceptionNote)
            .MaximumLength(1500).WithMessage("Servis kabul notu en fazla 1500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.ServiceReceptionNote));

        RuleFor(x => x.EstimatedAmount)
            .InclusiveBetween(0, 100_000_000)
            .WithMessage("Tahmini tutar geçerli aralıkta olmalıdır.")
            .When(x => x.EstimatedAmount.HasValue);

        RuleFor(x => x.EstimatedAmountNote)
            .MaximumLength(500).WithMessage("Tahmini tutar notu en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.EstimatedAmountNote));

        RuleFor(x => x.CustomerType)
            .IsInEnum().WithMessage("Geçerli bir müşteri tipi seçiniz.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));

        RuleFor(x => x.AuthorizedPersonName)
            .MaximumLength(150).WithMessage("Yetkili kişi adı en fazla 150 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorizedPersonName));

        RuleFor(x => x.TaxNumber)
            .Must(BeNumeric).WithMessage("Vergi numarası sadece rakamlardan oluşmalıdır.")
            .Length(10, 11).WithMessage("Vergi numarası 10 veya 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

        RuleFor(x => x.NationalIdentityNumber)
            .Must(BeNumeric).WithMessage("TC kimlik numarası sadece rakamlardan oluşmalıdır.")
            .Length(11).WithMessage("TC kimlik numarası 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalIdentityNumber));

        RuleFor(x => x.TaxOffice)
            .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxOffice));

        RuleFor(x => x.CustomerAddress)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerAddress));

        RuleFor(x => x.AddressCity)
            .MaximumLength(100).WithMessage("İl bilgisi en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.AddressCity));

        RuleFor(x => x.AddressDistrict)
            .MaximumLength(100).WithMessage("İlçe bilgisi en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.AddressDistrict));

        RuleFor(x => x.VehicleDeliveredBy)
            .MaximumLength(150).WithMessage("Aracı teslim eden kişi en fazla 150 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.VehicleDeliveredBy));

        RuleFor(x => x.RequestItems)
    .Must(x => x.Count <= 30)
    .WithMessage("En fazla 30 şikayet / talep eklenebilir.");

        RuleForEach(x => x.RequestItems)
            .SetValidator(new CreateServiceRequestItemDtoValidator());
    }

    private static bool BeValidTurkishMobilePhone(string value)
    {
        var digits = OnlyDigits(value);

        return digits.Length switch
        {
            10 => digits.StartsWith("5"),
            11 => digits.StartsWith("05"),
            _ => false
        };
    }

    private static bool HasLetterOrDigit(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Any(char.IsLetterOrDigit);
    }

    private static bool BeNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.All(char.IsDigit);
    }

    private static string OnlyDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }
}