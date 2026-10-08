using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.API.DTOs;

public class EntrenadorRequest
{
    public required string Nombre { get; set; }
    public required string Especialidad { get; set; }
    public required string Horario { get; set; }
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    [Range(1_000_000_000, 9_999_999_999, ErrorMessage = "El teléfono debe tener exactamente 10 dígitos.")]
    public long Telefono { get; set; }
}
