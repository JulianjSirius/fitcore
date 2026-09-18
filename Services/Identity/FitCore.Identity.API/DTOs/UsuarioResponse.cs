namespace FitCore.Identity.API.DTOs;

public class UsuarioResponse
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required int telefono { get; set; }
}
