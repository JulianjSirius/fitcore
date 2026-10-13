using FitCore.Identity.Application.Features.Usuarios.Queries;
using Mapster;

namespace FitCore.Identity.Application.Features.Usuarios.DTOs;

public sealed class UsuarioMapeo : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // La respuesta conserva "telefono" en minúscula por compatibilidad con los clientes.
        config.NewConfig<UsuarioResult, UsuarioResponse>()
            .Map(dest => dest.telefono, src => src.Telefono);
    }
}
