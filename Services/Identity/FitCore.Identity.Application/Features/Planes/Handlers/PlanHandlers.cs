using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Application.Features.Planes.Commands;
using FitCore.Identity.Application.Features.Planes.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Planes.Handlers;

public sealed class GetPlanesQueryHandler : IRequestHandler<GetPlanesQuery, IReadOnlyList<PlanResult>>
{
    private readonly IAppDbContext _context;

    public GetPlanesQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<PlanResult>> Handle(GetPlanesQuery request, CancellationToken cancellationToken)
        => await _context.Planes
            .AsNoTracking()
            .OrderBy(p => p.DuracionMeses).ThenBy(p => p.MaxBeneficiarios)
            .Select(p => new PlanResult(
                p.Id, p.Codigo, p.Nombre, p.Precio, p.DuracionMeses, p.MaxBeneficiarios, p.Activo, p.FechaActualizacion))
            .ToListAsync(cancellationToken);
}

public sealed class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, PlanResult?>
{
    private readonly IAppDbContext _context;

    public UpdatePlanCommandHandler(IAppDbContext context) => _context = context;

    public async Task<PlanResult?> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new MembresiaException("El nombre del plan es obligatorio.");
        }
        if (request.Precio <= 0)
        {
            throw new MembresiaException("El precio debe ser mayor que cero.");
        }
        if (request.DuracionMeses is < 1 or > 24)
        {
            throw new MembresiaException("La duración debe estar entre 1 y 24 meses.");
        }

        var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        // El cambio aplica a pagos futuros; las membresías ya pagadas conservan su vencimiento.
        plan.Nombre = request.Nombre.Trim();
        plan.Precio = request.Precio;
        plan.DuracionMeses = request.DuracionMeses;
        plan.Activo = request.Activo;
        plan.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PlanResult(
            plan.Id, plan.Codigo, plan.Nombre, plan.Precio, plan.DuracionMeses, plan.MaxBeneficiarios, plan.Activo, plan.FechaActualizacion);
    }
}
