using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class AdministradorTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private static object NuevoAdmin(string nivel = "Recepcionista", string? email = null) => new
    {
        nombre = "Rita",
        lastName = "Recepcion",
        email = email ?? Escenario.EmailUnico("nuevo-admin"),
        contrasena = Escenario.Contrasena,
        telefono = Escenario.Telefono,
        nivelAcceso = nivel
    };

    [Fact]
    public async Task POST_sin_sesion_responde_401()
    {
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Administrador", NuevoAdmin("Dueño"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_como_administrador_que_no_es_Dueno_responde_403()
    {
        var recepcion = await _e.AdministradorAsync("Recepcionista");
        var response = await _e.Como(recepcion).PostAsJsonAsync("/api/Administrador", NuevoAdmin());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_como_usuario_responde_403()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        var response = await _e.Como(usuario).PostAsJsonAsync("/api/Administrador", NuevoAdmin());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_como_Dueno_crea_y_el_nuevo_puede_entrar()
    {
        var dueno = await _e.AdministradorAsync();
        var email = Escenario.EmailUnico("creado");
        var response = await _e.Como(dueno).PostAsJsonAsync("/api/Administrador", NuevoAdmin(email: email));

        await Escenario.EsperarAsync(response, HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var get = await _e.Como(dueno).GetAsync($"/api/Administrador/{id}");
        await Escenario.EsperarAsync(get, HttpStatusCode.OK);
        Assert.False(string.IsNullOrEmpty(await _e.LoginAsync("administrador", email, Escenario.Contrasena)));
    }

    [Fact]
    public async Task GET_lista_como_administrador_200_y_como_usuario_403()
    {
        var admin = await _e.AdministradorAsync("Recepcionista");
        var usuario = await _e.UsuarioAsync(admin);

        var lista = await _e.Como(admin).GetAsync("/api/Administrador");
        await Escenario.EsperarAsync(lista, HttpStatusCode.OK);
        var items = await lista.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(items.EnumerateArray(), a => a.GetProperty("id").GetGuid() == admin.Id);
        Assert.All(items.EnumerateArray(), a => Assert.False(a.TryGetProperty("contrasena", out _)));

        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/Administrador")).StatusCode);
    }

    [Fact]
    public async Task GET_por_id_inexistente_responde_404()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Como(admin).GetAsync($"/api/Administrador/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_como_Dueno_actualiza_y_la_nueva_contrasena_funciona()
    {
        var dueno = await _e.AdministradorAsync();
        var otro = await _e.AdministradorAsync("Recepcionista");
        var response = await _e.Como(dueno).PutAsJsonAsync($"/api/Administrador/{otro.Id}", new
        {
            nombre = "Rita",
            lastName = "Cambiada",
            email = otro.Email,
            contrasena = "Nueva-Clave-456",
            telefono = Escenario.Telefono,
            nivelAcceso = "Contador"
        });

        await Escenario.EsperarAsync(response, HttpStatusCode.NoContent);
        Assert.False(string.IsNullOrEmpty(await _e.LoginAsync("administrador", otro.Email, "Nueva-Clave-456")));
    }

    [Fact]
    public async Task DELETE_como_Dueno_borra_y_luego_responde_404()
    {
        var dueno = await _e.AdministradorAsync();
        var otro = await _e.AdministradorAsync("Contador");

        await Escenario.EsperarAsync(await _e.Como(dueno).DeleteAsync($"/api/Administrador/{otro.Id}"), HttpStatusCode.NoContent);
        Assert.Equal(HttpStatusCode.NotFound, (await _e.Como(dueno).GetAsync($"/api/Administrador/{otro.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _e.Como(dueno).DeleteAsync($"/api/Administrador/{otro.Id}")).StatusCode);
    }

    [Fact]
    public async Task DELETE_como_administrador_que_no_es_Dueno_responde_403()
    {
        var recepcion = await _e.AdministradorAsync("Recepcionista");
        var otro = await _e.AdministradorAsync("Contador");
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(recepcion).DeleteAsync($"/api/Administrador/{otro.Id}")).StatusCode);
    }
}
