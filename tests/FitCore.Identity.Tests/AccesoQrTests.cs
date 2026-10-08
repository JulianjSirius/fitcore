using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FitCore.Identity.API.Jobs;
using FitCore.Identity.Domain;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace FitCore.Identity.Tests;

// GET /api/Usuario/me/qr-token, POST /api/Acceso/validar y el vencimiento automático.
[Collection(IdentityCollection.Nombre)]
public sealed class AccesoQrTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    private async Task<(Cuenta Admin, Cuenta Usuario)> UsuarioConMembresiaAsync()
    {
        var admin = await _e.AdministradorAsync();
        var usuario = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, usuario.Id, "Mensual"), HttpStatusCode.OK);
        return (admin, usuario);
    }

    private async Task<string> QrTokenAsync(Cuenta usuario)
    {
        var response = await _e.Como(usuario).GetAsync("/api/Usuario/me/qr-token");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private async Task<JsonElement> ValidarAsync(Cuenta recepcion, string token)
    {
        var response = await _e.Como(recepcion).PostAsJsonAsync("/api/Acceso/validar", new { token });
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task QR_sin_membresia_responde_403_con_mensaje()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        var response = await _e.Como(usuario).GetAsync("/api/Usuario/me/qr-token");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("membresía", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task QR_con_membresia_trae_el_id_del_usuario_y_dura_un_minuto()
    {
        var (_, usuario) = await UsuarioConMembresiaAsync();
        var response = await _e.Como(usuario).GetAsync("/api/Usuario/me/qr-token");
        await Escenario.EsperarAsync(response, HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("token").GetString());
        Assert.Equal(usuario.Id.ToString(), jwt.Subject);
        Assert.Contains("FitCore.Acceso", jwt.Audiences);
        Assert.InRange((jwt.ValidTo - jwt.ValidFrom).TotalSeconds, 59, 61);
    }

    [Fact]
    public async Task QR_solo_para_el_rol_Usuario()
    {
        var admin = await _e.AdministradorAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(admin).GetAsync("/api/Usuario/me/qr-token")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Anonimo().GetAsync("/api/Usuario/me/qr-token")).StatusCode);
    }

    [Fact]
    public async Task El_acompanante_Duo_tambien_obtiene_su_QR()
    {
        var admin = await _e.AdministradorAsync();
        var titular = await _e.UsuarioAsync(admin);
        var acompanante = await _e.UsuarioAsync(admin);
        await Escenario.EsperarAsync(await _e.RegistrarPagoAsync(admin, titular.Id, "Duo", acompanante.Email), HttpStatusCode.OK);

        var resultado = await ValidarAsync(admin, await QrTokenAsync(acompanante));
        Assert.True(resultado.GetProperty("permitido").GetBoolean());
        Assert.Equal(acompanante.Id, resultado.GetProperty("usuarioId").GetGuid());
    }

    [Fact]
    public async Task Validar_token_vigente_permite_el_acceso()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        var resultado = await ValidarAsync(admin, await QrTokenAsync(usuario));

        Assert.True(resultado.GetProperty("permitido").GetBoolean());
        Assert.Equal("Mensual", resultado.GetProperty("plan").GetString());
        Assert.Equal("Ana Pruebas", resultado.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Validar_lo_puede_hacer_un_entrenador_pero_no_un_usuario()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        var entrenador = await _e.EntrenadorAsync(admin);
        var token = await QrTokenAsync(usuario);

        Assert.True((await ValidarAsync(entrenador, token)).GetProperty("permitido").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _e.Como(usuario).PostAsJsonAsync("/api/Acceso/validar", new { token })).StatusCode);
    }

    [Fact]
    public async Task Token_alterado_se_rechaza()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        var token = await QrTokenAsync(usuario);
        var alterado = token[..^4] + (token.EndsWith("AAAA") ? "BBBB" : "AAAA");

        Assert.False((await ValidarAsync(admin, alterado)).GetProperty("permitido").GetBoolean());
    }

    [Fact]
    public async Task Token_expirado_se_rechaza()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        var vencido = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "FitCore.Identity.API",
            audience: "FitCore.Acceso",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-3),
            expires: DateTime.UtcNow.AddMinutes(-2),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(factory.QrKey)), SecurityAlgorithms.HmacSha256)));

        Assert.False((await ValidarAsync(admin, vencido)).GetProperty("permitido").GetBoolean());
    }

    [Fact]
    public async Task El_token_de_login_no_sirve_como_QR_ni_el_QR_como_login()
    {
        var (admin, usuario) = await UsuarioConMembresiaAsync();
        Assert.False((await ValidarAsync(admin, usuario.Token)).GetProperty("permitido").GetBoolean());

        var qr = await QrTokenAsync(usuario);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _e.Como(qr).GetAsync("/api/Membresia/mia")).StatusCode);
    }

    [Fact]
    public async Task Membresia_con_fecha_pasada_no_genera_QR_y_se_informa_Vencida()
    {
        var (_, usuario) = await UsuarioConMembresiaAsync();
        await VencerEnBaseAsync(usuario.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await _e.Como(usuario).GetAsync("/api/Usuario/me/qr-token")).StatusCode);
        var mia = await _e.Como(usuario).GetFromJsonAsync<JsonElement>("/api/Membresia/mia");
        Assert.Equal("Vencida", mia.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task El_proceso_de_vencimiento_marca_la_membresia_y_el_usuario_como_Vencida()
    {
        var (_, usuario) = await UsuarioConMembresiaAsync();
        await VencerEnBaseAsync(usuario.Id);

        // Corre una pasada del mismo proceso que la API ejecuta cada hora.
        var worker = new VencimientoMembresiasWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<VencimientoMembresiasWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var estado = "";
            for (var intento = 0; intento < 50 && estado != "Vencida"; intento++)
            {
                await Task.Delay(100);
                estado = await factory.EnBaseDeDatosAsync(db => db.Membresias
                    .Where(m => m.UsuarioTitularId == usuario.Id).Select(m => m.Estado).SingleAsync());
            }
            Assert.Equal(EstadosMembresia.Vencida, estado);

            var copia = await factory.EnBaseDeDatosAsync(db => db.Users
                .Where(u => u.Id == usuario.Id).Select(u => u.EstadoMembresia).SingleAsync());
            Assert.Equal(EstadosMembresia.Vencida, copia);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private Task VencerEnBaseAsync(Guid usuarioId)
        => factory.EnBaseDeDatosAsync(db => db.Membresias
            .Where(m => m.UsuarioTitularId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.FechaVencimiento, DateTime.UtcNow.AddMinutes(-5))));
}
