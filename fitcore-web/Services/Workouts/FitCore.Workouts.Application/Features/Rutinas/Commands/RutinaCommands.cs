using MediatR;
using FitCore.Workouts.Application.Features.Rutinas.Queries;

namespace FitCore.Workouts.Application.Features.Rutinas.Commands;

public sealed record RutinaEjercicioInput(
    Guid EjercicioId,
    int Series,
    int Repeticiones,
    int TiempoDescansoSegundos,
    int OrdenAparicion);

public sealed record CreateRutinaCommand(
    string Nombre,
    string Descripcion,
    string NivelDificultad,
    Guid UsuarioId,
    bool PermitirEdicionEntrenador,
    IReadOnlyList<RutinaEjercicioInput> RutinaEjercicios,
    Guid CreadorId,
    string Rol) : IRequest<RutinaResult>;

public sealed record UpdateRutinaCommand(
    Guid Id,
    string Nombre,
    string Descripcion,
    string NivelDificultad,
    bool PermitirEdicionEntrenador,
    IReadOnlyList<RutinaEjercicioInput> RutinaEjercicios,
    Guid ActorId,
    string Rol) : IRequest<RutinaResult?>;

public sealed record DeleteRutinaCommand(Guid Id, Guid ActorId, string Rol)
    : IRequest<bool>;
