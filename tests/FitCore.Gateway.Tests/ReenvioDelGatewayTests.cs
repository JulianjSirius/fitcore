using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Gateway.Tests;

// Levanta el gateway real y dos servicios falsos (en puertos reales) que responden quién
// recibió la petición. Comprueba la conexión Angular → gateway → API sin depender de las APIs.
public sealed class GatewayFixture : IAsyncLifetime
{
    private WebApplication? _identity;
    private WebApplication? _workouts;
    public WebApplicationFactory<Program> Gateway { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _identity = await IniciarServicioFalsoAsync("identity");
        _workouts = await IniciarServicioFalsoAsync("workouts");

        Gateway = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ReverseProxy:Clusters:identity:Destinations:identity-api:Address", Direccion(_identity))
            .UseSetting("ReverseProxy:Clusters:workouts:Destinations:workouts-api:Address", Direccion(_workouts)));
    }

    // Con el puerto 0, la dirección real solo la conoce el servidor ya iniciado.
    private static string Direccion(WebApplication app)
        => app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    // Servicio mínimo con el mismo CORS que las APIs reales (origen http://localhost:4200).
    private static async Task<WebApplication> IniciarServicioFalsoAsync(string nombre)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
            p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();
        app.UseCors("Frontend");
        app.Map("/{**ruta}", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var cuerpo = await reader.ReadToEndAsync();
            var auth = ctx.Request.Headers.Authorization.ToString();
            return Results.Text($"{nombre}|{ctx.Request.Method}|{ctx.Request.Path}{ctx.Request.QueryString}|{auth}|{cuerpo}");
        });
        await app.StartAsync();
        return app;
    }

    public async Task DisposeAsync()
    {
        await Gateway.DisposeAsync();
        if (_identity is not null) await _identity.DisposeAsync();
        if (_workouts is not null) await _workouts.DisposeAsync();
    }
}

public sealed class ReenvioDelGatewayTests(GatewayFixture fixture) : IClassFixture<GatewayFixture>
{
    private HttpClient Cliente() => fixture.Gateway.CreateClient();

    [Theory]
    [InlineData("/api/Auth/login/usuario", "identity")]
    [InlineData("/api/Usuario", "identity")]
    [InlineData("/api/Usuario/me/qr-token", "identity")]
    [InlineData("/api/Entrenador", "identity")]
    [InlineData("/api/Administrador", "identity")]
    [InlineData("/api/CodigoRegistro", "identity")]
    [InlineData("/api/Plan", "identity")]
    [InlineData("/api/Membresia/pagos", "identity")]
    [InlineData("/api/Acceso/validar", "identity")]
    [InlineData("/api/Ejercicios", "workouts")]
    [InlineData("/api/Rutinas", "workouts")]
    public async Task Cada_ruta_llega_al_servicio_correcto(string ruta, string servicio)
    {
        var response = await Cliente().GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var partes = (await response.Content.ReadAsStringAsync()).Split('|');
        Assert.Equal(servicio, partes[0]);
        Assert.Equal(ruta, partes[2]);
    }

    [Fact]
    public async Task Reenvia_metodo_query_cuerpo_y_token()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Membresia/pagos?usuarioId=abc")
        {
            Content = new StringContent("{\"metodo\":\"Efectivo\"}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-de-prueba");

        var partes = (await (await Cliente().SendAsync(request)).Content.ReadAsStringAsync()).Split('|');
        Assert.Equal(["identity", "POST", "/api/Membresia/pagos?usuarioId=abc", "Bearer token-de-prueba", "{\"metodo\":\"Efectivo\"}"], partes);
    }

    [Fact]
    public async Task La_consulta_CORS_del_navegador_pasa_y_vuelve_con_permiso_para_Angular()
    {
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/Plan");
        preflight.Headers.Add("Origin", "http://localhost:4200");
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await Cliente().SendAsync(preflight);
        Assert.True(response.IsSuccessStatusCode, $"Preflight respondió {(int)response.StatusCode}");
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Una_ruta_desconocida_responde_404_en_el_gateway()
        => Assert.Equal(HttpStatusCode.NotFound, (await Cliente().GetAsync("/api/NoExiste")).StatusCode);

    [Fact]
    public async Task Health_del_gateway_responde_200()
        => Assert.Equal(HttpStatusCode.OK, (await Cliente().GetAsync("/health")).StatusCode);
}
