using System.Security.Cryptography;
using FitCore.Identity.Infrastructure.Persistence;
using FitCore.Tests.Compartido;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Identity.Tests.Infraestructura;

// Levanta la API de Identity en memoria contra una base temporal en el servidor PostgreSQL de la app.
// Así cada prueba ejerce controlador → MediatR → EF Core → base de datos de verdad,
// incluidas las migraciones escritas a mano.
public sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string UserSecretsIdIdentity = "908ee7f7-9fb0-4aee-a9aa-3cc35ce935c0";
    private BaseDeDatosDePrueba? _baseDeDatos;

    public string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public string QrKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public async Task InitializeAsync()
    {
        _baseDeDatos = await BaseDeDatosDePrueba.CrearAsync("fitcore_identity_tests", UserSecretsIdIdentity);

        // Program.cs lee estos valores antes de construir la app, por eso van como
        // variables de entorno y no con ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _baseDeDatos.CadenaDeConexion);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("QrAcceso__Key", QrKey);

        // Fuerza el arranque (y las migraciones) antes de la primera prueba.
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");

    public async Task<T> EnBaseDeDatosAsync<T>(Func<AppDbContext, Task<T>> accion)
    {
        await using var scope = Services.CreateAsyncScope();
        return await accion(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task EnBaseDeDatosAsync(Func<AppDbContext, Task> accion)
        => EnBaseDeDatosAsync(async db => { await accion(db); return true; });

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (_baseDeDatos is not null)
        {
            await _baseDeDatos.DisposeAsync();
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class IdentityCollection : ICollectionFixture<IdentityApiFactory>
{
    public const string Nombre = "identity";
}
