using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Application.Features.Usuarios.Queries;
using FitCore.Identity.Application.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FitCore.Identity.Application.Features.Usuarios.Handlers;

public sealed class GenerarTokenAccesoQrQueryHandler
    : IRequestHandler<GenerarTokenAccesoQrQuery, TokenAccesoQrResult>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public GenerarTokenAccesoQrQueryHandler(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<TokenAccesoQrResult> Handle(
        GenerarTokenAccesoQrQuery request,
        CancellationToken cancellationToken)
    {
        // Sin membresía vigente (como titular o acompañante) no se emite QR.
        var tieneMembresia = await _context.Membresias
            .Where(MembresiaService.EstaVigente(DateTime.UtcNow))
            .AnyAsync(m => m.UsuarioTitularId == request.UsuarioId || m.AcompananteUsuarioId == request.UsuarioId,
                cancellationToken);
        if (!tieneMembresia)
        {
            throw new MembresiaInactivaException();
        }

        var (token, expiraEn) = TokenAccesoQr.Generar(_configuration, request.UsuarioId);
        return new TokenAccesoQrResult(token, expiraEn);
    }
}
