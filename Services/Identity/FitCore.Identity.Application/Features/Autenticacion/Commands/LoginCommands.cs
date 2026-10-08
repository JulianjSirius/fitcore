using MediatR;
using FitCore.Identity.Application.Features.Autenticacion.Queries;

namespace FitCore.Identity.Application.Features.Autenticacion.Commands;

public enum TipoCuenta
{
    Administrador,
    Usuario,
    Entrenador
}

public sealed record LoginCommand(TipoCuenta TipoCuenta, string Email, string Contrasena)
    : IRequest<LoginResult?>;
