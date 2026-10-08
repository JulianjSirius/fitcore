namespace FitCore.Identity.Domain;

// Planes de membresía del gimnasio. Existen tres fijos (Mensual, Duo y Anual);
// el administrador puede cambiar su precio y duración, pero no crear otros.
public class Plan
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; } // Mensual, Duo, Anual
    public required string Nombre { get; set; }
    public decimal Precio { get; set; } // COP
    public int DuracionMeses { get; set; }
    public int MaxBeneficiarios { get; set; } = 1; // Duo = 2 (titular + acompañante)
    public bool Activo { get; set; } = true;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

public static class CodigosPlan
{
    public const string Mensual = "Mensual";
    public const string Duo = "Duo";
    public const string Anual = "Anual";
}
