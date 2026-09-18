using FitCore.Workouts.Application.Abstractions;
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
        IQueryable<Rutina> query = context.Rutinas
            .AsNoTracking()
            .Include(rutina => rutina.RutinaEjercicios)
            .ThenInclude(rutinaEjercicio => rutinaEjercicio.Ejercicio);

        var rutinas = request.UsuarioId is null
            ? query
            : query.Where(rutina => rutina.UsuarioId == request.UsuarioId.Value);

        var entities = await rutinas.ToListAsync(cancellationToken);
        return entities.Select(ToResult).ToList();
    }

    private static RutinaResult ToResult(Rutina rutina)
        => new(
            rutina.Id,
            rutina.Nombre,
            rutina.Descripcion,
            rutina.NivelDificultad,
            rutina.UsuarioId,
            rutina.CreadorId,
            rutina.PermitirEdicionEntrenador,
            rutina.FechaCreacion,
            rutina.FechaActualizacion,
            rutina.RutinaEjercicios
                .OrderBy(item => item.OrdenAparicion)
                .Select(item => new RutinaEjercicioResult(
                    item.EjercicioId,
                    item.Ejercicio.Nombre,
                    item.Ejercicio.GrupoMuscular,
                    item.Series,
                    item.Repeticiones,
                    item.TiempoDescansoSegundos,
                    item.OrdenAparicion))
                .ToList());
}

public sealed class GetRutinaByIdQueryHandler : IRequestHandler<GetRutinaByIdQuery, RutinaResult?>
{
    private readonly IWorkoutsDbContext context;

