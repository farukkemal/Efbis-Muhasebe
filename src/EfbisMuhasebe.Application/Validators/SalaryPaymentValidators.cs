using System;
using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateSalaryPaymentDtoValidator : AbstractValidator<CreateSalaryPaymentDto>
{
    public CreateSalaryPaymentDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Personel seçilmelidir.");

        RuleFor(x => x.Year)
            .GreaterThan(2020).WithMessage("Yıl 2020'den büyük olmalıdır.")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage($"Yıl en fazla {DateTime.Now.Year + 1} olabilir.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Ay 1 ile 12 arasında olmalıdır.");

        RuleFor(x => x.GrossSalary)
            .GreaterThan(0).WithMessage("Brüt maaş 0'dan büyük olmalıdır.");
    }
}
