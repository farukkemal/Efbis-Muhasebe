using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseDto>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Depo Adı boş olamaz.")
            .MaximumLength(200).WithMessage("Depo Adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.WarehouseCode)
            .NotEmpty().WithMessage("Depo Kodu boş olamaz.")
            .MaximumLength(20).WithMessage("Depo Kodu en fazla 20 karakter olabilir.");
    }
}

public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseDto>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçersiz depo kimliği.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Depo Adı boş olamaz.")
            .MaximumLength(200).WithMessage("Depo Adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.WarehouseCode)
            .NotEmpty().WithMessage("Depo Kodu boş olamaz.")
            .MaximumLength(20).WithMessage("Depo Kodu en fazla 20 karakter olabilir.");
    }
}
