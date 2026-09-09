using Jotanunes.API.Shared.Middlewares;
using Jotanunes.Infra.IoC;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// A frente interna é a dona do schema, então é ela que aplica as migrations.
builder.Services.ApplyPendingMigrations();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Jotanunes - Back-office (Interno)",
        Version = "v1",
        Description = "API da frente interna, usada pela equipe da Jotanunes. Sem autenticação nesta etapa."
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
