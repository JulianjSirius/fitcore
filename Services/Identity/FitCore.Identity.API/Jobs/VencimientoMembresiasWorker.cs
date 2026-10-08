using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Domain;
using FitCore.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.API.Jobs;

// Marca como Vencida toda membresía cuya fecha ya pasó. Corre al arrancar y cada hora.
// El acceso no depende de este proceso (el QR revisa la fecha real); esto mantiene
// coherentes el estado guardado y la copia en User.
public sealed class VencimientoMembresiasWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VencimientoMembresiasWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await VencerAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Falló el proceso de vencimiento de membresías.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task VencerAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ahora = DateTime.UtcNow;

        var vencidas = await context.Membresias
            .Where(m => m.Estado == EstadosMembresia.Activa && m.FechaVencimiento <= ahora)
            .ToListAsync(cancellationToken);
        if (vencidas.Count == 0)
        {
            return;
        }

        var usuarioIds = vencidas
            .SelectMany(m => new[] { m.UsuarioTitularId, m.AcompananteUsuarioId })
            .OfType<Guid>()
            .Distinct()
            .ToList();

        // Quien ya tiene otra membresía vigente conserva su estado.
        var vigentes = await context.Membresias
            .Where(MembresiaService.EstaVigente(ahora))
            .Where(m => usuarioIds.Contains(m.UsuarioTitularId)
                || (m.AcompananteUsuarioId != null && usuarioIds.Contains(m.AcompananteUsuarioId.Value)))
            .Select(m => new { m.UsuarioTitularId, m.AcompananteUsuarioId })
            .ToListAsync(cancellationToken);
        var conOtraVigente = vigentes
            .SelectMany(m => new[] { m.UsuarioTitularId, m.AcompananteUsuarioId })
            .OfType<Guid>()
            .ToHashSet();

        var idsAVencer = usuarioIds.Where(id => !conOtraVigente.Contains(id)).ToList();
        var usuarios = await context.Users
            .Where(u => idsAVencer.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var membresia in vencidas)
        {
            membresia.Estado = EstadosMembresia.Vencida;
        }
        foreach (var usuario in usuarios)
        {
            MembresiaService.ActualizarCopia(usuario, EstadosMembresia.Vencida, usuario.TipoMembresia, usuario.FechaVencimientoMembresia);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Membresías vencidas: {Cantidad}.", vencidas.Count);
    }
}
