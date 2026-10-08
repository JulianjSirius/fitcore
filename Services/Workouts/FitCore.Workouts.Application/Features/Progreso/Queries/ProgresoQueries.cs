using FitCore.Workouts.Application.Features.Rutinas.Queries;
using MediatR;

namespace FitCore.Workouts.Application.Features.Progreso.Queries;

public sealed record GetMiProgresoQuery(Guid UsuarioId) : IRequest<ProgresoResumenResult>;

public sealed record ProgresoResumenResult(
    int TotalMejoras,
    int RecordsPersonales,
    decimal KgGanados,
    int RepeticionesGanadas,
    int MejorasEsteMes,
    IReadOnlyList<ProgresoResult> Historial,
    IReadOnlyList<LogroResult> Insignias,
    IReadOnlyList<LogroResult> Retos);

// Misma forma que los logros de asistencia de Identity para que el frontend los pinte igual.
public sealed record LogroResult(
    string Codigo,
    string Nombre,
    string Descripcion,
    int Meta,
    int Progreso,
    bool Obtenido,
    DateOnly? FechaObtencion);
