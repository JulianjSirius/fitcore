using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Acceso.Queries;
using FitCore.Identity.Application.Features.Logros;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Application.Security;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FitCore.Identity.Application.Features.Acceso.Handlers;

public sealed class ValidarTokenAccesoQueryHandler : IRequestHandler<ValidarTokenAccesoQuery, AccesoResult>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public ValidarTokenAccesoQueryHandler(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AccesoResult> Handle(ValidarTokenAccesoQuery request, CancellationToken cancellationToken)
    {
        var usuarioId = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : TokenAccesoQr.Validar(_configuration, request.Token.Trim());
        if (usuarioId is null)
        {
            return new AccesoResult(false, "QR inválido o vencido. Pide al usuario que lo actualice.", null, null, null, null);
        }

        var usuario = await _context.Users
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Id, Nombre = u.FirstName + " " + u.LastName })
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null)
        {
            return new AccesoResult(false, "El usuario ya no existe.", usuarioId, null, null, null);
        }

        // La membresía se revisa de nuevo: pudo vencer durante el minuto de vida del token.
        var membresia = await _context.Membresias
            .Where(MembresiaService.EstaVigente(DateTime.UtcNow))
            .Where(m => m.UsuarioTitularId == usuario.Id || m.AcompananteUsuarioId == usuario.Id)
            .Select(m => new { m.Plan.Nombre, m.FechaVencimiento })
            .FirstOrDefaultAsync(cancellationToken);

        if (membresia is null)
        {
            return new AccesoResult(false, "La membresía no está activa.", usuario.Id, usuario.Nombre, null, null);
        }

        await RegistrarAsistenciaAsync(usuario.Id, cancellationToken);
        return new AccesoResult(true, "Acceso permitido.", usuario.Id, usuario.Nombre, membresia.Nombre, membresia.FechaVencimiento);
    }

    // Cuenta para rachas e insignias. Solo la primera entrada del día crea registro.
    private async Task RegistrarAsistenciaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;
        var hoy = CalendarioGimnasio.Dia(ahora);
        if (await _context.Asistencias.AnyAsync(a => a.UsuarioId == usuarioId && a.Fecha == hoy, cancellationToken))
        {
            return;
        }

        _context.Asistencias.Add(new Asistencia { Id = Guid.NewGuid(), UsuarioId = usuarioId, Fecha = hoy, FechaHoraEntrada = ahora });
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dos escaneos simultáneos: el índice único ya guardó la asistencia del día.
        }
    }
}
