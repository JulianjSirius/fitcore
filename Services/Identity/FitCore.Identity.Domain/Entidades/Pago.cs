namespace FitCore.Identity.Domain;

// Pago de una membresía. Hoy los registra un administrador (Aprobado al instante);
// Pendiente, Referencia y ProveedorTransaccionId quedan listos para Wompi.
public class Pago
{
    public Guid Id { get; set; }
    // Se llena al aprobar el pago. Un pago Pendiente (Wompi) aún no tiene membresía.
    public Guid? MembresiaId { get; set; }
    public Guid UsuarioId { get; set; } // Titular que paga
    public Guid PlanId { get; set; }
    public Guid? AcompananteUsuarioId { get; set; } // Solo plan Duo
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "COP";
    public required string Metodo { get; set; }
    public required string Estado { get; set; }
    public required string Referencia { get; set; }
    public string? ProveedorTransaccionId { get; set; }
    public Guid? RegistradoPorAdministradorId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public static class MetodosPago
{
    public const string Efectivo = "Efectivo";
    public const string Transferencia = "Transferencia";
    public const string Datafono = "Datafono";
    public const string Wompi = "Wompi";

    public static readonly string[] Manuales = [Efectivo, Transferencia, Datafono];
}

public static class EstadosPago
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";
}
