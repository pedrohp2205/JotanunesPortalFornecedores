using System.Text;
using Jotanunes.Infra.Security.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Jotanunes.Infra.IoC;

// Autenticação JWT usada apenas pela frente externa (Portal do Fornecedor).
// A frente interna roda sem autenticação por enquanto.
public static class DependencyInjectionAuth
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection service, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Seção de configuração 'Jwt' não encontrada.");

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey é obrigatória e deve ter ao menos 32 caracteres.");
        }

        service.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        service.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        service.AddAuthorization();

        return service;
    }
}
