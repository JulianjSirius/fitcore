using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Logros.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Logros.Handlers;

public sealed class GetMiAsistenciaQueryHandler : IRequestHandler<GetMiAsistenciaQuery, AsistenciaResumenResult>
{
    private readonly IAppDbContext _context;

    public GetMiAsistenciaQueryHandler(IAppDbContext context) => _context = context;

    public async Task<AsistenciaResumenResult> Handle(GetMiAsistenciaQuery request, CancellationToken cancellationToken)
    {
        var fechas = await _context.Asistencias
            .Where(a => a.UsuarioId == request.UsuarioId)
            .Select(a => a.Fecha)
            .ToListAsync(cancellationToken);

        return CalculadoraAsistencia.Calcular(fechas, CalendarioGimnasio.Hoy());
    }
}
