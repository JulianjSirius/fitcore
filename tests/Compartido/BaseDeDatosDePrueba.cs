using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FitCore.Tests.Compartido;

// Crea una base de datos temporal en el mismo servidor PostgreSQL que usa la app y la
// borra al terminar. Así las pruebas también comprueban la conexión real al servidor.
//
// La conexión se toma, en este orden, de:
//   1. la variable de entorno FITCORE_TESTS_POSTGRES (cadena de conexión sin Database), o
//   2. los User Secrets de la API (ConnectionStrings:DefaultConnection), cambiando el nombre de la base.
public sealed class BaseDeDatosDePrueba : IAsyncDisposable
{
    private readonly string _servidor;

    public string Nombre { get; }
    public string CadenaDeConexion { get; }

    private BaseDeDatosDePrueba(string servidor, string nombre)
    {
        _servidor = servidor;
        Nombre = nombre;
        CadenaDeConexion = new NpgsqlConnectionStringBuilder(servidor) { Database = nombre }.ConnectionString;
    }

    public static async Task<BaseDeDatosDePrueba> CrearAsync(string prefijo, string userSecretsIdDeLaApi)
    {
        var servidor = Environment.GetEnvironmentVariable("FITCORE_TESTS_POSTGRES")
            ?? new ConfigurationBuilder().AddUserSecrets(userSecretsIdDeLaApi).Build()
                .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No hay conexión a PostgreSQL para las pruebas. Define FITCORE_TESTS_POSTGRES " +
                "o configura ConnectionStrings:DefaultConnection en los user-secrets de la API.");

        var admin = new NpgsqlConnectionStringBuilder(servidor) { Database = "postgres", Pooling = false }.ConnectionString;
        var nombre = $"{prefijo}_{Guid.NewGuid():N}"[..40];

        await using var conexion = new NpgsqlConnection(admin);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombre}\"", conexion);
        await comando.ExecuteNonQueryAsync();

        return new BaseDeDatosDePrueba(admin, nombre);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var conexion = new NpgsqlConnection(_servidor);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Nombre}\" WITH (FORCE)", conexion);
        await comando.ExecuteNonQueryAsync();
    }
}
