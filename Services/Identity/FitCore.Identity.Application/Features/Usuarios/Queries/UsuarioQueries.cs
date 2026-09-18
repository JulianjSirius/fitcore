using MediatR;

namespace FitCore.Identity.Application.Features.Usuarios.Queries;

public sealed record GetUsuariosQuery : IRequest<IReadOnlyList<UsuarioResult>>;

public sealed record GetUsuarioByIdQuery(Guid Id) : IRequest<UsuarioResult?>;

public sealed record UsuarioResult(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    int Telefono);
