using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad boş bırakılamaz.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad boş bırakılamaz.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Personel kodu boş bırakılamaz.")
            .MaximumLength(20).WithMessage("Personel kodu en fazla 20 karakter olabilir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Unvan/Görev alanı boş bırakılamaz.");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Maaş negatif olamaz.");
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçersiz personel kimliği.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad boş bırakılamaz.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad boş bırakılamaz.");

        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Personel kodu boş bırakılamaz.");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Maaş negatif olamaz.");
    }
}
