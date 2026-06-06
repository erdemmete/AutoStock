using AutoStock.Services.Dtos.Invoices;
using FluentValidation;

namespace AutoStock.Services.Validators.Invoices;

public class CreateInvoiceDraftItemDtoValidator : AbstractValidator<CreateInvoiceDraftItemDto>
{
    public CreateInvoiceDraftItemDtoValidator()
    {
        RuleFor(x => x.ItemType)
            .InclusiveBetween(1, 3)
            .WithMessage("Geçerli bir kalem tipi seçiniz.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kalem açıklaması zorunludur.")
            .MaximumLength(300).WithMessage("Kalem açıklaması en fazla 300 karakter olabilir.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(9999).WithMessage("Miktar en fazla 9999 olabilir.");

        RuleFor(x => x.Unit)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Birim zorunludur.")
            .MaximumLength(30).WithMessage("Birim en fazla 30 karakter olabilir.");

        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0, 100_000_000)
            .WithMessage("Birim fiyat geçerli aralıkta olmalıdır.");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100)
            .WithMessage("İskonto oranı 0 ile 100 arasında olmalıdır.");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.");

        RuleFor(x => x.StockItemId)
            .GreaterThan(0).WithMessage("Geçerli bir stok kartı seçiniz.")
            .When(x => x.StockItemId.HasValue);
    }
}