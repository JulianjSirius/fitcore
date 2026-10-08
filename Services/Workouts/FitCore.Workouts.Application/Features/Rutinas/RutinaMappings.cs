using System.Linq.Expressions;
using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using FitCore.Workouts.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Features.Rutinas;

internal static class RutinaMappings
{
    // Expresión traducible a SQL: se usa en los Select de EF para proyectar directamente.
    public static readonly Expression<Func<Rutina, RutinaResult>> ToResult = rutina => new RutinaResult(
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
                item.PesoKg,
                item.TiempoDescansoSegundos,
                item.OrdenAparicion))
            .ToList());

    public static Task<RutinaResult?> LoadResultAsync(
        this IWorkoutsDbContext context,
        Guid id,
        CancellationToken cancellationToken)
        => context.Rutinas
            .AsNoTracking()
            .Where(rutina => rutina.Id == id)
            .Select(ToResult)
            .FirstOrDefaultAsync(cancellationToken);
}

internal static class RutinaReglas
{
    // Cabe en numeric(6,2) y descarta errores de digitación.
    public const decimal PesoMaximoKg = 1000m;

    public static bool PuedeModificar(Rutina rutina, Guid actorId, string rol)
        => rol == "Usuario" && rutina.UsuarioId == actorId
            || rol == "Entrenador" && rutina.PermitirEdicionEntrenador;

    public static void ValidarRolCreador(string rol)
    {
        if (rol is not ("Usuario" or "Entrenador"))
        {
            throw new UnauthorizedAccessException("El rol no puede crear rutinas.");
        }
    }

    public static void ValidarEjercicios(IReadOnlyList<RutinaEjercicioInput> inputs)
    {
        if (inputs.Count == 0)
        {
            throw new InvalidOperationException("Una rutina debe tener al menos un ejercicio.");
        }

        if (inputs.Select(input => input.EjercicioId).Distinct().Count() != inputs.Count)
        {
            throw new InvalidOperationException("No se puede repetir un ejercicio dentro de una rutina.");
        }

        if (inputs.Any(input => input.Series <= 0 || input.Repeticiones <= 0 || input.TiempoDescansoSegundos < 0 || input.OrdenAparicion <= 0))
        {
            throw new InvalidOperationException("Los valores de los ejercicios de la rutina no son válidos.");
        }

        if (inputs.Any(input => input.PesoKg < 0 || input.PesoKg > PesoMaximoKg))
        {
            throw new InvalidOperationException($"El peso debe estar entre 0 y {PesoMaximoKg:0} kg.");
        }
    }

    public static async Task AsegurarQueEjerciciosExisten(
        IWorkoutsDbContext context,
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

    public static RutinaEjercicio CrearEnlace(Guid rutinaId, RutinaEjercicioInput input)
        => new()
        {
            RutinaId = rutinaId,
            EjercicioId = input.EjercicioId,
            Series = input.Series,
            Repeticiones = input.Repeticiones,
            PesoKg = input.PesoKg,
            TiempoDescansoSegundos = input.TiempoDescansoSegundos,
            OrdenAparicion = input.OrdenAparicion
        };
}
