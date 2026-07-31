using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateShiftDtoValidator : AbstractValidator<CreateShiftDto>
{
    public CreateShiftDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Personel seçimi zorunludur.");

        RuleFor(x => x.ShiftDate)
            .NotEmpty().WithMessage("Vardiya tarihi zorunludur.");

        RuleFor(x => x.ShiftType)
            .IsInEnum().WithMessage("Geçerli bir vardiya tipi seçiniz.");
    }
}

public class UpdateShiftDtoValidator : AbstractValidator<UpdateShiftDto>
{
    public UpdateShiftDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir vardiya seçiniz.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Personel seçimi zorunludur.");

        RuleFor(x => x.ShiftDate)
            .NotEmpty().WithMessage("Vardiya tarihi zorunludur.");

        RuleFor(x => x.ShiftType)
            .IsInEnum().WithMessage("Geçerli bir vardiya tipi seçiniz.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçerli bir durum seçiniz.");
    }
}
