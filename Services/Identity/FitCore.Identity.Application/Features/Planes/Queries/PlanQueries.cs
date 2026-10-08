using MediatR;

namespace FitCore.Identity.Application.Features.Planes.Queries;

public sealed record GetPlanesQuery : IRequest<IReadOnlyList<PlanResult>>;

public sealed record PlanResult(
    Guid Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int DuracionMeses,
    int MaxBeneficiarios,
    bool Activo,
    DateTime FechaActualizacion);
