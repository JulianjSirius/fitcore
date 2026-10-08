namespace FitCore.Workouts.Domain.Entidades;

public class Rutina
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public required string NivelDificultad { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CreadorId { get; set; }
    public bool PermitirEdicionEntrenador { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<RutinaEjercicio> RutinaEjercicios { get; set; } = new List<RutinaEjercicio>();
}
