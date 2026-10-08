using MediatR;
using FitCore.Identity.Application.Features.Planes.Queries;

namespace FitCore.Identity.Application.Features.Planes.Commands;

// Los planes son fijos (Mensual, Duo, Anual): el administrador solo edita sus valores.
public sealed record UpdatePlanCommand(
    Guid Id,
    string Nombre,
    decimal Precio,
    int DuracionMeses,
    bool Activo) : IRequest<PlanResult?>;
