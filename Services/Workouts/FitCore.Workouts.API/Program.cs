using System.Text;
using FitCore.Workouts.Application;
using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Infrastructure.ExternalServices;
using FitCore.Workouts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

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
builder.Services.AddHealthChecks().AddDbContextCheck<WorkoutsDbContext>("base-de-datos");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FitCore Workouts API",
        Version = "v1",
        Description = "API para gestionar ejercicios y rutinas de entrenamiento."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Introduce el token JWT con el formato: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(FitCore.Workouts.Application.Features.Rutinas.Queries.GetRutinasQuery).Assembly));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<WorkoutsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IWorkoutsDbContext>(services => services.GetRequiredService<WorkoutsDbContext>());
builder.Services.AddHttpClient<IWgerExerciseService, WgerExerciseService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Wger:BaseUrl"] ?? "https://wger.de/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' (mínimo 32 bytes). Configúrala con 'dotnet user-secrets' o la variable de entorno Jwt__Key.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FitCore.Identity.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FitCore.Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
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
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutsDbContext>();
    await dbContext.Database.MigrateAsync();

    if (!await dbContext.Ejercicios.AnyAsync())
    {
        var wgerExerciseService = scope.ServiceProvider.GetRequiredService<IWgerExerciseService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await wgerExerciseService.PopulateIfEmptyAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo poblar la biblioteca desde Wger.");
        }
    }
}

app.Run();

// Visible para WebApplicationFactory en los proyectos de pruebas.
public partial class Program;
