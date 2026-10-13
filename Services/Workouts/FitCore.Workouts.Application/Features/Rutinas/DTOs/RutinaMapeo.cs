using FitCore.Workouts.Application.Features.Progreso.DTOs;
using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using Mapster;

namespace FitCore.Workouts.Application.Features.Rutinas.DTOs;

public sealed class RutinaMapeo : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RutinaEjercicioRequest, RutinaEjercicioInput>()
            .Map(dest => dest.PesoKg, src => Math.Round(src.PesoKg, 2));

        // Solo el PUT trae progresos; las demás respuestas los dejan vacíos.
        config.NewConfig<RutinaResult, RutinaResponse>()
            .Ignore(dest => dest.Progresos);

        config.NewConfig<RutinaActualizadaResult, RutinaResponse>()
            .Map(dest => dest, src => src.Rutina)
            .Map(dest => dest.Progresos, src => src.Progresos);
    }
}
