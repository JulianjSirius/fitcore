using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Domain;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class UsuarioTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private static object Registro(string codigo, string? email = null) => new
    {
        firstName = "Sara",
        lastName = "Registro",
        email = email ?? Escenario.EmailUnico("registro"),
        contrasena = Escenario.Contrasena,
        telefono = Escenario.Telefono,
        codigoRegistro = codigo
    };

    [Fact]
    public async Task POST_sin_codigo_responde_400()
    {
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Usuario", new
        {
            firstName = "Sara", lastName = "Sin", email = Escenario.EmailUnico("sin-codigo"),
            contrasena = Escenario.Contrasena, telefono = Escenario.Telefono
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_con_codigo_inexistente_responde_400_con_mensaje()
    {
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro("NOEXISTE22"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("código de registro", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task POST_con_codigo_valido_crea_201_y_marca_el_codigo_como_usado()
    {
        var admin = await _e.AdministradorAsync();
        var codigo = await _e.CodigoRegistroAsync(admin);

        var response = await _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro(codigo.ToLowerInvariant()));
        await Escenario.EsperarAsync(response, HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var usadoPor = await factory.EnBaseDeDatosAsync(db =>
            db.CodigosRegistro.Where(c => c.Codigo == codigo).Select(c => c.UsadoPorUsuarioId).SingleAsync());
        Assert.Equal(id, usadoPor);

        var contrasena = await factory.EnBaseDeDatosAsync(db =>
            db.Users.Where(u => u.Id == id).Select(u => u.Contrasena).SingleAsync());
        Assert.StartsWith("PBKDF2-SHA256$", contrasena);
    }

    [Fact]
    public async Task Un_codigo_no_se_puede_usar_dos_veces()
    {
        var admin = await _e.AdministradorAsync();
        var codigo = await _e.CodigoRegistroAsync(admin);

        await Escenario.EsperarAsync(await _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro(codigo)), HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro(codigo))).StatusCode);
    }

    [Fact]
    public async Task Dos_registros_simultaneos_con_el_mismo_codigo_solo_crean_uno()
    {
        var admin = await _e.AdministradorAsync();
        var codigo = await _e.CodigoRegistroAsync(admin);

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro(codigo))));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task Un_codigo_vencido_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        const string codigo = "VENCIDO234";
        await factory.EnBaseDeDatosAsync(async db =>
        {
            db.CodigosRegistro.Add(new CodigoRegistro
            {
                Id = Guid.NewGuid(), Codigo = codigo, CreadoPorAdministradorId = admin.Id,
                FechaCreacion = DateTime.UtcNow.AddDays(-10), FechaExpiracion = DateTime.UtcNow.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.BadRequest, (await _e.Anonimo().PostAsJsonAsync("/api/Usuario", Registro(codigo))).StatusCode);
    }

    [Fact]
    public async Task GET_lista_como_admin_y_entrenador_200_como_usuario_403_sin_sesion_401()
    {
        var admin = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var usuario = await _e.UsuarioAsync(admin);

        var comoAdmin = await _e.Como(admin).GetAsync("/api/Usuario");
        await Escenario.EsperarAsync(comoAdmin, HttpStatusCode.OK);
        var lista = await comoAdmin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(lista.EnumerateArray(), u => u.GetProperty("id").GetGuid() == usuario.Id);
        Assert.All(lista.EnumerateArray(), u => Assert.False(u.TryGetProperty("contrasena", out _)));

        await Escenario.EsperarAsync(await _e.Como(entrenador).GetAsync("/api/Usuario"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/Usuario")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().GetAsync("/api/Usuario")).StatusCode);
    }

    [Fact]
    public async Task GET_por_id_propio_200_otro_usuario_403_inexistente_404()
    {
        var admin = await _e.AdministradorAsync();
        var ana = await _e.UsuarioAsync(admin);
        var beto = await _e.UsuarioAsync(admin);

        await Escenario.EsperarAsync(await _e.Como(ana).GetAsync($"/api/Usuario/{ana.Id}"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(ana).GetAsync($"/api/Usuario/{beto.Id}")).StatusCode);
        await Escenario.EsperarAsync(await _e.Como(admin).GetAsync($"/api/Usuario/{beto.Id}"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.NotFound, (await _e.Como(admin).GetAsync($"/api/Usuario/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task PUT_como_Dueno_204_como_usuario_403()
    {
        var dueno = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(dueno);
        var cambios = new
        {
            firstName = "Sara", lastName = "Editada", email = usuario.Email,
            contrasena = "Otra-Clave-789", telefono = Escenario.Telefono
        };

        await Escenario.EsperarAsync(await _e.Como(dueno).PutAsJsonAsync($"/api/Usuario/{usuario.Id}", cambios), HttpStatusCode.NoContent);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).PutAsJsonAsync($"/api/Usuario/{usuario.Id}", cambios)).StatusCode);
        Assert.False(string.IsNullOrEmpty(await _e.LoginAsync("usuario", usuario.Email, "Otra-Clave-789")));
    }
}
