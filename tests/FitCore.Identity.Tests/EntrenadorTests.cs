using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class EntrenadorTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private static object NuevoEntrenador(string? email = null) => new
    {
        nombre = "Leo",
        especialidad = "Movilidad",
        horario = "Mañanas",
        email = email ?? Escenario.EmailUnico("entrenador"),
        contrasena = Escenario.Contrasena,
        telefono = Escenario.Telefono
    };

    [Fact]
    public async Task POST_sin_sesion_responde_401()
        => Assert.Equal(HttpStatusCode.Unauthorized,
            (await _e.Anonimo().PostAsJsonAsync("/api/Entrenador", NuevoEntrenador())).StatusCode);

    [Fact]
    public async Task POST_como_usuario_responde_403()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _e.Como(usuario).PostAsJsonAsync("/api/Entrenador", NuevoEntrenador())).StatusCode);
    }

    [Fact]
    public async Task POST_como_administrador_crea_201()
    {
        var admin = await _e.AdministradorAsync("Recepcionista");
        var response = await _e.Como(admin).PostAsJsonAsync("/api/Entrenador", NuevoEntrenador());
        await Escenario.EsperarAsync(response, HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_con_telefono_invalido_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Como(admin).PostAsJsonAsync("/api/Entrenador", new
        {
            nombre = "Leo", especialidad = "Fuerza", horario = "Tardes",
            email = Escenario.EmailUnico("tel"), contrasena = Escenario.Contrasena, telefono = 123
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_lista_como_entrenador_200_como_usuario_403()
    {
        var admin = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var usuario = await _e.UsuarioAsync(admin);

        var lista = await _e.Como(entrenador).GetAsync("/api/Entrenador");
        await Escenario.EsperarAsync(lista, HttpStatusCode.OK);
        Assert.Contains((await lista.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == entrenador.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/Entrenador")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().GetAsync("/api/Entrenador")).StatusCode);
    }

    [Fact]
    public async Task GET_por_id_propio_200_y_de_un_usuario_403()
    {
        var admin = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var usuario = await _e.UsuarioAsync(admin);

        await Escenario.EsperarAsync(await _e.Como(entrenador).GetAsync($"/api/Entrenador/{entrenador.Id}"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync($"/api/Entrenador/{entrenador.Id}")).StatusCode);
    }

    [Fact]
    public async Task PUT_como_Dueno_204_e_inexistente_404()
    {
        var dueno = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(dueno);

        var ok = await _e.Como(dueno).PutAsJsonAsync($"/api/Entrenador/{entrenador.Id}", NuevoEntrenador(entrenador.Email));
        await Escenario.EsperarAsync(ok, HttpStatusCode.NoContent);

        var noExiste = await _e.Como(dueno).PutAsJsonAsync($"/api/Entrenador/{Guid.NewGuid()}", NuevoEntrenador());
        Assert.Equal(HttpStatusCode.NotFound, noExiste.StatusCode);
    }

    [Fact]
    public async Task PUT_como_entrenador_responde_403()
    {
        var admin = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var response = await _e.Como(entrenador).PutAsJsonAsync($"/api/Entrenador/{entrenador.Id}", NuevoEntrenador(entrenador.Email));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
