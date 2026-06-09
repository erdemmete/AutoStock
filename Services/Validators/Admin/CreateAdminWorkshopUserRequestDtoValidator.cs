using AutoStock.Services.Dtos.Admin.Workshops;
using FluentValidation;

namespace AutoStock.Services.Validators.Admin.Workshops;

public class CreateAdminWorkshopUserRequestDtoValidator : AbstractValidator<CreateAdminWorkshopUserRequestDto>
{
    private static readonly string[] AllowedRoles = { "Owner", "Staff" };

    public CreateAdminWorkshopUserRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(150).WithMessage("Ad soyad en fazla 150 karakter olabilir.");

        RuleFor(x => x.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Kullanıcı adı sadece harf, rakam, nokta, alt çizgi veya tire içerebilir.");

        RuleFor(x => x.Email)
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(25).WithMessage("Telefon numarası en fazla 25 karakter olabilir.")
            .Must(BeValidPhone).WithMessage("Geçerli bir telefon numarası giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Rol zorunludur.")
            .Must(role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Rol sadece Owner veya Staff olabilir.");
    }

    private static bool BeValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        return digits.Length is >= 10 and <= 15;
    }
}