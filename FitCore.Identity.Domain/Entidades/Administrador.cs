namespace FitCore.Identity.Domain;

public class Administrador
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    public required string Telefono { get; set; }
    public required string NivelAcceso { get; set; }
}
