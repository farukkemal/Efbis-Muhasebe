using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Application.Mappings;
using EfbisMuhasebe.Application.Services;
using EfbisMuhasebe.Application.Validators;
using EfbisMuhasebe.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EfbisMuhasebe.Application;

/// <summary>
/// Application katmanı DI kayıtları.
/// Web projesinde tek satırla çağrılır: services.AddApplicationServices();
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);

        // FluentValidation
        services.AddScoped<IValidator<CreateProductDto>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductDto>, UpdateProductValidator>();

        // Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISalesProductService, SalesProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStockTransactionService, StockTransactionService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ICashAccountService, CashAccountService>();
        services.AddScoped<IIncomeExpenseService, IncomeExpenseService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ISalaryPaymentService, SalaryPaymentService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
