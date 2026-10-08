namespace FitCore.Identity.API.DTOs;

public class AdministradorResponse
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public long Telefono { get; set; }
    public required string NivelAcceso { get; set; }
}
