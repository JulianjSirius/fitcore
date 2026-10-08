using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class MembresiaTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private static async Task<JsonElement> MembresiaDe(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("membresia");

    [Fact]
    public async Task POST_pagos_mensual_activa_la_membresia_por_un_mes()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);

        var response = await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var membresia = body.GetProperty("membresia");
        Assert.Equal("Activa", membresia.GetProperty("estado").GetString());
        Assert.Equal("Mensual", membresia.GetProperty("planCodigo").GetString());
        var vence = membresia.GetProperty("fechaVencimiento").GetDateTime();
        Assert.InRange(vence, DateTime.UtcNow.AddMonths(1).AddMinutes(-1), DateTime.UtcNow.AddMonths(1).AddMinutes(1));

        var pago = body.GetProperty("pago");
        Assert.Equal("Aprobado", pago.GetProperty("estado").GetString());
        Assert.Equal((await _e.PlanAsync("Mensual")).GetProperty("precio").GetDecimal(), pago.GetProperty("monto").GetDecimal());
        Assert.StartsWith("MAN-", pago.GetProperty("referencia").GetString());

        // La copia del estado en User queda sincronizada.
        var user = await factory.EnBaseDeDatosAsync(db => db.Users.SingleAsync(u => u.Id == usuario.Id));
        Assert.Equal("Activa", user.EstadoMembresia);
        Assert.Equal("Mensual", user.TipoMembresia);
    }

    [Fact]
    public async Task Pagar_antes_de_vencer_extiende_desde_el_vencimiento_anterior()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);

        var primero = await MembresiaDe(await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual"));
        var segundoResponse = await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual");
        await Escenario.EsperarAsync(segundoResponse, HttpStatusCode.OK);
        var segundo = await MembresiaDe(segundoResponse);

        Assert.Equal(primero.GetProperty("id").GetGuid(), segundo.GetProperty("id").GetGuid());
        Assert.Equal(primero.GetProperty("fechaVencimiento").GetDateTime().AddMonths(1),
            segundo.GetProperty("fechaVencimiento").GetDateTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Otro_plan_con_membresia_vigente_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual"), HttpStatusCode.OK);

        var response = await _e.RegistrarPagoAsync(admin, usuario.Id, "Anual");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duo_sin_acompanante_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, usuario.Id, "Duo")).StatusCode);
    }

    [Fact]
    public async Task Mensual_con_acompanante_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var titular = await _e.UsuarioAsync(admin);
        var otro = await _e.UsuarioAsync(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, titular.Id, "Mensual", otro.Email)).StatusCode);
    }

    [Fact]
    public async Task Duo_activa_al_titular_y_al_acompanante_con_el_mismo_vencimiento()
    {
        var admin = await _e.AdministradorAsync();
        var titular = await _e.UsuarioAsync(admin);
        var acompanante = await _e.UsuarioAsync(admin);

        var response = await _e.RegistrarPagoAsync(admin, titular.Id, "Duo", acompanante.Email);
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var membresia = await MembresiaDe(response);
        Assert.Equal(acompanante.Id, membresia.GetProperty("acompananteUsuarioId").GetGuid());

        var mia = await _e.Como(acompanante).GetAsync("/api/Membresia/mia");
        await Escenario.EsperarAsync(mia, HttpStatusCode.OK);
        var delAcompanante = await mia.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Activa", delAcompanante.GetProperty("estado").GetString());
        Assert.Equal(membresia.GetProperty("fechaVencimiento").GetDateTime(), delAcompanante.GetProperty("fechaVencimiento").GetDateTime());
    }

    [Fact]
    public async Task Duo_con_acompanante_que_ya_tiene_membresia_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var titular = await _e.UsuarioAsync(admin);
        var acompanante = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, acompanante.Id, "Mensual"), HttpStatusCode.OK);

        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, titular.Id, "Duo", acompanante.Email)).StatusCode);
    }

    [Fact]
    public async Task Duo_con_correo_no_registrado_o_el_mismo_titular_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var titular = await _e.UsuarioAsync(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, titular.Id, "Duo", "nadie@test.fitcore")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, titular.Id, "Duo", titular.Email)).StatusCode);
    }

    [Theory]
    [InlineData("Wompi")]
    [InlineData("Bitcoin")]
    public async Task Metodo_no_manual_responde_400(string metodo)
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual", metodo: metodo)).StatusCode);
    }

    [Fact]
    public async Task Usuario_inexistente_o_plan_inexistente_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await _e.RegistrarPagoAsync(admin, Guid.NewGuid(), "Mensual")).StatusCode);

        var usuario = await _e.UsuarioAsync(admin);
        var response = await _e.Como(admin).PostAsJsonAsync("/api/Membresia/pagos",
            new { usuarioId = usuario.Id, planId = Guid.NewGuid(), metodo = "Efectivo" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Referencia_repetida_responde_400()
    {
        var admin = await _e.AdministradorAsync();
        var ana = await _e.UsuarioAsync(admin);
        var beto = await _e.UsuarioAsync(admin);
        var planId = (await _e.PlanAsync("Mensual")).GetProperty("id").GetGuid();
        var referencia = $"COMP-{Guid.NewGuid():N}"[..20];

        var primero = await _e.Como(admin).PostAsJsonAsync("/api/Membresia/pagos",
            new { usuarioId = ana.Id, planId, metodo = "Transferencia", referencia });
        await Escenario.EsperarAsync(primero, HttpStatusCode.OK);

        var repetido = await _e.Como(admin).PostAsJsonAsync("/api/Membresia/pagos",
            new { usuarioId = beto.Id, planId, metodo = "Transferencia", referencia });
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);
    }

    [Fact]
    public async Task POST_pagos_sin_sesion_401_como_usuario_o_entrenador_403()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        var entrenador = await _e.EntrenadorAsync(admin);
        var planId = (await _e.PlanAsync("Mensual")).GetProperty("id").GetGuid();
        var pago = new { usuarioId = usuario.Id, planId, metodo = "Efectivo" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().PostAsJsonAsync("/api/Membresia/pagos", pago)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).PostAsJsonAsync("/api/Membresia/pagos", pago)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(entrenador).PostAsJsonAsync("/api/Membresia/pagos", pago)).StatusCode);
    }

    [Fact]
    public async Task GET_pagos_como_admin_filtra_por_usuario_y_como_usuario_403()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual"), HttpStatusCode.OK);

        var response = await _e.Como(admin).GetAsync($"/api/Membresia/pagos?usuarioId={usuario.Id}");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var pagos = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Single(pagos);
        Assert.Equal(usuario.Id, pagos[0].GetProperty("usuarioId").GetGuid());

        await Escenario.EsperarAsync(await _e.Como(admin).GetAsync("/api/Membresia/pagos"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/Membresia/pagos")).StatusCode);
    }

    [Fact]
    public async Task GET_mia_sin_membresia_404_y_como_admin_403()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        Assert.Equal(HttpStatusCode.NotFound, (await _e.Como(usuario).GetAsync("/api/Membresia/mia")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(admin).GetAsync("/api/Membresia/mia")).StatusCode);
    }

    [Fact]
    public async Task GET_de_usuario_propio_admin_y_entrenador_200_otro_usuario_403()
    {
        var admin = await _e.AdministradorAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var ana = await _e.UsuarioAsync(admin);
        var beto = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, ana.Id, "Mensual"), HttpStatusCode.OK);

        var url = $"/api/Membresia/usuario/{ana.Id}";
        await Escenario.EsperarAsync(await _e.Como(ana).GetAsync(url), HttpStatusCode.OK);
        await Escenario.EsperarAsync(await _e.Como(admin).GetAsync(url), HttpStatusCode.OK);
        await Escenario.EsperarAsync(await _e.Como(entrenador).GetAsync(url), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(beto).GetAsync(url)).StatusCode);
    }
}
