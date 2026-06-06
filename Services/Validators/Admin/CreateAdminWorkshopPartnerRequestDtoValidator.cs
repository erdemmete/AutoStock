using AutoStock.Services.Dtos.Admin.Workshops;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class CreateAdminWorkshopPartnerRequestDtoValidator : AbstractValidator<CreateAdminWorkshopPartnerRequestDto>
{
    public CreateAdminWorkshopPartnerRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yetkili / ortak adı zorunludur.")
            .MaximumLength(150).WithMessage("Yetkili / ortak adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Ünvan en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(25).WithMessage("Telefon numarası en fazla 25 karakter olabilir.")
            .Must(BeValidPhone).WithMessage("Geçerli bir telefon numarası giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Email)
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }

    private static bool BeValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        return digits.Length is >= 10 and <= 15;
    }
}