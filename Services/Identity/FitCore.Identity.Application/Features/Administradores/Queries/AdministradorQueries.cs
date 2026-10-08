using MediatR;

namespace FitCore.Identity.Application.Features.Administradores.Queries;

public sealed record GetAdministradoresQuery : IRequest<IReadOnlyList<AdministradorResult>>;

public sealed record GetAdministradorByIdQuery(Guid Id) : IRequest<AdministradorResult?>;

public sealed record AdministradorResult(
    Guid Id,
    string Nombre,
    string LastName,
    string Email,
    long Telefono,
    string NivelAcceso);
