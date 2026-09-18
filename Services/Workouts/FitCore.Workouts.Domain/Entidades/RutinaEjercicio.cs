namespace FitCore.Workouts.Domain.Entidades;

public class RutinaEjercicio
{
    public Guid RutinaId { get; set; }
    public Guid EjercicioId { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }
    public int TiempoDescansoSegundos { get; set; }
    public int OrdenAparicion { get; set; }

    public Rutina Rutina { get; set; } = null!;
    public Ejercicio Ejercicio { get; set; } = null!;
}
