using MediatR;
using FitCore.Identity.Application.Features.Usuarios.Queries;

namespace FitCore.Identity.Application.Features.Usuarios.Commands;

public sealed record CreateUsuarioCommand(
    string FirstName,
    string LastName,
    string Email,
    string Contrasena,
    long Telefono,
    string CodigoRegistro) : IRequest<UsuarioResult>;

public sealed record UpdateUsuarioCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Contrasena,
    long Telefono) : IRequest<bool>;
