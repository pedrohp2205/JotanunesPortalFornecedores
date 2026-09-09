using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Jotanunes.Infra.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jotanunes.Infra.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection service, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE")
            ?? configuration.GetConnectionString("ConnectionString");

        service.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        service.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        service.AddScoped<IUnitOfWork, UnitOfWork>();

        service.AddScoped<ICompanyService, CompanyService>();

        return service;
    }
}
