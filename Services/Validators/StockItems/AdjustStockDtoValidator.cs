using AutoStock.Services.Dtos.StockItems;
using FluentValidation;

namespace AutoStock.Services.Validators.StockItems
{
    public class AdjustStockDtoValidator : AbstractValidator<AdjustStockDto>
    {
        public AdjustStockDtoValidator()
        {
            RuleFor(x => x.NewQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Yeni stok miktarı negatif olamaz.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }
}