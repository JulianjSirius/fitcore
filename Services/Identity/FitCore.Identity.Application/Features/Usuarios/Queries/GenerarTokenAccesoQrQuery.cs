using MediatR;

namespace FitCore.Identity.Application.Features.Usuarios.Queries;

// Token de 1 minuto con el id del usuario; el frontend lo convierte en el QR de entrada.
public sealed record GenerarTokenAccesoQrQuery(Guid UsuarioId) : IRequest<TokenAccesoQrResult>;

public sealed record TokenAccesoQrResult(string Token, DateTime ExpiraEn);
