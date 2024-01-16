using Microsoft.Extensions.DependencyInjection;
using Wam.Users.Repositories;
using Wam.Users.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWamUsersModule(this IServiceCollection services)
    {
        services.AddTransient<IUsersService, UsersService>();
        services.AddTransient<IUsersRepository, UsersRepository>();
        return services;
    }
}