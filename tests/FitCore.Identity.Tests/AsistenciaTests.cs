using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Application.Features.Logros;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Tests;

// Asistencias registradas al validar el QR y GET /api/Usuario/me/asistencia.
[Collection(IdentityCollection.Nombre)]
public sealed class AsistenciaTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private async Task<(Cuenta Admin, Cuenta Usuario)> UsuarioConMembresiaAsync()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual"), HttpStatusCode.OK);
        return (admin, usuario);
    }

    private async Task ValidarEntradaAsync(Cuenta recepcion, Cuenta usuario)
    {
        var qr = await _e.Como(usuario).GetFromJsonAsync<JsonElement>("/api/Usuario/me/qr-token");
        var response = await _e.Como(recepcion).PostAsJsonAsync("/api/Acceso/validar", new { token = qr.GetProperty("token").GetString() });
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
    }

    private static JsonElement Logro(JsonElement lista, string codigo)
        => lista.EnumerateArray().Single(l => l.GetProperty("codigo").GetString() == codigo);

    [Fact]
    public async Task Sin_asistencias_todo_en_cero()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        var resumen = await _e.Como(usuario).GetFromJsonAsync<JsonElement>("/api/Usuario/me/asistencia");

        Assert.Equal(0, resumen.GetProperty("rachaActual").GetInt32());
        Assert.Equal(0, resumen.GetProperty("totalAsistencias").GetInt32());
        Assert.False(Logro(resumen.GetProperty("insignias"), "primera-visita").GetProperty("obtenido").GetBoolean());
    }

    [Fact]
    public async Task Entrar_dos_veces_el_mismo_dia_cuenta_una_asistencia()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        await ValidarEntradaAsync(admin, usuario);
        await ValidarEntradaAsync(admin, usuario);

        Assert.Equal(1, await factory.EnBaseDeDatosAsync(db => db.Asistencias.CountAsync(a => a.UsuarioId == usuario.Id)));

        var resumen = await _e.Como(usuario).GetFromJsonAsync<JsonElement>("/api/Usuario/me/asistencia");
        Assert.Equal(1, resumen.GetProperty("rachaActual").GetInt32());
        Assert.Equal(1, resumen.GetProperty("totalAsistencias").GetInt32());
        Assert.Equal(CalendarioGimnasio.Hoy().ToString("yyyy-MM-dd"), resumen.GetProperty("ultimaAsistencia").GetString());
        var primera = Logro(resumen.GetProperty("insignias"), "primera-visita");
        Assert.True(primera.GetProperty("obtenido").GetBoolean());
        Assert.Equal(1, Logro(resumen.GetProperty("retos"), "reto-mes-asistencia").GetProperty("progreso").GetInt32());
    }

    [Fact]
    public async Task Acceso_denegado_no_registra_asistencia()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        var qr = await _e.Como(usuario).GetFromJsonAsync<JsonElement>("/api/Usuario/me/qr-token");
        // La membresía vence dentro del minuto de vida del QR.
        await factory.EnBaseDeDatosAsync(db => db.Membresias
            .Where(m => m.UsuarioTitularId == usuario.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.FechaVencimiento, DateTime.UtcNow.AddMinutes(-5))));

        var response = await _e.Como(admin).PostAsJsonAsync("/api/Acceso/validar", new { token = qr.GetProperty("token").GetString() });
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("permitido").GetBoolean());
        Assert.Equal(0, await factory.EnBaseDeDatosAsync(db => db.Asistencias.CountAsync(a => a.UsuarioId == usuario.Id)));
    }

    [Fact]
    public async Task Solo_el_rol_Usuario_consulta_su_asistencia()
    {
        var admin = await _e.AdministradorAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(admin).GetAsync("/api/Usuario/me/asistencia")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().GetAsync("/api/Usuario/me/asistencia")).StatusCode);
    }
}

// Cálculo puro de rachas, insignias y reto mensual.
public sealed class CalculadoraAsistenciaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 20);

    private static DateOnly[] Dias(params int[] haceDias) => haceDias.Select(d => Hoy.AddDays(-d)).ToArray();

    [Fact]
    public void Dias_consecutivos_hasta_hoy_forman_la_racha_actual()
    {
        var r = CalculadoraAsistencia.Calcular(Dias(2, 1, 0), Hoy);
        Assert.Equal(3, r.RachaActual);
        Assert.Equal(3, r.MejorRacha);
    }

    [Fact]
    public void Si_aun_no_viene_hoy_la_racha_de_ayer_sigue_viva()
        => Assert.Equal(2, CalculadoraAsistencia.Calcular(Dias(2, 1), Hoy).RachaActual);

    [Fact]
    public void Un_dia_sin_venir_rompe_la_racha_pero_se_conserva_la_mejor()
    {
        var r = CalculadoraAsistencia.Calcular(Dias(10, 9, 8, 7, 5), Hoy);
        Assert.Equal(0, r.RachaActual);
        Assert.Equal(4, r.MejorRacha);
    }

    [Fact]
    public void Racha_de_7_dias_se_fecha_el_dia_que_se_alcanzo()
    {
        var r = CalculadoraAsistencia.Calcular(Dias(9, 8, 7, 6, 5, 4, 3, 2), Hoy);
        var racha7 = r.Insignias.Single(i => i.Codigo == "racha-7");
        Assert.True(racha7.Obtenido);
        Assert.Equal(Hoy.AddDays(-3), racha7.FechaObtencion);
        Assert.False(r.Insignias.Single(i => i.Codigo == "racha-30").Obtenido);
        Assert.Equal(8, r.Insignias.Single(i => i.Codigo == "racha-30").Progreso);
    }

    [Fact]
    public void El_reto_mensual_solo_cuenta_el_mes_actual()
    {
        // 20 de octubre: 25 días atrás cae en septiembre.
        var r = CalculadoraAsistencia.Calcular(Dias(25, 3, 1), Hoy);
        Assert.Equal(2, r.AsistenciasEsteMes);
        var reto = Assert.Single(r.Retos);
        Assert.Equal(2, reto.Progreso);
        Assert.False(reto.Obtenido);
        Assert.Contains("octubre", reto.Descripcion);
    }

    [Fact]
    public void Fechas_repetidas_cuentan_una_vez()
        => Assert.Equal(1, CalculadoraAsistencia.Calcular([Hoy, Hoy], Hoy).TotalAsistencias);
}
