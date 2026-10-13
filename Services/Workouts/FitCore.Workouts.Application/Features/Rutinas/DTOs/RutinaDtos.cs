using FitCore.Workouts.Application.Features.Progreso.DTOs;

namespace FitCore.Workouts.Application.Features.Rutinas.DTOs;

public sealed class RutinaRequest
{
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public required string NivelDificultad { get; set; }
    public Guid UsuarioId { get; set; }
    public bool PermitirEdicionEntrenador { get; set; }
    public List<RutinaEjercicioRequest> RutinaEjercicios { get; set; } = [];
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
