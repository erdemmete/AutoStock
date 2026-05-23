using AutoStock.Services.Dtos.StockItems;
using FluentValidation;

namespace AutoStock.Services.Validators.StockItems
{
    public class UpdateStockItemDtoValidator : AbstractValidator<UpdateStockItemDto>
    {
        public UpdateStockItemDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Stok bilgisi hatalı.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stok adı zorunludur.")
                .MaximumLength(150).WithMessage("Stok adı en fazla 150 karakter olabilir.");

            RuleFor(x => x.Code)
                .MaximumLength(50).WithMessage("Stok kodu en fazla 50 karakter olabilir.");

            RuleFor(x => x.Barcode)
                .MaximumLength(100).WithMessage("Barkod en fazla 100 karakter olabilir.");

            RuleFor(x => x.Brand)
                .MaximumLength(100).WithMessage("Marka en fazla 100 karakter olabilir.");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Birim zorunludur.")
                .MaximumLength(20).WithMessage("Birim en fazla 20 karakter olabilir.");

            RuleFor(x => x.PurchasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Alış fiyatı negatif olamaz.");

            RuleFor(x => x.SalePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı negatif olamaz.");

            RuleFor(x => x.MinimumQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum miktar negatif olamaz.");
        }
    }
}