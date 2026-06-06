using AutoStock.Services.Dtos.ServiceRecords;
using FluentValidation;

namespace AutoStock.Services.Validators.ServiceRecords;

public class CreateServiceRequestItemDtoValidator : AbstractValidator<CreateServiceRequestItemDto>
{
    public CreateServiceRequestItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Şikayet / talep başlığı zorunludur.")
            .MaximumLength(300).WithMessage("Şikayet / talep başlığı en fazla 300 karakter olabilir.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Şikayet / talep notu en fazla 1000 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.EstimatedAmount)
            .InclusiveBetween(0, 100_000_000)
            .WithMessage("Tahmini tutar geçerli aralıkta olmalıdır.")
            .When(x => x.EstimatedAmount.HasValue);
    }
}