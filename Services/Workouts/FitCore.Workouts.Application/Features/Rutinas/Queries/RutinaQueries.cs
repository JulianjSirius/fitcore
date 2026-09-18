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
    int TiempoDescansoSegundos,
    int OrdenAparicion);
