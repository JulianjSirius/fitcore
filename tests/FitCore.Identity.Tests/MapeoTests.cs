using FitCore.Identity.Application.Features.Usuarios.DTOs;
using FitCore.Identity.Application.Features.Usuarios.Queries;
using Mapster;

namespace FitCore.Identity.Tests;

public sealed class MapeoTests
{
    private static readonly TypeAdapterConfig Config = CrearConfig();

    private static TypeAdapterConfig CrearConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(UsuarioMapeo).Assembly);
        return config;
    }

    [Fact]
    public void La_configuracion_compila()
        => Config.Compile();

    [Fact]
    public void Usuario_conserva_telefono_en_minuscula()
    {
        var usuario = new UsuarioResult(Guid.NewGuid(), "Ana", "Gómez", "ana@fitcore.com", 3001234567);

        var response = usuario.Adapt<UsuarioResponse>(Config);

        Assert.Equal(usuario.Id, response.Id);
        Assert.Equal("Ana", response.FirstName);
        Assert.Equal(3001234567, response.telefono);
    }
}
