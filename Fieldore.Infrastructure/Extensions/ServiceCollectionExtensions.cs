using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Customers.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Fieldore.Application.Locations.Contracts;
using Fieldore.Infrastructure.Auth;
using Fieldore.Infrastructure.Customers;
using Fieldore.Infrastructure.Data;
using Fieldore.Infrastructure.Jobs;
using Fieldore.Infrastructure.Locations;
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
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<ILocationLookupService, LocationLookupService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
