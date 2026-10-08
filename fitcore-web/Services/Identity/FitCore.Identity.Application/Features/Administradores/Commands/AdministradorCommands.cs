using FitCore.Identity.Application.Features.Administradores.Queries;
using MediatR;

namespace FitCore.Identity.Application.Features.Administradores.Commands;

public sealed record CreateAdministradorCommand(
    string Nombre,
    string LastName,
    string Email,
    string Contrasena,
    string Telefono,
    string NivelAcceso) : IRequest<AdministradorResult>;

public sealed record UpdateAdministradorCommand(
    Guid Id,
    string Nombre,
    string LastName,
    string Email,
    string Contrasena,
    string Telefono,
    string NivelAcceso) : IRequest<bool>;

public sealed record DeleteAdministradorCommand(Guid Id) : IRequest<bool>;
