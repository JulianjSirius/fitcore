namespace FitCore.Identity.API.DTOs;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
}
