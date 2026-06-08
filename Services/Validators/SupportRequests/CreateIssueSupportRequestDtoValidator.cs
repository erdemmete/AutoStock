using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.SupportRequests;
using FluentValidation;

namespace AutoStock.Services.Validators.SupportRequests
{
    public class CreateIssueSupportRequestDtoValidator : AbstractValidator<CreateIssueSupportRequestDto>
    {
        public CreateIssueSupportRequestDtoValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Konu zorunludur.")
                .MaximumLength(200).WithMessage("Konu en fazla 200 karakter olabilir.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Açıklama zorunludur.")
                .MaximumLength(4000).WithMessage("Açıklama en fazla 4000 karakter olabilir.");

            RuleFor(x => x.Priority)
                .Must(x => Enum.IsDefined(typeof(SupportRequestPriority), x))
                .WithMessage("Geçersiz öncelik seçimi.");
        }
    }
}