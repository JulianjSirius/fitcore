namespace FitCore.Workouts.Domain.Entidades;

public class Ejercicio
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string GrupoMuscular { get; set; }
    public required string DescripcionOrientativa { get; set; }

    public ICollection<RutinaEjercicio> RutinaEjercicios { get; set; } = new List<RutinaEjercicio>();
}
