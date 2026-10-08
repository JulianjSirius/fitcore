using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.API.DTOs;

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

public class RegistrarPagoRequest
{
    public Guid UsuarioId { get; set; }
    public Guid PlanId { get; set; }

    [Required]
    public required string Metodo { get; set; } // Efectivo, Transferencia o Datafono

    [MaxLength(64)]
    public string? Referencia { get; set; } // Comprobante; si se omite se genera una

    [EmailAddress]
    public string? AcompananteEmail { get; set; } // Obligatorio en el plan Duo
}

public class ValidarAccesoRequest
{
    [Required]
    public required string Token { get; set; }
}
