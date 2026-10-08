using MediatR;

namespace FitCore.Workouts.Application.Features.Rutinas.Queries;

public sealed record GetRutinasQuery(Guid? UsuarioId = null)
    : IRequest<IReadOnlyList<RutinaResult>>;

public sealed record GetRutinaByIdQuery(Guid Id)
    : IRequest<RutinaResult?>;

public sealed record RutinaResult(
    Guid Id,
    string Nombre,
    string Descripcion,
    string NivelDificultad,
    Guid UsuarioId,
    Guid CreadorId,
    bool PermitirEdicionEntrenador,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    IReadOnlyList<RutinaEjercicioResult> RutinaEjercicios);

public sealed record RutinaEjercicioResult(
    Guid EjercicioId,
    string Nombre,
    string GrupoMuscular,
    int Series,
    int Repeticiones,
    decimal PesoKg,
    int TiempoDescansoSegundos,
    int OrdenAparicion);

// Respuesta del PUT: la rutina y las mejoras (repeticiones o peso) que generó la edición.
public sealed record RutinaActualizadaResult(
    RutinaResult Rutina,
    IReadOnlyList<ProgresoResult> Progresos);

public sealed record ProgresoResult(
    Guid Id,
    Guid RutinaId,
    Guid EjercicioId,
    string NombreEjercicio,
    int RepeticionesAnteriores,
    int RepeticionesNuevas,
    decimal PesoAnteriorKg,
    decimal PesoNuevoKg,
    bool EsRecordPersonal,
    DateTime Fecha);
