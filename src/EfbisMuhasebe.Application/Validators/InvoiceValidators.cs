using EfbisMuhasebe.Application.DTOs;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Geçerli bir müşteri seçilmelidir.");
        RuleFor(x => x.InvoiceType).IsInEnum().WithMessage("Geçerli bir fatura tipi seçilmelidir.");
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("Fatura tarihi boş olamaz.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Faturada en az bir kalem bulunmalıdır.");
        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemDtoValidator());
    }
}

public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
{
    public CreateInvoiceItemDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Geçerli bir ürün seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Birim fiyat 0 veya daha büyük olmalıdır.");
        RuleFor(x => x.VatRate).GreaterThanOrEqualTo(0).WithMessage("KDV oranı geçerli olmalıdır.");
        RuleFor(x => x.DiscountRate).InclusiveBetween(0, 100).WithMessage("İskonto oranı 0 ile 100 arasında olmalıdır.");
    }
}
