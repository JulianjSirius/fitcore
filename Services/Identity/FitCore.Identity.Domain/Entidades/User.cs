namespace FitCore.Identity.Domain;

public class User : ICuenta
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public long Telefono { get; set; }
    public required string Contrasena { get; set; }

    // NUEVO: Control de Acceso y Membresía
    public required string EstadoMembresia { get; set; } = "Inactivo"; // Ej: Activo, Vencido, Suspendido
    public required string TipoMembresia { get; set; } // Ej: Basica, Premium, VIP
    public required DateTime? FechaVencimientoMembresia { get; set; }
    public required string CodigoAccesoQR { get; set; }  // Token único para generar el QR de entrada

    // NUEVO: Datos Biométricos y de Salud (Opcionales pero recomendados)
    public string CondicionesMedicas { get; set; } = string.Empty; // Ej: "Asma", "Lesión de rodilla"

    // NUEVO: Seguridad
    public string ContactoEmergenciaNombre { get; set; } = string.Empty;
    public string ContactoEmergenciaTelefono { get; set; } = string.Empty;
}