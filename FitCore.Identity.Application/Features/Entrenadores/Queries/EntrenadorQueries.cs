using MediatR;

namespace FitCore.Identity.Application.Features.Entrenadores.Queries;

public sealed record GetEntrenadoresQuery : IRequest<IReadOnlyList<EntrenadorResult>>;

public sealed record GetEntrenadorByIdQuery(Guid Id) : IRequest<EntrenadorResult?>;

public sealed record EntrenadorResult(
    Guid Id,
    string Nombre,
    string Especialidad,
    string Horario,
    string Email,
    int Telefono);
