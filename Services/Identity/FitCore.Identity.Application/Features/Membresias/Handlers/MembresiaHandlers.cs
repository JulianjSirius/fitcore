using System.Security.Cryptography;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Membresias.Commands;
using FitCore.Identity.Application.Features.Membresias.Queries;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Membresias.Handlers;

public sealed class RegistrarPagoManualCommandHandler
    : IRequestHandler<RegistrarPagoManualCommand, PagoRegistradoResult>
{
    private readonly IAppDbContext _context;
    private readonly MembresiaService _membresias;

    public RegistrarPagoManualCommandHandler(IAppDbContext context, MembresiaService membresias)
    {
        _context = context;
        _membresias = membresias;
    }

    public async Task<PagoRegistradoResult> Handle(
        RegistrarPagoManualCommand request,
        CancellationToken cancellationToken)
    {
        var metodo = MetodosPago.Manuales.FirstOrDefault(
            m => string.Equals(m, request.Metodo?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new MembresiaException(
                $"Método de pago no válido. Usa uno de: {string.Join(", ", MetodosPago.Manuales)}.");

        var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new MembresiaException("El plan no existe.");
        if (!plan.Activo)
        {
            throw new MembresiaException($"El plan {plan.Nombre} está desactivado.");
        }

        Guid? acompananteId = null;
        if (!string.IsNullOrWhiteSpace(request.AcompananteEmail))
        {
            var email = request.AcompananteEmail.Trim();
            acompananteId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new MembresiaException($"No hay un usuario registrado con el correo {email}.");
        }

        var referencia = string.IsNullOrWhiteSpace(request.Referencia)
            ? $"MAN-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetHexString(8)}"
            : request.Referencia.Trim();
        if (await _context.Pagos.AnyAsync(p => p.Referencia == referencia, cancellationToken))
        {
            throw new MembresiaException($"Ya existe un pago con la referencia {referencia}.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var pago = new Pago
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            PlanId = plan.Id,
            AcompananteUsuarioId = acompananteId,
            Monto = plan.Precio, // Se guarda el precio vigente al momento del pago
            Metodo = metodo,
            Estado = EstadosPago.Pendiente,
            Referencia = referencia,
            RegistradoPorAdministradorId = request.AdministradorId,
            Fecha = DateTime.UtcNow
        };
        _context.Pagos.Add(pago);

        var membresia = await _membresias.AplicarPagoAprobadoAsync(pago, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var membresiaResult = await MembresiaConsultas.ProyectarAsync(_context, membresia.Id, cancellationToken);
        return new PagoRegistradoResult(MembresiaConsultas.ToResult(pago, plan), membresiaResult!);
    }
}

public sealed class GetMembresiaActualQueryHandler
    : IRequestHandler<GetMembresiaActualQuery, MembresiaResult?>
{
    private readonly IAppDbContext _context;

    public GetMembresiaActualQueryHandler(IAppDbContext context) => _context = context;

    public async Task<MembresiaResult?> Handle(GetMembresiaActualQuery request, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;
        var delUsuario = _context.Membresias
            .Where(m => m.UsuarioTitularId == request.UsuarioId || m.AcompananteUsuarioId == request.UsuarioId);

        var id = await delUsuario.Where(MembresiaService.EstaVigente(ahora)).Select(m => (Guid?)m.Id).FirstOrDefaultAsync(cancellationToken)
            ?? await delUsuario.OrderByDescending(m => m.FechaVencimiento).Select(m => (Guid?)m.Id).FirstOrDefaultAsync(cancellationToken);

        return id is null ? null : await MembresiaConsultas.ProyectarAsync(_context, id.Value, cancellationToken);
    }
}

public sealed class GetPagosQueryHandler : IRequestHandler<GetPagosQuery, IReadOnlyList<PagoResult>>
{
    private readonly IAppDbContext _context;

    public GetPagosQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<PagoResult>> Handle(GetPagosQuery request, CancellationToken cancellationToken)
    {
        var pagos = _context.Pagos.AsQueryable();
        if (request.UsuarioId is { } usuarioId)
        {
            pagos = pagos.Where(p => p.UsuarioId == usuarioId || p.AcompananteUsuarioId == usuarioId);
        }

        return await pagos
            .OrderByDescending(p => p.Fecha)
            .Join(_context.Planes, p => p.PlanId, plan => plan.Id, (p, plan) => new PagoResult(
                p.Id, p.UsuarioId, p.PlanId, plan.Nombre, p.Monto, p.Moneda, p.Metodo, p.Estado,
                p.Referencia, p.Fecha, p.MembresiaId, p.AcompananteUsuarioId))
            .ToListAsync(cancellationToken);
    }
}

internal static class MembresiaConsultas
{
    public static Task<MembresiaResult?> ProyectarAsync(IAppDbContext context, Guid membresiaId, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;
        return context.Membresias
            .AsNoTracking()
            .Where(m => m.Id == membresiaId)
            .Select(m => new MembresiaResult(
                m.Id,
                m.PlanId,
                m.Plan.Codigo,
                m.Plan.Nombre,
                // Si ya pasó la fecha se informa Vencida aunque el proceso horario no haya corrido.
                m.Estado == EstadosMembresia.Activa && m.FechaVencimiento <= ahora ? EstadosMembresia.Vencida : m.Estado,
                m.FechaInicio,
                m.FechaVencimiento,
                m.UsuarioTitularId,
                context.Users.Where(u => u.Id == m.UsuarioTitularId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? "",
                m.AcompananteUsuarioId,
                context.Users.Where(u => u.Id == m.AcompananteUsuarioId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static PagoResult ToResult(Pago p, Plan plan)
        => new(p.Id, p.UsuarioId, p.PlanId, plan.Nombre, p.Monto, p.Moneda, p.Metodo, p.Estado,
            p.Referencia, p.Fecha, p.MembresiaId, p.AcompananteUsuarioId);
}
