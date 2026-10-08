namespace FitCore.Workouts.Domain.Entidades;

// Mejora registrada al editar una rutina: subieron las repeticiones o el peso de un ejercicio.
// No tiene llaves foráneas para que el historial sobreviva si se borra la rutina o el ejercicio.
public class ProgresoEjercicio
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid RutinaId { get; set; }
    public Guid EjercicioId { get; set; }
    public required string NombreEjercicio { get; set; }
    public int RepeticionesAnteriores { get; set; }
    public int RepeticionesNuevas { get; set; }
    public decimal PesoAnteriorKg { get; set; }
    public decimal PesoNuevoKg { get; set; }
    public bool EsRecordPersonal { get; set; }
    public DateTime Fecha { get; set; }
}
