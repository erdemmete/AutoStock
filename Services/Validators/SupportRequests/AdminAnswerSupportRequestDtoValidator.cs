using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.SupportRequests;
using FluentValidation;

namespace AutoStock.Services.Validators.SupportRequests
{
    public class AdminAnswerSupportRequestDtoValidator : AbstractValidator<AdminAnswerSupportRequestDto>
    {
        public AdminAnswerSupportRequestDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Destek talebi bilgisi hatalı.");

            RuleFor(x => x.AdminResponse)
                .NotEmpty().WithMessage("Admin cevabı zorunludur.")
                .MaximumLength(4000).WithMessage("Admin cevabı en fazla 4000 karakter olabilir.");

            RuleFor(x => x.Status)
                .Must(x =>
                    x == SupportRequestStatus.InProgress ||
                    x == SupportRequestStatus.Answered ||
                    x == SupportRequestStatus.Closed)
                .WithMessage("Cevap sonrası durum İşlemde, Yanıtlandı veya Kapandı olabilir.");
        }
    }
}