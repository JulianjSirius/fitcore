using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.Application.Features.Acceso.DTOs;

public class ValidarAccesoRequest
{
    [Required]
    public required string Token { get; set; }
}
