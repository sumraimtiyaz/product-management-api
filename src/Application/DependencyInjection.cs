using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;

namespace ProductManagement.Application;

public static class DependencyInjection
{
    /// <summary>Registers the application services and request validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<IProductService>(includeInternalTypes: false);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
