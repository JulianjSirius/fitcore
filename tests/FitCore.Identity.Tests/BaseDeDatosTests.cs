using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Tests;

// Conexión API ↔ base de datos: migraciones, modelo y endpoint de salud.
[Collection(IdentityCollection.Nombre)]
public sealed class BaseDeDatosTests(IdentityApiFactory factory)
{
    [Fact]
    public async Task Todas_las_migraciones_quedan_aplicadas()
    {
        var pendientes = await factory.EnBaseDeDatosAsync(db => db.Database.GetPendingMigrationsAsync());
        Assert.Empty(pendientes);
    }

    [Fact]
    public async Task El_modelo_coincide_con_las_migraciones()
    {
        // Si falla, a una migración (por ejemplo, una escrita a mano) le falta algo del modelo.
        var hayCambios = await factory.EnBaseDeDatosAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));
        Assert.False(hayCambios, "El modelo de EF tiene cambios que ninguna migración refleja.");
    }

    [Fact]
    public async Task Health_responde_200_cuando_la_base_responde()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_migracion_siembra_los_tres_planes()
    {
        var planes = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/Plan");
        var codigos = planes.EnumerateArray().Select(p => p.GetProperty("codigo").GetString()).Order();
        Assert.Equal(["Anual", "Duo", "Mensual"], codigos);
    }
}
