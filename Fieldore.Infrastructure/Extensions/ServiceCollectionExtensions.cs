using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Customers.Contracts;
using Fieldore.Application.Estimates.Contracts;
using Fieldore.Application.Expenses.Contracts;
using Fieldore.Application.Invoices.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Fieldore.Application.Locations.Contracts;
using Fieldore.Application.Payments.Contracts;
using Fieldore.Application.ServiceCatalog.Contracts;
using Fieldore.Application.Stripe.Contracts;
using Fieldore.Application.Workers.Contracts;
using Fieldore.Infrastructure.Auth;
using Fieldore.Infrastructure.Customers;
using Fieldore.Infrastructure.Data;
using Fieldore.Infrastructure.Estimates;
using Fieldore.Infrastructure.Expenses;
using Fieldore.Infrastructure.Invoices;
using Fieldore.Infrastructure.Jobs;
using Fieldore.Infrastructure.Locations;
using Fieldore.Infrastructure.Payments;
using Fieldore.Infrastructure.ServiceCatalog;
using Fieldore.Infrastructure.Stripe;
using Fieldore.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fieldore.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("SqlServer")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("SQL Server connection string is missing.");

        services.AddDbContext<FieldoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IEstimateService, EstimateService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<ILocationLookupService, LocationLookupService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
