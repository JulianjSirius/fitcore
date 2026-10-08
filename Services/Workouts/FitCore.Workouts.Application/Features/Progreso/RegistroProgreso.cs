using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using FitCore.Workouts.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Features.Progreso;

// Valores de un ejercicio antes y después de editar la rutina.
public sealed record CambioEjercicio(
    Guid EjercicioId,
    int RepeticionesAnteriores,
    int RepeticionesNuevas,
    decimal PesoAnteriorKg,
    decimal PesoNuevoKg)
{
    public bool EsMejora => RepeticionesNuevas > RepeticionesAnteriores || PesoNuevoKg > PesoAnteriorKg;
}

internal static class RegistroProgreso
{
    // Agrega al contexto un progreso por cada ejercicio que subió repeticiones o peso.
    // No guarda: lo hace el handler junto con la rutina.
    public static async Task<IReadOnlyList<ProgresoEjercicio>> RegistrarAsync(
        IWorkoutsDbContext context,
        Rutina rutina,
        IEnumerable<CambioEjercicio> cambios,
        CancellationToken cancellationToken)
    {
        var mejoras = cambios.Where(cambio => cambio.EsMejora).ToList();
        if (mejoras.Count == 0)
        {
            return [];
        }

        var ids = mejoras.Select(mejora => mejora.EjercicioId).ToList();
        var nombres = await context.Ejercicios
            .Where(ejercicio => ids.Contains(ejercicio.Id))
            .ToDictionaryAsync(ejercicio => ejercicio.Id, ejercicio => ejercicio.Nombre, cancellationToken);
        var marcasPrevias = await context.ProgresosEjercicio
            .Where(progreso => progreso.UsuarioId == rutina.UsuarioId && ids.Contains(progreso.EjercicioId))
            .Select(progreso => new { progreso.EjercicioId, progreso.PesoNuevoKg, progreso.RepeticionesNuevas })
            .ToListAsync(cancellationToken);

        var ahora = DateTime.UtcNow;
        var progresos = mejoras.Select(mejora =>
        {
            // Récord personal: supera el valor anterior y la mejor marca histórica del usuario en ese ejercicio.
            var esRecord = CalculadoraProgreso.SuperaMarca(
                    mejora.PesoNuevoKg, mejora.RepeticionesNuevas, mejora.PesoAnteriorKg, mejora.RepeticionesAnteriores)
                && marcasPrevias
                    .Where(marca => marca.EjercicioId == mejora.EjercicioId)
                    .All(marca => CalculadoraProgreso.SuperaMarca(
                        mejora.PesoNuevoKg, mejora.RepeticionesNuevas, marca.PesoNuevoKg, marca.RepeticionesNuevas));

            return new ProgresoEjercicio
            {
                Id = Guid.NewGuid(),
                UsuarioId = rutina.UsuarioId,
                RutinaId = rutina.Id,
                EjercicioId = mejora.EjercicioId,
                NombreEjercicio = nombres.GetValueOrDefault(mejora.EjercicioId, "Ejercicio"),
                RepeticionesAnteriores = mejora.RepeticionesAnteriores,
                RepeticionesNuevas = mejora.RepeticionesNuevas,
                PesoAnteriorKg = mejora.PesoAnteriorKg,
                PesoNuevoKg = mejora.PesoNuevoKg,
                EsRecordPersonal = esRecord,
                Fecha = ahora
            };
        }).ToList();

        context.ProgresosEjercicio.AddRange(progresos);
        return progresos;
    }

    public static ProgresoResult ToResult(ProgresoEjercicio progreso)
        => new(
            progreso.Id,
            progreso.RutinaId,
            progreso.EjercicioId,
            progreso.NombreEjercicio,
            progreso.RepeticionesAnteriores,
            progreso.RepeticionesNuevas,
            progreso.PesoAnteriorKg,
            progreso.PesoNuevoKg,
            progreso.EsRecordPersonal,
            progreso.Fecha);
}
