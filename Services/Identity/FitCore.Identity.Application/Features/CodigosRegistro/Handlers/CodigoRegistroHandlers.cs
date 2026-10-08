using System.Security.Cryptography;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.CodigosRegistro.Commands;
using FitCore.Identity.Application.Features.CodigosRegistro.Queries;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.CodigosRegistro.Handlers;

public sealed class GetCodigosRegistroQueryHandler
    : IRequestHandler<GetCodigosRegistroQuery, IReadOnlyList<CodigoRegistroResult>>
{
    private readonly IAppDbContext _context;

    public GetCodigosRegistroQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<CodigoRegistroResult>> Handle(
        GetCodigosRegistroQuery request,
        CancellationToken cancellationToken)
        => await _context.CodigosRegistro
            .OrderByDescending(c => c.FechaCreacion)
            .Select(c => new CodigoRegistroResult(
                c.Id, c.Codigo, c.CreadoPorAdministradorId, c.FechaCreacion,
                c.FechaExpiracion, c.UsadoPorUsuarioId, c.FechaUso))
            .ToListAsync(cancellationToken);
}

public sealed class GenerarCodigoRegistroCommandHandler
    : IRequestHandler<GenerarCodigoRegistroCommand, CodigoRegistroResult>
{
    // Sin 0/O ni 1/I para que el código se pueda dictar o copiar sin confusiones.
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Longitud = 10;

    private readonly IAppDbContext _context;

    public GenerarCodigoRegistroCommandHandler(IAppDbContext context) => _context = context;

    public async Task<CodigoRegistroResult> Handle(
        GenerarCodigoRegistroCommand request,
        CancellationToken cancellationToken)
    {
        var codigo = new CodigoRegistro
        {
            Id = Guid.NewGuid(),
            Codigo = RandomNumberGenerator.GetString(Alfabeto, Longitud),
            CreadoPorAdministradorId = request.AdministradorId,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(request.DiasVigencia)
        };

        _context.CodigosRegistro.Add(codigo);
        await _context.SaveChangesAsync(cancellationToken);

        return new CodigoRegistroResult(
            codigo.Id, codigo.Codigo, codigo.CreadoPorAdministradorId, codigo.FechaCreacion,
            codigo.FechaExpiracion, codigo.UsadoPorUsuarioId, codigo.FechaUso);
    }
}
