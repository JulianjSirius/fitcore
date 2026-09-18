using MediatR;

namespace FitCore.Workouts.Application.Features.Ejercicios.Queries;

public sealed record GetEjerciciosQuery : IRequest<IReadOnlyList<EjercicioResult>>;

public sealed record GetEjercicioByIdQuery(Guid Id) : IRequest<EjercicioResult?>;

public sealed record EjercicioResult(
    Guid Id,
    string Nombre,
    string GrupoMuscular,
    string DescripcionOrientativa);
