using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.Queries;

namespace FitCore.Workouts.API.DTOs;

public sealed class RutinaRequest
{
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public required string NivelDificultad { get; set; }
    public Guid UsuarioId { get; set; }
    public bool PermitirEdicionEntrenador { get; set; }
    public List<RutinaEjercicioRequest> RutinaEjercicios { get; set; } = [];

    public IReadOnlyList<RutinaEjercicioInput> ToInputs()
        => RutinaEjercicios.Select(item => new RutinaEjercicioInput(
            item.EjercicioId,
            item.Series,
            item.Repeticiones,
            Math.Round(item.PesoKg, 2),
            item.TiempoDescansoSegundos,
            item.OrdenAparicion)).ToList();
}

public sealed class RutinaEjercicioRequest
{
    public Guid EjercicioId { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }
    public decimal PesoKg { get; set; }
    public int TiempoDescansoSegundos { get; set; }
    public int OrdenAparicion { get; set; }
}

public sealed class RutinaResponse
{
    public Guid Id { get; init; }
    public required string Nombre { get; init; }
    public required string Descripcion { get; init; }
    public required string NivelDificultad { get; init; }
    public Guid UsuarioId { get; init; }
    public Guid CreadorId { get; init; }
    public bool PermitirEdicionEntrenador { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaActualizacion { get; init; }
    public required IReadOnlyList<RutinaEjercicioResponse> RutinaEjercicios { get; init; }
    // Solo el PUT lo llena: mejoras de repeticiones o peso que generó la edición.
    public IReadOnlyList<ProgresoResponse> Progresos { get; init; } = [];

    public static RutinaResponse FromResult(RutinaActualizadaResult result)
        => FromResult(result.Rutina, result.Progresos.Select(ProgresoResponse.FromResult).ToList());

    public static RutinaResponse FromResult(RutinaResult result)
        => FromResult(result, []);

    private static RutinaResponse FromResult(RutinaResult result, IReadOnlyList<ProgresoResponse> progresos)
        => new()
        {
            Id = result.Id,
            Nombre = result.Nombre,
            Descripcion = result.Descripcion,
            NivelDificultad = result.NivelDificultad,
            UsuarioId = result.UsuarioId,
            CreadorId = result.CreadorId,
            PermitirEdicionEntrenador = result.PermitirEdicionEntrenador,
            FechaCreacion = result.FechaCreacion,
            FechaActualizacion = result.FechaActualizacion,
            RutinaEjercicios = result.RutinaEjercicios.Select(item => new RutinaEjercicioResponse
            {
                EjercicioId = item.EjercicioId,
                Nombre = item.Nombre,
                GrupoMuscular = item.GrupoMuscular,
                Series = item.Series,
                Repeticiones = item.Repeticiones,
                PesoKg = item.PesoKg,
                TiempoDescansoSegundos = item.TiempoDescansoSegundos,
                OrdenAparicion = item.OrdenAparicion
            }).ToList(),
            Progresos = progresos
        };
}

public sealed class RutinaEjercicioResponse
{
    public Guid EjercicioId { get; init; }
    public required string Nombre { get; init; }
    public required string GrupoMuscular { get; init; }
    public int Series { get; init; }
    public int Repeticiones { get; init; }
    public decimal PesoKg { get; init; }
    public int TiempoDescansoSegundos { get; init; }
    public int OrdenAparicion { get; init; }
}
