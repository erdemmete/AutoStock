using AutoStock.Services.Dtos.ServiceRecords;
using FluentValidation;

namespace AutoStock.Services.Validators.ServiceRecords;

public class UpdateServiceRecordStatusRequestValidator : AbstractValidator<UpdateServiceRecordStatusRequest>
{
    public UpdateServiceRecordStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .InclusiveBetween(1, 4)
            .WithMessage("Geçerli bir servis durumu seçiniz.");
    }
}