using CombinationsGenerator.Application.SessionStore;
using CombinationsGenerator.Application.Services;
using CombinationsGenerator.Domain;
using CombinationsGenerator.Infrastructure.SessionStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CombinationsGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCombinationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SessionStoreOptions>(configuration.GetSection("SessionStore"));
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<IPermutationGenerator, PermutationGenerator>();
        services.AddScoped<IStartService, StartService>();
        services.AddScoped<INextService, NextService>();
        services.AddScoped<IAllService, AllService>();
        services.AddScoped<ICurrentService, CurrentService>();
        services.AddScoped<IResetService, ResetService>();
        return services;
    }
}
