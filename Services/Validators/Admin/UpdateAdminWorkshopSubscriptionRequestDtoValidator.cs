using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Interfaces;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class UpdateAdminWorkshopSubscriptionRequestDtoValidator : AbstractValidator<UpdateAdminWorkshopSubscriptionRequestDto>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateAdminWorkshopSubscriptionRequestDtoValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        RuleFor(x => x.SubscriptionStatus)
            .IsInEnum()
            .WithMessage("Geçerli bir abonelik durumu seçiniz.");

        RuleFor(x => x.SubscriptionEndDate)
             .Must(x => !x.HasValue || x.Value.Date >= dateTimeProvider.Today)
             .WithMessage("Üyelik bitiş tarihi bugünden önce olamaz.")
             .Must(x => !x.HasValue || x.Value.Date <= dateTimeProvider.Today.AddYears(10))
             .WithMessage("Üyelik bitiş tarihi en fazla 10 yıl sonrası olabilir.");

        RuleFor(x => x.SubscriptionNote)
            .MaximumLength(500)
            .WithMessage("Abonelik notu en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.SubscriptionNote));
    }
}