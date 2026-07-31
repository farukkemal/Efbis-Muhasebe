using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateCashAccountValidator : AbstractValidator<CreateCashAccountDto>
{
    public CreateCashAccountValidator()
    {
        RuleFor(x => x.AccountCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}

public class UpdateCashAccountValidator : AbstractValidator<UpdateCashAccountDto>
{
    public UpdateCashAccountValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AccountCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}

public class CreateCashTransactionValidator : AbstractValidator<CreateCashTransactionDto>
{
    public CreateCashTransactionValidator()
    {
        RuleFor(x => x.CashAccountId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
