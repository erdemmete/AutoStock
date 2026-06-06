using AutoStock.Services.Dtos.Admin.Workshops;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class CreateAdminWorkshopRequestDtoValidator : AbstractValidator<CreateAdminWorkshopRequestDto>
{
    private static readonly string[] AllowedRoles = { "Owner", "Staff" };

    public CreateAdminWorkshopRequestDtoValidator()
    {
        RuleFor(x => x.WorkshopName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Servis adı zorunludur.")
            .MaximumLength(200).WithMessage("Servis adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.SubscriptionStatus)
            .IsInEnum()
            .WithMessage("Geçerli bir abonelik durumu seçiniz.");

        RuleFor(x => x.TrialDays)
            .InclusiveBetween(1, 365)
            .WithMessage("Deneme süresi 1 ile 365 gün arasında olmalıdır.")
            .When(x => x.TrialDays.HasValue);

        RuleFor(x => x.SubscriptionEndDate)
            .GreaterThan(DateTime.Today.AddDays(-1))
            .WithMessage("Abonelik bitiş tarihi geçmiş bir tarih olamaz.")
            .When(x => x.SubscriptionEndDate.HasValue);

        RuleFor(x => x.SubscriptionNote)
            .MaximumLength(500)
            .WithMessage("Abonelik notu en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.SubscriptionNote));

        RuleFor(x => x.FirstUserFullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("İlk kullanıcı adı soyadı zorunludur.")
            .MaximumLength(150).WithMessage("İlk kullanıcı adı soyadı en fazla 150 karakter olabilir.");

        RuleFor(x => x.FirstUserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Kullanıcı adı sadece harf, rakam, nokta, alt çizgi veya tire içerebilir.");

        RuleFor(x => x.FirstUserEmail)
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.FirstUserEmail));

        RuleFor(x => x.FirstUserPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Geçici şifre zorunludur.")
            .MinimumLength(6).WithMessage("Geçici şifre en az 6 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Geçici şifre en fazla 100 karakter olabilir.");

        RuleFor(x => x.FirstUserRole)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("İlk kullanıcı rolü zorunludur.")
            .Must(role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("İlk kullanıcı rolü sadece Owner veya Staff olabilir.");
    }
}