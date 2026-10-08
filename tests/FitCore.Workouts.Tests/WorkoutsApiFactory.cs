using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FitCore.Tests.Compartido;
using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Domain.Entidades;
using FitCore.Workouts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace FitCore.Workouts.Tests;

// API de Workouts en memoria contra una base temporal del servidor PostgreSQL de la app. Wger (servicio externo)
// se reemplaza por una semilla fija para no depender de internet.
public sealed class WorkoutsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string UserSecretsIdWorkouts = "e0d440b2-2b9c-4190-97e5-16cf868effac";
    private BaseDeDatosDePrueba? _baseDeDatos;

    private readonly string _jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public async Task InitializeAsync()
    {
        _baseDeDatos = await BaseDeDatosDePrueba.CrearAsync("fitcore_workouts_tests", UserSecretsIdWorkouts);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _baseDeDatos.CadenaDeConexion);
        Environment.SetEnvironmentVariable("Jwt__Key", _jwtKey);
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWgerExerciseService>();
            services.AddScoped<IWgerExerciseService, WgerSemillaFija>();
        });
    }

    // Token con la misma forma que emite Identity (sub, email, Rol), firmado con la clave de prueba.
    public string Token(string rol, Guid? id = null)
    {
        var token = new JwtSecurityToken(
            issuer: "FitCore.Identity.API",
            audience: "FitCore.Clients",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, (id ?? Guid.NewGuid()).ToString()),
                new Claim(JwtRegisteredClaimNames.Email, $"{rol.ToLowerInvariant()}@test.fitcore"),
                new Claim("Rol", rol)
            ],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpClient Como(string rol, Guid? id = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(rol, id));
        return client;
    }

    public async Task<T> EnBaseDeDatosAsync<T>(Func<WorkoutsDbContext, Task<T>> accion)
    {
        await using var scope = Services.CreateAsyncScope();
        return await accion(scope.ServiceProvider.GetRequiredService<WorkoutsDbContext>());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (_baseDeDatos is not null)
        {
            await _baseDeDatos.DisposeAsync();
        }
    }

    private sealed class WgerSemillaFija(IWorkoutsDbContext context) : IWgerExerciseService
    {
        public async Task<int> PopulateIfEmptyAsync(CancellationToken cancellationToken = default)
        {
            context.Ejercicios.Add(new Ejercicio { Id = Guid.NewGuid(), Nombre = "Sentadilla", GrupoMuscular = "Piernas", DescripcionOrientativa = "Semilla de prueba" });
            context.Ejercicios.Add(new Ejercicio { Id = Guid.NewGuid(), Nombre = "Press banca", GrupoMuscular = "Pecho", DescripcionOrientativa = "Semilla de prueba" });
            return await context.SaveChangesAsync(cancellationToken);
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class WorkoutsCollection : ICollectionFixture<WorkoutsApiFactory>
{
    public const string Nombre = "workouts";
}
