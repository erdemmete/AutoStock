using AutoStock.Services.Dtos.ServiceRecords;
using FluentValidation;

namespace AutoStock.Services.Validators.ServiceRecords;

public class AddServiceOperationRequestValidator : AbstractValidator<AddServiceOperationRequest>
{
    public AddServiceOperationRequestValidator()
    {
        RuleFor(x => x.ServiceRequestItemId)
            .GreaterThan(0).WithMessage("Geçerli bir şikayet / talep seçiniz.")
            .When(x => x.ServiceRequestItemId.HasValue);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Geçerli bir işlem tipi seçiniz.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("İşlem açıklaması zorunludur.")
            .MaximumLength(300).WithMessage("İşlem açıklaması en fazla 300 karakter olabilir.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 999)
            .WithMessage("Miktar 1 ile 999 arasında olmalıdır.");

        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0, 100_000_000)
            .WithMessage("Birim fiyat geçerli aralıkta olmalıdır.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("İşlem notu en fazla 1000 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.StockItemId)
            .GreaterThan(0).WithMessage("Geçerli bir stok kartı seçiniz.")
            .When(x => x.StockItemId.HasValue);
    }
}