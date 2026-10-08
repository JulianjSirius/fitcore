using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Application.Features.Progreso.Queries;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Features.Progreso.Handlers;

public sealed class GetMiProgresoQueryHandler : IRequestHandler<GetMiProgresoQuery, ProgresoResumenResult>
{
    private readonly IWorkoutsDbContext context;

    public GetMiProgresoQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<ProgresoResumenResult> Handle(GetMiProgresoQuery request, CancellationToken cancellationToken)
    {
        var progresos = await context.ProgresosEjercicio
            .AsNoTracking()
            .Where(progreso => progreso.UsuarioId == request.UsuarioId)
            .Select(progreso => new ProgresoResult(
                progreso.Id,
                progreso.RutinaId,
                progreso.EjercicioId,
                progreso.NombreEjercicio,
                progreso.RepeticionesAnteriores,
                progreso.RepeticionesNuevas,
                progreso.PesoAnteriorKg,
                progreso.PesoNuevoKg,
                progreso.EsRecordPersonal,
                progreso.Fecha))
            .ToListAsync(cancellationToken);

        return CalculadoraProgreso.Calcular(progresos, CalendarioGimnasio.Hoy());
    }
}
