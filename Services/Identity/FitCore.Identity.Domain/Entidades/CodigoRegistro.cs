namespace FitCore.Identity.Domain;

// Código de un solo uso que un administrador entrega a un cliente para que pueda
// crear su perfil de usuario. Sin un código válido no se permite el registro.
public class CodigoRegistro
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public Guid CreadoPorAdministradorId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }

    public Guid? UsadoPorUsuarioId { get; set; }
    public DateTime? FechaUso { get; set; }
}
