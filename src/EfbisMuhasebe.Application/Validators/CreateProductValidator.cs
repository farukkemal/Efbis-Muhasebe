using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

/// <summary>
/// Yeni ürün oluşturma validasyonları.
/// Benzersizlik kontrolleri için IUnitOfWork inject edilir.
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Ürün kodu zorunludur.")
            .MaximumLength(50).WithMessage("Ürün kodu en fazla 50 karakter olabilir.")
            .MustAsync(async (code, cancellation) =>
                await _unitOfWork.Products.IsProductCodeUniqueAsync(code))
            .WithMessage("Bu ürün kodu zaten kullanılmaktadır.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barkod en fazla 100 karakter olabilir.")
            .MustAsync(async (barcode, cancellation) =>
            {
                if (string.IsNullOrEmpty(barcode)) return true;
                return await _unitOfWork.Products.IsBarcodeUniqueAsync(barcode);
            })
            .WithMessage("Bu barkod zaten kullanılmaktadır.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Alış fiyatı negatif olamaz.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı negatif olamaz.");

        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0).WithMessage("İskonto değeri negatif olamaz.")
            .Must((dto, value) =>
            {
                if (dto.DiscountType == DiscountType.Percentage)
                    return value <= 100;
                return true;
            }).WithMessage("Yüzde iskonto 100'den fazla olamaz.");

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok miktarı negatif olamaz.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Başlangıç stok miktarı negatif olamaz.");

        RuleFor(x => x.PurchaseVatRate)
            .IsInEnum().WithMessage("Geçerli bir KDV oranı seçiniz.");

        RuleFor(x => x.SaleVatRate)
            .IsInEnum().WithMessage("Geçerli bir KDV oranı seçiniz.");

        RuleFor(x => x.SpecialTaxValue)
            .GreaterThanOrEqualTo(0).WithMessage("ÖTV değeri negatif olamaz.")
            .When(x => x.SpecialTaxType != SpecialTaxType.None && x.SpecialTaxValue.HasValue);

        RuleFor(x => x.CommunicationTaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("ÖİV oranı negatif olamaz.")
            .LessThanOrEqualTo(100).WithMessage("ÖİV oranı 100'den fazla olamaz.")
            .When(x => x.CommunicationTaxRate.HasValue);
    }
}
