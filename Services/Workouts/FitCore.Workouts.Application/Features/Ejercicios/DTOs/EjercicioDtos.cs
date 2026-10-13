namespace FitCore.Workouts.Application.Features.Ejercicios.DTOs;

public sealed class EjercicioRequest
{
    public required string Nombre { get; set; }
    public required string GrupoMuscular { get; set; }
    public required string DescripcionOrientativa { get; set; }
}

public sealed class EjercicioResponse
{
    public Guid Id { get; init; }
    public required string Nombre { get; init; }
    public required string GrupoMuscular { get; init; }
    public required string DescripcionOrientativa { get; init; }
}
