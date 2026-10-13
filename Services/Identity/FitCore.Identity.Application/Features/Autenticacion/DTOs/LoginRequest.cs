namespace FitCore.Identity.Application.Features.Autenticacion.DTOs;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
}
