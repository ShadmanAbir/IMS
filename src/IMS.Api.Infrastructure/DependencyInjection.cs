using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Infrastructure.Mapping;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Domain.Entities;

namespace IMS.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string? connectionString = null)
    {
        // AutoMapper profiles
        services.AddAutoMapper(cfg => {
            cfg.AddProfile(new DomainProfile());
        });

        // Http context
        services.AddHttpContextAccessor();

        // Tenant context
        services.AddScoped<ITenantContext, Services.TenantContext>();

        // Token service and authentication
        services.AddScoped<IMS.Api.Application.Common.Interfaces.ITokenService, Services.TokenService>();
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IAuthenticationService, Services.AuthenticationService>();

        // Audit
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IAuditService, Services.AuditService>();
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IAuditRepository, Repositories.AuditRepository>();

        // Inventory and dashboard repositories
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IInventoryRepository, Repositories.InventoryRepository>();
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IDashboardRepository, Repositories.DashboardRepository>();

        // Unit of work (Dapper variant)
        services.AddScoped<IMS.Api.Application.Common.Interfaces.IUnitOfWork, Data.DapperUnitOfWork>();

        // Register IDbConnectionFactory if connection string provided
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IDbConnectionFactory>(sp => new Data.PostgreSqlConnectionFactory(connectionString));
        }

        // Identity configuration when ApplicationDbContext is registered
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        }).AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();

        // Other infrastructure services can be registered here

        return services;
    }
}
