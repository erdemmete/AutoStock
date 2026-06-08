using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.SupportRequests;
using FluentValidation;

namespace AutoStock.Services.Validators.SupportRequests
{
    public class AdminUpdateSupportRequestStatusDtoValidator : AbstractValidator<AdminUpdateSupportRequestStatusDto>
    {
        public AdminUpdateSupportRequestStatusDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Destek talebi bilgisi hatalı.");

            RuleFor(x => x.Status)
                .Must(x => Enum.IsDefined(typeof(SupportRequestStatus), x))
                .WithMessage("Geçersiz destek talebi durumu.");
        }
    }
}