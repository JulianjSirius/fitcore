using FitCore.Workouts.Application.Features.Rutinas.Queries;
using Mapster;

namespace FitCore.Workouts.Application.Features.Progreso.DTOs;

public sealed class ProgresoMapeo : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProgresoResult, ProgresoResponse>()
            .Map(dest => dest.DiferenciaRepeticiones, src => src.RepeticionesNuevas - src.RepeticionesAnteriores)
            .Map(dest => dest.DiferenciaPesoKg, src => src.PesoNuevoKg - src.PesoAnteriorKg);
    }
}
