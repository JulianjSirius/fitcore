using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Identity.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Tests;

[Collection(IdentityCollection.Nombre)]
public sealed class AuthTests(IdentityApiFactory factory)
{
    private readonly Escenario _e = new(factory);

    [Fact]
    public async Task Login_de_administrador_devuelve_token_y_rol()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Auth/login/administrador",
            new { email = admin.Email, contrasena = admin.Contrasena });

        await Escenario.EsperarAsync(response, HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Administrador", body.GetProperty("role").GetString());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("token").GetString());
        Assert.Equal(admin.Id.ToString(), jwt.Subject);
        Assert.Equal("Dueño", jwt.Claims.Single(c => c.Type == "NivelAcceso").Value);
    }

    [Fact]
    public async Task Contrasena_incorrecta_responde_401()
    {
        var admin = await _e.AdministradorAsync();
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Auth/login/administrador",
            new { email = admin.Email, contrasena = "otra-clave" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_no_entra_por_el_login_de_administrador()
    {
        var usuario = await _e.UsuarioAsync(await _e.AdministradorAsync());
        var response = await _e.Anonimo().PostAsJsonAsync("/api/Auth/login/administrador",
            new { email = usuario.Email, contrasena = usuario.Contrasena });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_de_usuario_y_entrenador_no_llevan_NivelAcceso()
    {
        var admin = await _e.AdministradorAsync();
        foreach (var cuenta in new[] { await _e.UsuarioAsync(admin), await _e.EntrenadorAsync(admin) })
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(cuenta.Token);
            Assert.DoesNotContain(jwt.Claims, c => c.Type == "NivelAcceso");
        }
    }

    [Fact]
    public async Task Contrasena_antigua_en_texto_plano_se_convierte_a_hash_al_entrar()
    {
        var admin = await _e.AdministradorAsync(contrasenaEnTextoPlano: true);

        var guardada = await factory.EnBaseDeDatosAsync(db =>
            db.Administradores.Where(a => a.Id == admin.Id).Select(a => a.Contrasena).SingleAsync());
        Assert.StartsWith("PBKDF2-SHA256$", guardada);

        // Y se puede seguir entrando con la misma contraseña.
        Assert.False(string.IsNullOrEmpty(await _e.LoginAsync("administrador", admin.Email, admin.Contrasena)));
    }

    [Theory]
    [InlineData("administrador")]
    [InlineData("usuario")]
    [InlineData("entrenador")]
    public async Task Credenciales_vacias_responden_401(string tipo)
    {
        var response = await _e.Anonimo().PostAsJsonAsync($"/api/Auth/login/{tipo}", new { email = "", contrasena = "" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
