using MediatR;
using FitCore.Identity.Application.Features.CodigosRegistro.Queries;

namespace FitCore.Identity.Application.Features.CodigosRegistro.Commands;

public sealed record GenerarCodigoRegistroCommand(
    Guid AdministradorId,
    int DiasVigencia) : IRequest<CodigoRegistroResult>;
