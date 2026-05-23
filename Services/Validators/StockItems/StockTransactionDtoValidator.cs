using AutoStock.Services.Dtos.StockItems;
using FluentValidation;

namespace AutoStock.Services.Validators.StockItems
{
    public class StockTransactionDtoValidator : AbstractValidator<StockTransactionDto>
    {
        public StockTransactionDtoValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Miktar sıfırdan büyük olmalıdır.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }
}