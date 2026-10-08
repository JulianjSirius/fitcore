using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Identity.Tests.Infraestructura;

public sealed record Cuenta(Guid Id, string Email, string Contrasena, string Token);

// Atajos para armar el estado que necesita cada prueba usando la API real.
public sealed class Escenario(IdentityApiFactory factory)
{
    public const string Contrasena = "Clave-Segura-123";
    public const long Telefono = 3_001_234_567;

    public HttpClient Anonimo() => factory.CreateClient();

    public HttpClient Como(Cuenta cuenta) => Como(cuenta.Token);

    public HttpClient Como(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string EmailUnico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}@test.fitcore";

    // Los administradores no se pueden crear de forma anónima, así que el primero
    // (el Dueño) se inserta directo en la base, como existe en producción.
    public async Task<Cuenta> AdministradorAsync(string nivelAcceso = "Dueño", bool contrasenaEnTextoPlano = false)
    {
        var email = EmailUnico("admin");
        var id = Guid.NewGuid();
        await factory.EnBaseDeDatosAsync(async db =>
        {
            var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
            db.Administradores.Add(new Administrador
            {
                Id = id,
                Nombre = "Admin",
                LastName = "Pruebas",
                Email = email,
                Telefono = Telefono,
                Contrasena = contrasenaEnTextoPlano ? Contrasena : hasher.Hash(Contrasena),
                NivelAcceso = nivelAcceso
            });
            await db.SaveChangesAsync();
        });

        return new Cuenta(id, email, Contrasena, await LoginAsync("administrador", email, Contrasena));
    }

    public async Task<string> CodigoRegistroAsync(Cuenta admin, int diasVigencia = 7)
    {
        var response = await Como(admin).PostAsJsonAsync("/api/CodigoRegistro", new { diasVigencia });
        await EsperarAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString()!;
    }

    public async Task<Cuenta> UsuarioAsync(Cuenta admin, string? email = null)
    {
        email ??= EmailUnico("usuario");
        var codigo = await CodigoRegistroAsync(admin);
        var response = await Anonimo().PostAsJsonAsync("/api/Usuario", new
        {
            firstName = "Ana",
            lastName = "Pruebas",
            email,
            contrasena = Contrasena,
            telefono = Telefono,
            codigoRegistro = codigo
        });
        await EsperarAsync(response, HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return new Cuenta(id, email, Contrasena, await LoginAsync("usuario", email, Contrasena));
    }

    public async Task<Cuenta> EntrenadorAsync(Cuenta admin)
    {
        var email = EmailUnico("entrenador");
        var response = await Como(admin).PostAsJsonAsync("/api/Entrenador", new
        {
            nombre = "Leo",
            especialidad = "Fuerza",
            horario = "Lunes a viernes",
            email,
            contrasena = Contrasena,
            telefono = Telefono
        });
        await EsperarAsync(response, HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return new Cuenta(id, email, Contrasena, await LoginAsync("entrenador", email, Contrasena));
    }

    public async Task<string> LoginAsync(string tipo, string email, string contrasena)
    {
        var response = await Anonimo().PostAsJsonAsync($"/api/Auth/login/{tipo}", new { email, contrasena });
        await EsperarAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    public async Task<JsonElement> PlanAsync(string codigo)
    {
        var planes = await Anonimo().GetFromJsonAsync<JsonElement>("/api/Plan");
        return planes.EnumerateArray().Single(p => p.GetProperty("codigo").GetString() == codigo);
    }

    public async Task<HttpResponseMessage> RegistrarPagoAsync(
        Cuenta admin, Guid usuarioId, string planCodigo, string? acompananteEmail = null, string metodo = "Efectivo")
    {
        var plan = await PlanAsync(planCodigo);
        return await Como(admin).PostAsJsonAsync("/api/Membresia/pagos", new
        {
            usuarioId,
            planId = plan.GetProperty("id").GetGuid(),
            metodo,
            acompananteEmail
        });
    }

    // Falla con el cuerpo de la respuesta a la vista, para que el error se entienda.
    public static async Task EsperarAsync(HttpResponseMessage response, HttpStatusCode esperado)
    {
        if (response.StatusCode != esperado)
        {
            var cuerpo = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: " +
                $"se esperaba {(int)esperado} {esperado} y llegó {(int)response.StatusCode} {response.StatusCode}. Cuerpo: {cuerpo}");
        }
    }
}
