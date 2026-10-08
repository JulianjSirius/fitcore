using MediatR;

namespace FitCore.Identity.Application.Features.Logros.Queries;

public sealed record GetMiAsistenciaQuery(Guid UsuarioId) : IRequest<AsistenciaResumenResult>;

public sealed record AsistenciaResumenResult(
    int RachaActual,
    int MejorRacha,
    int TotalAsistencias,
    int AsistenciasEsteMes,
    DateOnly? UltimaAsistencia,
    IReadOnlyList<DateOnly> DiasRecientes,
    IReadOnlyList<LogroResult> Insignias,
    IReadOnlyList<LogroResult> Retos);

// Sirve para insignias (permanentes) y retos (se reinician cada mes).
public sealed record LogroResult(
    string Codigo,
    string Nombre,
    string Descripcion,
    int Meta,
    int Progreso,
    bool Obtenido,
    DateOnly? FechaObtencion);
