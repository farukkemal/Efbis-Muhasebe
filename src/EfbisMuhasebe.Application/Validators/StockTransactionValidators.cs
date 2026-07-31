using FluentValidation;
using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Validators;

public class CreateStockTransactionValidator : AbstractValidator<CreateStockTransactionDto>
{
    public CreateStockTransactionValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Ürün seçimi zorunludur.");
            
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.");
            
        RuleFor(x => x.TransactionType)
            .IsInEnum().WithMessage("Geçersiz işlem tipi.");
    }
}
