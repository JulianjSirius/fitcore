using MediatR;
using FitCore.Workouts.Application.Features.Ejercicios.Queries;

namespace FitCore.Workouts.Application.Features.Ejercicios.Commands;

public sealed record CreateEjercicioCommand(
    string Nombre,
    string GrupoMuscular,
    string DescripcionOrientativa) : IRequest<EjercicioResult>;

public sealed record UpdateEjercicioCommand(
    Guid Id,
    string Nombre,
    string GrupoMuscular,
    string DescripcionOrientativa) : IRequest<EjercicioResult?>;

public sealed record DeleteEjercicioCommand(Guid Id) : IRequest<bool>;
