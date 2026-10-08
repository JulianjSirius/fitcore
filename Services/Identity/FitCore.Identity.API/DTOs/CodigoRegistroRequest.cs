using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.API.DTOs;

public class CodigoRegistroRequest
{
    [Range(1, 90, ErrorMessage = "La vigencia debe estar entre 1 y 90 días.")]
    public int DiasVigencia { get; set; } = 7;
}
