extern alias IdentityApi;
extern alias WorkoutsApi;

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Gateway.Tests;

// Si alguien agrega un controlador y olvida la ruta en el gateway, Angular recibe 404.
// Estas pruebas leen los controladores reales y la configuración real del gateway.
public sealed class RutasDelGatewayTests
{
    private static readonly JsonElement ReverseProxy = JsonDocument
        .Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gateway.appsettings.json")))
        .RootElement.GetProperty("ReverseProxy");

    public static TheoryData<string, string> Controladores()
    {
        var datos = new TheoryData<string, string>();
        foreach (var (assembly, cluster) in new[]
        {
            (typeof(IdentityApi::FitCore.Identity.API.Controllers.AuthController).Assembly, "identity"),
            (typeof(WorkoutsApi::FitCore.Workouts.API.Controllers.RutinasController).Assembly, "workouts")
        })
        {
            foreach (var nombre in NombresDeControladores(assembly))
            {
                datos.Add(nombre, cluster);
            }
        }
        return datos;
    }

    private static IEnumerable<string> NombresDeControladores(Assembly assembly)
        => assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<RouteAttribute>()?.Template == "api/[controller]")
            .Select(t => t.Name[..^"Controller".Length]);

    [Theory]
    [MemberData(nameof(Controladores))]
    public void Cada_controlador_tiene_ruta_hacia_su_servicio(string controlador, string clusterEsperado)
    {
        var ruta = ReverseProxy.GetProperty("Routes").EnumerateObject()
            .Select(r => r.Value)
            .FirstOrDefault(r => string.Equals(
                r.GetProperty("Match").GetProperty("Path").GetString(),
                $"/api/{controlador}/{{**rest}}",
                StringComparison.OrdinalIgnoreCase));

        Assert.True(ruta.ValueKind == JsonValueKind.Object,
            $"El gateway no tiene ruta para /api/{controlador}. Agrégala en Services/Gateway/FitCore.Gateway/appsettings.json.");
        Assert.Equal(clusterEsperado, ruta.GetProperty("ClusterId").GetString());
    }

    [Fact]
    public void Cada_ruta_apunta_a_un_cluster_existente()
    {
        var clusters = ReverseProxy.GetProperty("Clusters").EnumerateObject().Select(c => c.Name).ToHashSet();
        foreach (var ruta in ReverseProxy.GetProperty("Routes").EnumerateObject())
        {
            Assert.Contains(ruta.Value.GetProperty("ClusterId").GetString()!, clusters);
        }
    }

    [Fact]
    public void Los_clusters_apuntan_a_los_puertos_de_cada_servicio()
    {
        string Direccion(string cluster) => ReverseProxy.GetProperty("Clusters").GetProperty(cluster)
            .GetProperty("Destinations").EnumerateObject().Single().Value.GetProperty("Address").GetString()!;

        Assert.Equal("http://localhost:5231/", Direccion("identity"));
        Assert.Equal("http://localhost:5000/", Direccion("workouts"));
    }
}
