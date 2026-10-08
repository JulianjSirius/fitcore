namespace FitCore.Identity.Domain;

public class Entrenador : ICuenta
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string LastName { get; set; } // Agregado para consistencia
    public required string Email { get; set; }
    public long Telefono { get; set; }
    public required string Contrasena { get; set; }

    public required string Especialidad { get; set; }  // Ej: "Hipertrofia", "Crossfit"
    public required string Horario { get; set; }

    // NUEVO: Perfil Profesional
    public required string Biografia { get; set; } = string.Empty; // Resumen para mostrar en la app
    public int AniosExperiencia { get; set; }
    public string FotoPerfilUrl { get; set; } = string.Empty; // Para la UI de Angular/Flutter

    // NUEVO: Estado laboral
    public bool EstaActivo { get; set; } = true; // Si es false, ya no trabaja en el gimnasio
    public DateTime FechaContratacion { get; set; } = DateTime.UtcNow;
}
