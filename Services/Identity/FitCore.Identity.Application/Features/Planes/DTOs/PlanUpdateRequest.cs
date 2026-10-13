using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.Application.Features.Planes.DTOs;

public class PlanUpdateRequest
{
    [Required]
    public required string Nombre { get; set; }

    [Range(1, 100_000_000, ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [Range(1, 24, ErrorMessage = "La duración debe estar entre 1 y 24 meses.")]
    public int DuracionMeses { get; set; }

    public bool Activo { get; set; } = true;
}
