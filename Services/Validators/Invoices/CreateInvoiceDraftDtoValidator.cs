using AutoStock.Services.Dtos.Invoices;
using FluentValidation;

namespace AutoStock.Services.Validators.Invoices;

public class CreateInvoiceDraftDtoValidator : AbstractValidator<CreateInvoiceDraftDto>
{
    public CreateInvoiceDraftDtoValidator()
    {
        RuleFor(x => x.ServiceRecordId)
            .GreaterThan(0).WithMessage("Geçerli bir servis kaydı seçiniz.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Geçerli bir müşteri seçiniz.");

        RuleFor(x => x.CustomerTitle)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Müşteri ünvanı / adı zorunludur.")
            .MaximumLength(200).WithMessage("Müşteri ünvanı / adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.CustomerTaxOffice)
            .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerTaxOffice));

        RuleFor(x => x.CustomerTaxNumber)
            .Must(BeNumeric).WithMessage("Vergi numarası sadece rakamlardan oluşmalıdır.")
            .Length(10).WithMessage("Vergi numarası 10 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerTaxNumber));

        RuleFor(x => x.CustomerTckn)
            .Must(BeNumeric).WithMessage("TC kimlik numarası sadece rakamlardan oluşmalıdır.")
            .Length(11).WithMessage("TC kimlik numarası 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerTckn));

        RuleFor(x => x.CustomerAddress)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerAddress));

        RuleFor(x => x.Plate)
            .MaximumLength(20).WithMessage("Plaka en fazla 20 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Plate));

        RuleFor(x => x.ChassisNumber)
            .MaximumLength(50).WithMessage("Şasi numarası en fazla 50 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.ChassisNumber));

        RuleFor(x => x.Mileage)
            .InclusiveBetween(0, 2_000_000)
            .WithMessage("Kilometre 0 ile 2.000.000 arasında olmalıdır.")
            .When(x => x.Mileage.HasValue);

        RuleFor(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Fatura kalemleri boş olamaz.")
            .Must(x => x.Count > 0).WithMessage("En az bir fatura kalemi eklenmelidir.")
            .Must(x => x.Count <= 100).WithMessage("En fazla 100 fatura kalemi eklenebilir.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateInvoiceDraftItemDtoValidator());
    }

    private static bool BeNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.All(char.IsDigit);
    }
}