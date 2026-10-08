using System.Text;
using FitCore.Identity.Application;
using FitCore.Identity.API.Desarrollo;
using FitCore.Identity.API.Jobs;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Infrastructure.Persistence;
using FitCore.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
// /health responde 200 solo si la API puede consultar su base de datos.
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("base-de-datos");
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IAppDbContext).Assembly));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAppDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<MembresiaService>();
builder.Services.AddHostedService<VencimientoMembresiasWorker>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' (mínimo 32 bytes). Configúrala con 'dotnet user-secrets' o la variable de entorno Jwt__Key.");
}
var qrKey = builder.Configuration["QrAcceso:Key"];
if (string.IsNullOrWhiteSpace(qrKey) || Encoding.UTF8.GetByteCount(qrKey) < 32 || qrKey == jwtKey)
{
    throw new InvalidOperationException(
        "Falta 'QrAcceso:Key' (mínimo 32 bytes y distinta de Jwt:Key). Configúrala con 'dotnet user-secrets'.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FitCore.Identity.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FitCore.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = "Rol",
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdministrador", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrador");
        policy.RequireClaim("NivelAcceso", "Dueño");
    });
});

var app = builder.Build();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue<bool>("SemillaPostman:Habilitada"))
    {
        await SemillaDesarrollo.AsegurarDuenoPostmanAsync(
            db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
    }
}

app.Run();

// Visible para WebApplicationFactory en los proyectos de pruebas.
public partial class Program;
