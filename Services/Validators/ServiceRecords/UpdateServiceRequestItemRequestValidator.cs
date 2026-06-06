using AutoStock.Services.Dtos.ServiceRecords;
using FluentValidation;

namespace AutoStock.Services.Validators.ServiceRecords;

public class UpdateServiceRequestItemRequestValidator : AbstractValidator<UpdateServiceRequestItemRequest>
{
    public UpdateServiceRequestItemRequestValidator()
    {
        RuleFor(x => x.RepairDetail)
            .MaximumLength(2000).WithMessage("Onarım detayı en fazla 2000 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.RepairDetail));

        RuleFor(x => x.FinalAmount)
            .InclusiveBetween(0, 100_000_000)
            .WithMessage("Son tutar geçerli aralıkta olmalıdır.")
            .When(x => x.FinalAmount.HasValue);
    }
}