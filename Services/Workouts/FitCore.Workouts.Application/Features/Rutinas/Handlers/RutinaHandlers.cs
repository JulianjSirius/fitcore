using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Application.Features.Progreso;
using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using FitCore.Workouts.Domain.Entidades;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Features.Rutinas.Handlers;

public sealed class GetRutinasQueryHandler : IRequestHandler<GetRutinasQuery, IReadOnlyList<RutinaResult>>
{
    private readonly IWorkoutsDbContext context;

    public GetRutinasQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<IReadOnlyList<RutinaResult>> Handle(GetRutinasQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Rutina> query = context.Rutinas.AsNoTracking();

        if (request.UsuarioId is not null)
        {
            query = query.Where(rutina => rutina.UsuarioId == request.UsuarioId.Value);
        }

        return await query.Select(RutinaMappings.ToResult).ToListAsync(cancellationToken);
    }
}

public sealed class GetRutinaByIdQueryHandler : IRequestHandler<GetRutinaByIdQuery, RutinaResult?>
{
    private readonly IWorkoutsDbContext context;

    public GetRutinaByIdQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public Task<RutinaResult?> Handle(GetRutinaByIdQuery request, CancellationToken cancellationToken)
        => context.LoadResultAsync(request.Id, cancellationToken);
}

public sealed class CreateRutinaCommandHandler : IRequestHandler<CreateRutinaCommand, RutinaResult>
{
    private readonly IWorkoutsDbContext context;

    public CreateRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<RutinaResult> Handle(CreateRutinaCommand request, CancellationToken cancellationToken)
    {
        RutinaReglas.ValidarRolCreador(request.Rol);
        if (request.Rol == "Usuario" && request.UsuarioId != request.CreadorId)
        {
            throw new UnauthorizedAccessException("Un usuario solo puede crear rutinas para sí mismo.");
        }
        RutinaReglas.ValidarEjercicios(request.RutinaEjercicios);
        await RutinaReglas.AsegurarQueEjerciciosExisten(context, request.RutinaEjercicios, cancellationToken);

        var rutina = new Rutina
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            NivelDificultad = request.NivelDificultad,
            UsuarioId = request.UsuarioId,
            CreadorId = request.CreadorId,
            PermitirEdicionEntrenador = request.PermitirEdicionEntrenador,
            FechaCreacion = DateTime.UtcNow
        };

        foreach (var input in request.RutinaEjercicios)
        {
            rutina.RutinaEjercicios.Add(RutinaReglas.CrearEnlace(rutina.Id, input));
        }

        context.Rutinas.Add(rutina);
        await context.SaveChangesAsync(cancellationToken);

        return await context.LoadResultAsync(rutina.Id, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo cargar la rutina creada.");
    }
}

public sealed class UpdateRutinaCommandHandler : IRequestHandler<UpdateRutinaCommand, RutinaActualizadaResult?>
{
    private readonly IWorkoutsDbContext context;

    public UpdateRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<RutinaActualizadaResult?> Handle(UpdateRutinaCommand request, CancellationToken cancellationToken)
    {
        var rutina = await context.Rutinas
            .Include(item => item.RutinaEjercicios)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (rutina is null || !RutinaReglas.PuedeModificar(rutina, request.ActorId, request.Rol))
        {
            return null;
        }

        RutinaReglas.ValidarEjercicios(request.RutinaEjercicios);
        await RutinaReglas.AsegurarQueEjerciciosExisten(context, request.RutinaEjercicios, cancellationToken);

        rutina.Nombre = request.Nombre;
        rutina.Descripcion = request.Descripcion;
        rutina.NivelDificultad = request.NivelDificultad;
        rutina.PermitirEdicionEntrenador = request.PermitirEdicionEntrenador;
        rutina.FechaActualizacion = DateTime.UtcNow;

        var requestedExercises = request.RutinaEjercicios.ToDictionary(item => item.EjercicioId);
        foreach (var existing in rutina.RutinaEjercicios.ToList())
        {
            if (!requestedExercises.ContainsKey(existing.EjercicioId))
            {
                context.RutinaEjercicios.Remove(existing);
            }
        }

        var cambios = new List<CambioEjercicio>();
        foreach (var input in request.RutinaEjercicios)
        {
            if (rutina.RutinaEjercicios.FirstOrDefault(item => item.EjercicioId == input.EjercicioId) is { } existing)
            {
                cambios.Add(new CambioEjercicio(
                    input.EjercicioId, existing.Repeticiones, input.Repeticiones, existing.PesoKg, input.PesoKg));
                existing.Series = input.Series;
                existing.Repeticiones = input.Repeticiones;
                existing.PesoKg = input.PesoKg;
                existing.TiempoDescansoSegundos = input.TiempoDescansoSegundos;
                existing.OrdenAparicion = input.OrdenAparicion;
            }
            else
            {
                rutina.RutinaEjercicios.Add(RutinaReglas.CrearEnlace(rutina.Id, input));
            }
        }
        var progresos = await RegistroProgreso.RegistrarAsync(context, rutina, cambios, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var result = await context.LoadResultAsync(request.Id, cancellationToken);
        return result is null
            ? null
            : new RutinaActualizadaResult(result, progresos.Select(RegistroProgreso.ToResult).ToList());
    }
}

public sealed class DeleteRutinaCommandHandler : IRequestHandler<DeleteRutinaCommand, bool>
{
    private readonly IWorkoutsDbContext context;

    public DeleteRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<bool> Handle(DeleteRutinaCommand request, CancellationToken cancellationToken)
    {
        var rutina = await context.Rutinas.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (rutina is null || !RutinaReglas.PuedeModificar(rutina, request.ActorId, request.Rol))
        {
            return false;
        }

        context.Rutinas.Remove(rutina);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
