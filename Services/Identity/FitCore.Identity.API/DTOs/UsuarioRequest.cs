using System.ComponentModel.DataAnnotations;

namespace FitCore.Identity.API.DTOs;

public class UsuarioRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    [Range(1_000_000_000, 9_999_999_999, ErrorMessage = "El teléfono debe tener exactamente 10 dígitos.")]
    public long telefono { get; set; }
}

// El registro público exige el código que entrega un administrador.
public class CrearUsuarioRequest : UsuarioRequest
{
    public required string CodigoRegistro { get; set; }
}
