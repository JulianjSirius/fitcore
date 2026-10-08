namespace FitCore.Identity.Domain;

public class Administrador : ICuenta
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public long Telefono { get; set; }
    public required string Contrasena { get; set; }

    public required string NivelAcceso { get; set; } // Ej: "SuperAdmin", "Recepcionista"

    // NUEVO: Auditoría y Control
    public bool EstaActivo { get; set; } = true; // Para revocar accesos sin borrar el registro // Fecha del último login exitoso
    public string SucursalId { get; set; } = string.Empty; // Opcional, por si FitCore maneja múltiples sedes en el futuro
}