    public GetRutinaByIdQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<RutinaResult?> Handle(GetRutinaByIdQuery request, CancellationToken cancellationToken)
        => await context.Rutinas
            .AsNoTracking()
            .Include(rutina => rutina.RutinaEjercicios)
            .ThenInclude(rutinaEjercicio => rutinaEjercicio.Ejercicio)
            .Where(rutina => rutina.Id == request.Id)
            .Select(rutina => new RutinaResult(
                rutina.Id,
                rutina.Nombre,
                rutina.Descripcion,
                rutina.NivelDificultad,
                rutina.UsuarioId,
                rutina.CreadorId,
                rutina.PermitirEdicionEntrenador,
                rutina.FechaCreacion,
                rutina.FechaActualizacion,
                rutina.RutinaEjercicios
                    .OrderBy(item => item.OrdenAparicion)
                    .Select(item => new RutinaEjercicioResult(
                        item.EjercicioId,
                        item.Ejercicio.Nombre,
                        item.Ejercicio.GrupoMuscular,
                        item.Series,
                        item.Repeticiones,
                        item.TiempoDescansoSegundos,
                        item.OrdenAparicion))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateRutinaCommandHandler : IRequestHandler<CreateRutinaCommand, RutinaResult>
{
    private readonly IWorkoutsDbContext context;

    public CreateRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<RutinaResult> Handle(CreateRutinaCommand request, CancellationToken cancellationToken)
    {
        ValidateRole(request.Rol);
        if (request.Rol == "Usuario" && request.UsuarioId != request.CreadorId)
        {
            throw new UnauthorizedAccessException("Un usuario solo puede crear rutinas para sí mismo.");
        }
        ValidateInputs(request.RutinaEjercicios);
        await EnsureExercisesExist(request.RutinaEjercicios, cancellationToken);

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

        AddExercises(rutina, request.RutinaEjercicios);
        context.Rutinas.Add(rutina);
        await context.SaveChangesAsync(cancellationToken);

        return await LoadResult(rutina.Id, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo cargar la rutina creada.");
    }

    private async Task EnsureExercisesExist(
        IReadOnlyList<RutinaEjercicioInput> inputs,
        CancellationToken cancellationToken)
    {
        var ids = inputs.Select(input => input.EjercicioId).Distinct().ToList();
        var count = await context.Ejercicios.CountAsync(ejercicio => ids.Contains(ejercicio.Id), cancellationToken);
        if (count != ids.Count)
        {
            throw new InvalidOperationException("Una o más referencias de ejercicio no existen.");
        }
    }

    private async Task<RutinaResult?> LoadResult(Guid id, CancellationToken cancellationToken)
        => await context.Rutinas
            .AsNoTracking()
            .Include(rutina => rutina.RutinaEjercicios)
            .ThenInclude(item => item.Ejercicio)
            .Where(rutina => rutina.Id == id)
            .Select(rutina => new RutinaResult(
                rutina.Id, rutina.Nombre, rutina.Descripcion, rutina.NivelDificultad,
                rutina.UsuarioId, rutina.CreadorId, rutina.PermitirEdicionEntrenador,
                rutina.FechaCreacion, rutina.FechaActualizacion,
                rutina.RutinaEjercicios.OrderBy(item => item.OrdenAparicion).Select(item => new RutinaEjercicioResult(
                    item.EjercicioId, item.Ejercicio.Nombre, item.Ejercicio.GrupoMuscular,
                    item.Series, item.Repeticiones, item.TiempoDescansoSegundos, item.OrdenAparicion)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    internal static void AddExercises(Rutina rutina, IReadOnlyList<RutinaEjercicioInput> inputs)
    {
        foreach (var input in inputs)
        {
            rutina.RutinaEjercicios.Add(new RutinaEjercicio
            {
                RutinaId = rutina.Id,
                EjercicioId = input.EjercicioId,
                Series = input.Series,
                Repeticiones = input.Repeticiones,
                TiempoDescansoSegundos = input.TiempoDescansoSegundos,
                OrdenAparicion = input.OrdenAparicion
            });
        }
    }

    internal static void ValidateInputs(IReadOnlyList<RutinaEjercicioInput> inputs)
    {
        if (inputs.Select(input => input.EjercicioId).Distinct().Count() != inputs.Count)
        {
            throw new InvalidOperationException("No se puede repetir un ejercicio dentro de una rutina.");
        }

        if (inputs.Any(input => input.Series <= 0 || input.Repeticiones <= 0 || input.TiempoDescansoSegundos < 0 || input.OrdenAparicion <= 0))
        {
            throw new InvalidOperationException("Los valores de los ejercicios de la rutina no son válidos.");
        }
    }

    internal static void ValidateRole(string role)
    {
        if (role is not ("Usuario" or "Entrenador"))
        {
            throw new UnauthorizedAccessException("El rol no puede crear rutinas.");
        }
    }
}

public sealed class UpdateRutinaCommandHandler : IRequestHandler<UpdateRutinaCommand, RutinaResult?>
{
    private readonly IWorkoutsDbContext context;

    public UpdateRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<RutinaResult?> Handle(UpdateRutinaCommand request, CancellationToken cancellationToken)
    {
        var rutina = await context.Rutinas
            .Include(item => item.RutinaEjercicios)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (rutina is null || !CanEdit(rutina, request.ActorId, request.Rol))
        {
            return null;
        }

        CreateRutinaCommandHandler.ValidateInputs(request.RutinaEjercicios);
        var exerciseIds = request.RutinaEjercicios.Select(item => item.EjercicioId).Distinct().ToList();
        if (await context.Ejercicios.CountAsync(item => exerciseIds.Contains(item.Id), cancellationToken) != exerciseIds.Count)
        {
            throw new InvalidOperationException("Una o más referencias de ejercicio no existen.");
        }

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

        foreach (var input in request.RutinaEjercicios)
        {
            if (rutina.RutinaEjercicios.FirstOrDefault(item => item.EjercicioId == input.EjercicioId) is { } existing)
            {
                existing.Series = input.Series;
                existing.Repeticiones = input.Repeticiones;
                existing.TiempoDescansoSegundos = input.TiempoDescansoSegundos;
                existing.OrdenAparicion = input.OrdenAparicion;
            }
            else
            {
                rutina.RutinaEjercicios.Add(new RutinaEjercicio
                {
                    RutinaId = rutina.Id,
                    EjercicioId = input.EjercicioId,
                    Series = input.Series,
                    Repeticiones = input.Repeticiones,
                    TiempoDescansoSegundos = input.TiempoDescansoSegundos,
                    OrdenAparicion = input.OrdenAparicion
                });
            }
        }
        await context.SaveChangesAsync(cancellationToken);

        return await context.Rutinas
            .AsNoTracking()
            .Include(item => item.RutinaEjercicios)
            .ThenInclude(item => item.Ejercicio)
            .Where(item => item.Id == request.Id)
            .Select(item => new RutinaResult(
                item.Id, item.Nombre, item.Descripcion, item.NivelDificultad, item.UsuarioId, item.CreadorId,
                item.PermitirEdicionEntrenador, item.FechaCreacion, item.FechaActualizacion,
                item.RutinaEjercicios.OrderBy(link => link.OrdenAparicion).Select(link => new RutinaEjercicioResult(
                    link.EjercicioId, link.Ejercicio.Nombre, link.Ejercicio.GrupoMuscular,
                    link.Series, link.Repeticiones, link.TiempoDescansoSegundos, link.OrdenAparicion)).ToList()))
            .FirstAsync(cancellationToken);
    }

    private static bool CanEdit(Rutina rutina, Guid actorId, string role)
        => role == "Usuario" && rutina.UsuarioId == actorId
            || role == "Entrenador" && rutina.PermitirEdicionEntrenador;
}

public sealed class DeleteRutinaCommandHandler : IRequestHandler<DeleteRutinaCommand, bool>
{
    private readonly IWorkoutsDbContext context;

    public DeleteRutinaCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<bool> Handle(DeleteRutinaCommand request, CancellationToken cancellationToken)
    {
        var rutina = await context.Rutinas.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (rutina is null || !(request.Rol == "Usuario" && rutina.UsuarioId == request.ActorId || request.Rol == "Entrenador" && rutina.PermitirEdicionEntrenador))
        {
            return false;
        }

        context.Rutinas.Remove(rutina);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
