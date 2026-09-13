using FitCore.Identity.Application.Features.Entrenadores.Commands;
using FitCore.Identity.Application.Features.Entrenadores.Queries;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Entrenadores.Handlers;

public sealed class GetEntrenadoresQueryHandler
    : IRequestHandler<GetEntrenadoresQuery, IReadOnlyList<EntrenadorResult>>
{
    private readonly IAppDbContext _context;

    public GetEntrenadoresQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<EntrenadorResult>> Handle(
        GetEntrenadoresQuery request,
        CancellationToken cancellationToken)
        => await _context.Entrenadores
            .Select(e => new EntrenadorResult(
                e.Id, e.Nombre, e.Especialidad, e.Horario, e.Email, e.Telefono))
            .ToListAsync(cancellationToken);
}

public sealed class GetEntrenadorByIdQueryHandler
    : IRequestHandler<GetEntrenadorByIdQuery, EntrenadorResult?>
{
    private readonly IAppDbContext _context;

    public GetEntrenadorByIdQueryHandler(IAppDbContext context) => _context = context;

    public async Task<EntrenadorResult?> Handle(
        GetEntrenadorByIdQuery request,
        CancellationToken cancellationToken)
        => await _context.Entrenadores
            .Where(e => e.Id == request.Id)
            .Select(e => new EntrenadorResult(
                e.Id, e.Nombre, e.Especialidad, e.Horario, e.Email, e.Telefono))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateEntrenadorCommandHandler
    : IRequestHandler<CreateEntrenadorCommand, EntrenadorResult>
{
    private readonly IAppDbContext _context;

    public CreateEntrenadorCommandHandler(IAppDbContext context) => _context = context;

    public async Task<EntrenadorResult> Handle(
        CreateEntrenadorCommand request,
        CancellationToken cancellationToken)
    {
        var entrenador = new Entrenador
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Especialidad = request.Especialidad,
            Horario = request.Horario,
            Email = request.Email,
            Contrasena = request.Contrasena,
            Telefono = request.Telefono
        };

        _context.Entrenadores.Add(entrenador);
        await _context.SaveChangesAsync(cancellationToken);

        return new EntrenadorResult(
            entrenador.Id,
            entrenador.Nombre,
            entrenador.Especialidad,
            entrenador.Horario,
            entrenador.Email,
            entrenador.Telefono);
    }
}

public sealed class UpdateEntrenadorCommandHandler
    : IRequestHandler<UpdateEntrenadorCommand, bool>
{
    private readonly IAppDbContext _context;

    public UpdateEntrenadorCommandHandler(IAppDbContext context) => _context = context;

    public async Task<bool> Handle(
        UpdateEntrenadorCommand request,
        CancellationToken cancellationToken)
    {
        var entrenador = await _context.Entrenadores.FindAsync([request.Id], cancellationToken);
        if (entrenador is null)
        {
            return false;
        }

        entrenador.Nombre = request.Nombre;
        entrenador.Especialidad = request.Especialidad;
        entrenador.Horario = request.Horario;
        entrenador.Email = request.Email;
        entrenador.Contrasena = request.Contrasena;
        entrenador.Telefono = request.Telefono;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
