namespace FitCore.Identity.Domain;

// Un registro por usuario y día en que entró al gimnasio. Lo crea la validación del QR
// cuando el acceso es permitido; las rachas e insignias de asistencia salen de aquí.
public class Asistencia
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateOnly Fecha { get; set; } // Día en hora de Colombia
    public DateTime FechaHoraEntrada { get; set; } // UTC de la primera entrada del día
}
