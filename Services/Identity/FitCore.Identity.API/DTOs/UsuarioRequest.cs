namespace FitCore.Identity.API.DTOs;

public class UsuarioRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    public required int telefono { get; set; }
}
