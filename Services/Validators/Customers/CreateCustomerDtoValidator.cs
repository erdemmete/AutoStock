using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Customers;
using FluentValidation;

namespace AutoStock.Services.Validators.Customers
{
    public class CreateCustomerDtoValidator
        : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerDtoValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası zorunludur.");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Ad soyad zorunludur.")
                .When(x => x.Type == CustomerType.Individual);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Resmi cari/fatura adı zorunludur.")
                .When(x => x.Type == CustomerType.SoleProprietorship);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Firma resmi ünvanı zorunludur.")
                .When(x => x.Type == CustomerType.Corporate);
        }
    }
}