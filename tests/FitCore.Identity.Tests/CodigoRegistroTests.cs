using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class CodigoRegistroTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    [Fact]
    public async Task POST_como_administrador_genera_codigo_de_10_caracteres()
    {
        var admin = await _e.AdministradorAsync("Recepcionista");
        var response = await _e.Como(admin).PostAsJsonAsync("/api/CodigoRegistro", new { diasVigencia = 3 });

        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Matches("^[A-HJ-NP-Z2-9]{10}$", body.GetProperty("codigo").GetString());
        Assert.Equal(admin.Id, body.GetProperty("creadoPorAdministradorId").GetGuid());

        var vence = body.GetProperty("fechaExpiracion").GetDateTime();
        Assert.InRange(vence, DateTime.UtcNow.AddDays(3).AddMinutes(-1), DateTime.UtcNow.AddDays(3).AddMinutes(1));
    }

    [Fact]
    public async Task POST_sin_cuerpo_usa_7_dias_por_defecto()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Como(admin).PostAsJsonAsync("/api/CodigoRegistro", new { });
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var vence = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fechaExpiracion").GetDateTime();
        Assert.InRange(vence, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public async Task POST_con_vigencia_fuera_de_rango_responde_400(int dias)
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Como(admin).PostAsJsonAsync("/api/CodigoRegistro", new { diasVigencia = dias });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_sin_sesion_401_y_como_usuario_o_entrenador_403()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        var entrenador = await _e.EntrenadorAsync(admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().PostAsJsonAsync("/api/CodigoRegistro", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).PostAsJsonAsync("/api/CodigoRegistro", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(entrenador).PostAsJsonAsync("/api/CodigoRegistro", new { })).StatusCode);
    }

    [Fact]
    public async Task GET_lista_incluye_el_codigo_generado_y_quien_lo_uso()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin); // consume un código
        var libre = await _e.CodigoRegistroAsync(admin);

        var response = await _e.Como(admin).GetAsync("/api/CodigoRegistro");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var codigos = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();

        Assert.Contains(codigos, c => c.GetProperty("codigo").GetString() == libre
            && c.GetProperty("usadoPorUsuarioId").ValueKind == JsonValueKind.Null);
        Assert.Contains(codigos, c => c.GetProperty("usadoPorUsuarioId").ValueKind == JsonValueKind.String
            && c.GetProperty("usadoPorUsuarioId").GetGuid() == usuario.Id);
    }

    [Fact]
    public async Task GET_lista_como_usuario_responde_403()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/CodigoRegistro")).StatusCode);
    }
}
