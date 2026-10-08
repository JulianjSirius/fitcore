namespace FitCore.Identity.Domain;

// Periodo de membresía pagado. Es la fuente de verdad; los campos de membresía de
// User son una copia rápida del estado actual.
public class Membresia
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public Guid UsuarioTitularId { get; set; }
    public Guid? AcompananteUsuarioId { get; set; } // Solo plan Duo
    public string Estado { get; set; } = EstadosMembresia.Activa;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class EstadosMembresia
{
    public const string Activa = "Activa";
    public const string Vencida = "Vencida";
    public const string Inactivo = "Inactivo"; // Valor por defecto de User sin membresía
}
