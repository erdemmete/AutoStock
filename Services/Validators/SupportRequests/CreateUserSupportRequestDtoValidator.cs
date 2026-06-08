using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.SupportRequests;
using FluentValidation;

namespace AutoStock.Services.Validators.SupportRequests
{
    public class CreateUserSupportRequestDtoValidator : AbstractValidator<CreateUserSupportRequestDto>
    {
        public CreateUserSupportRequestDtoValidator()
        {
            RuleFor(x => x.RequestedUserFullName)
                .NotEmpty().WithMessage("Eklenecek kullanıcı adı soyadı zorunludur.")
                .MaximumLength(150).WithMessage("Ad soyad en fazla 150 karakter olabilir.");

            RuleFor(x => x.RequestedUserPhone)
                .MaximumLength(30).WithMessage("Telefon en fazla 30 karakter olabilir.");

            RuleFor(x => x.RequestedUserEmail)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
                .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.")
                .When(x => !string.IsNullOrWhiteSpace(x.RequestedUserEmail));

            RuleFor(x => x.RequestedUserRole)
                .Must(x => Enum.IsDefined(typeof(SupportRequestedUserRole), x))
                .WithMessage("Geçersiz rol seçimi.");

            RuleFor(x => x.Note)
                .MaximumLength(4000).WithMessage("Not en fazla 4000 karakter olabilir.");

            RuleFor(x => x.Priority)
                .Must(x => Enum.IsDefined(typeof(SupportRequestPriority), x))
                .WithMessage("Geçersiz öncelik seçimi.");
        }
    }
}