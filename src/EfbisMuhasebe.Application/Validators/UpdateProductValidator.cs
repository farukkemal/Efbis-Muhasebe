using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using FluentValidation;

namespace EfbisMuhasebe.Application.Validators;

/// <summary>Ürün güncelleme validasyonları</summary>
public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir ürün ID giriniz.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Ürün kodu zorunludur.")
            .MaximumLength(50).WithMessage("Ürün kodu en fazla 50 karakter olabilir.")
            .MustAsync(async (dto, code, cancellation) =>
                await _unitOfWork.Products.IsProductCodeUniqueAsync(code, dto.Id))
            .WithMessage("Bu ürün kodu zaten kullanılmaktadır.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barkod en fazla 100 karakter olabilir.")
            .MustAsync(async (dto, barcode, cancellation) =>
            {
                if (string.IsNullOrEmpty(barcode)) return true;
                return await _unitOfWork.Products.IsBarcodeUniqueAsync(barcode, dto.Id);
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
    }
}
