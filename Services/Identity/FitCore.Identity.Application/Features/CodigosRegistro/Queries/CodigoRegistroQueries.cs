using MediatR;

namespace FitCore.Identity.Application.Features.CodigosRegistro.Queries;

public sealed record GetCodigosRegistroQuery : IRequest<IReadOnlyList<CodigoRegistroResult>>;

public sealed record CodigoRegistroResult(
    Guid Id,
    string Codigo,
    Guid CreadoPorAdministradorId,
    DateTime FechaCreacion,
    DateTime FechaExpiracion,
    Guid? UsadoPorUsuarioId,
    DateTime? FechaUso);
