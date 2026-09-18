using MediatR;
using FitCore.Identity.Application.Features.Autenticacion.Queries;

namespace FitCore.Identity.Application.Features.Autenticacion.Commands;

public sealed record LoginAdministradorCommand(string Email, string Contrasena)
    : IRequest<LoginResult?>;

public sealed record LoginUsuarioCommand(string Email, string Contrasena)
    : IRequest<LoginResult?>;

public sealed record LoginEntrenadorCommand(string Email, string Contrasena)
    : IRequest<LoginResult?>;
