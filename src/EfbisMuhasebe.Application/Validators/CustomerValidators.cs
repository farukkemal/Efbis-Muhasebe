using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.CustomerCode)
            .NotEmpty().WithMessage("Cari kodu zorunludur.")
            .MaximumLength(50).WithMessage("Cari kodu en fazla 50 karakter olabilir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Firma unvanı / ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Firma unvanı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.CustomerCode)
            .NotEmpty().WithMessage("Cari kodu zorunludur.")
            .MaximumLength(50).WithMessage("Cari kodu en fazla 50 karakter olabilir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Firma unvanı / ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Firma unvanı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
