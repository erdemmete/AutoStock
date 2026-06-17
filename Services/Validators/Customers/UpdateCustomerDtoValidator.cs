using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Customers;
using FluentValidation;

namespace AutoStock.Services.Validators.Customers
{
    public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
    {
        public UpdateCustomerDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Geçerli bir müşteri seçilmelidir.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası zorunludur.");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Ad soyad zorunludur.")
                .When(x => x.Type == CustomerType.Individual);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Fatura adı / ünvan zorunludur.")
                .When(x => x.Type == CustomerType.SoleProprietorship);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Firma resmi ünvanı zorunludur.")
                .When(x => x.Type == CustomerType.Corporate);
        }
    }
}
