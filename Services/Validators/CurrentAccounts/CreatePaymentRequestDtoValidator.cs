using AutoStock.Services.Dtos.CurrentAccounts;
using AutoStock.Services.Interfaces;
using FluentValidation;

namespace AutoStock.Services.Validators.CurrentAccounts;

public class CreatePaymentRequestDtoValidator : AbstractValidator<CreatePaymentRequestDto>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePaymentRequestDtoValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        
    }
    public CreatePaymentRequestDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir müşteri seçiniz.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Tahsilat tutarı 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000_000)
            .WithMessage("Tahsilat tutarı geçerli aralıkta olmalıdır.");

        RuleFor(x => x.PaymentDate)
            .LessThanOrEqualTo(_dateTimeProvider.Now.AddDays(1))
            .WithMessage("Tahsilat tarihi ileri bir tarih olamaz.")
            .When(x => x.PaymentDate.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Açıklama en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}