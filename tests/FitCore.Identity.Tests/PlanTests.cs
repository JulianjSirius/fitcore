using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class PlanTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    [Fact]
    public async Task GET_es_publico_y_trae_precio_duracion_y_personas()
    {
        var response = await _e.Anonimo().GetAsync("/api/Plan");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);

        var duo = await _e.PlanAsync("Duo");
        Assert.Equal(2, duo.GetProperty("maxBeneficiarios").GetInt32());
        Assert.Equal(12, (await _e.PlanAsync("Anual")).GetProperty("duracionMeses").GetInt32());
        Assert.True(duo.GetProperty("precio").GetDecimal() > 0);
    }

    [Fact]
    public async Task PUT_como_administrador_cambia_el_precio_y_GET_lo_refleja()
    {
        var admin = await _e.AdministradorAsync("Recepcionista");
        var anual = await _e.PlanAsync("Anual");
        var id = anual.GetProperty("id").GetGuid();
        var precioOriginal = anual.GetProperty("precio").GetDecimal();

        try
        {
            var response = await _e.Como(admin).PutAsJsonAsync($"/api/Plan/{id}",
                new { nombre = "Anual", precio = 999_000m, duracionMeses = 12, activo = true });
            await Escenario.EsperarAsync(response, HttpStatusCode.OK);
            Assert.Equal(999_000m, (await _e.PlanAsync("Anual")).GetProperty("precio").GetDecimal());
        }
        finally
        {
            await _e.Como(admin).PutAsJsonAsync($"/api/Plan/{id}",
                new { nombre = "Anual", precio = precioOriginal, duracionMeses = 12, activo = true });
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(100_000, 0)]
    [InlineData(100_000, 25)]
    public async Task PUT_con_valores_invalidos_responde_400(decimal precio, int meses)
    {
        var admin = await _e.AdministradorAsync();
        var id = (await _e.PlanAsync("Mensual")).GetProperty("id").GetGuid();
        var response = await _e.Como(admin).PutAsJsonAsync($"/api/Plan/{id}",
            new { nombre = "Mensual", precio, duracionMeses = meses, activo = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PUT_de_plan_inexistente_responde_404()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Como(admin).PutAsJsonAsync($"/api/Plan/{Guid.NewGuid()}",
            new { nombre = "X", precio = 1000m, duracionMeses = 1, activo = true });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_sin_sesion_401_y_como_usuario_403()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        var id = (await _e.PlanAsync("Mensual")).GetProperty("id").GetGuid();
        var cambio = new { nombre = "Mensual", precio = 1m, duracionMeses = 1, activo = true };

        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().PutAsJsonAsync($"/api/Plan/{id}", cambio)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).PutAsJsonAsync($"/api/Plan/{id}", cambio)).StatusCode);
    }
}
