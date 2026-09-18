namespace FitCore.Identity.Domain;

public class User
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    public required int telefono { get; set; }
}