using MediatR;

namespace FitCore.Identity.Application.Features.Membresias.Queries;

// Membresía vigente del usuario (como titular o acompañante); si no tiene, la última que tuvo.
public sealed record GetMembresiaActualQuery(Guid UsuarioId) : IRequest<MembresiaResult?>;

public sealed record GetPagosQuery(Guid? UsuarioId) : IRequest<IReadOnlyList<PagoResult>>;

public sealed record MembresiaResult(
    Guid Id,
    Guid PlanId,
    string PlanCodigo,
    string PlanNombre,
    string Estado,
    DateTime FechaInicio,
    DateTime FechaVencimiento,
    Guid UsuarioTitularId,
    string TitularNombre,
    Guid? AcompananteUsuarioId,
    string? AcompananteNombre);

public sealed record PagoResult(
    Guid Id,
    Guid UsuarioId,
    Guid PlanId,
    string PlanNombre,
    decimal Monto,
    string Moneda,
    string Metodo,
    string Estado,
    string Referencia,
    DateTime Fecha,
    Guid? MembresiaId,
    Guid? AcompananteUsuarioId);

public sealed record PagoRegistradoResult(PagoResult Pago, MembresiaResult Membresia);
