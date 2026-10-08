using MediatR;
using FitCore.Identity.Application.Features.Membresias.Queries;

namespace FitCore.Identity.Application.Features.Membresias.Commands;

// Pago registrado por un administrador (efectivo, transferencia o datáfono).
public sealed record RegistrarPagoManualCommand(
    Guid UsuarioId,
    Guid PlanId,
    string Metodo,
    string? Referencia,
    string? AcompananteEmail,
    Guid AdministradorId) : IRequest<PagoRegistradoResult>;
