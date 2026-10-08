using MediatR;

namespace FitCore.Identity.Application.Features.Acceso.Queries;

// Lo usa recepción al escanear el QR del usuario.
public sealed record ValidarTokenAccesoQuery(string Token) : IRequest<AccesoResult>;

public sealed record AccesoResult(
    bool Permitido,
    string Motivo,
    Guid? UsuarioId,
    string? Nombre,
    string? Plan,
    DateTime? FechaVencimiento);
