using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.Application.Features.Membresias.DTOs;

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
