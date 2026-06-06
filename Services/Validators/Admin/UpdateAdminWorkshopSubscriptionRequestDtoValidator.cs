using AutoStock.Services.Dtos.Admin.Workshops;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class UpdateAdminWorkshopSubscriptionRequestDtoValidator : AbstractValidator<UpdateAdminWorkshopSubscriptionRequestDto>
{
    public UpdateAdminWorkshopSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.SubscriptionStatus)
            .IsInEnum()
            .WithMessage("Geçerli bir abonelik durumu seçiniz.");

        RuleFor(x => x.SubscriptionEndDate)
            .LessThanOrEqualTo(DateTime.Today.AddYears(10))
            .WithMessage("Abonelik bitiş tarihi çok ileri bir tarih olamaz.")
            .When(x => x.SubscriptionEndDate.HasValue);

        RuleFor(x => x.SubscriptionNote)
            .MaximumLength(500)
            .WithMessage("Abonelik notu en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.SubscriptionNote));
    }
}