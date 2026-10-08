namespace FitCore.Identity.API.DTOs;

public class EntrenadorResponse
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string Especialidad { get; set; }
    public required string Horario { get; set; }
    public required string Email { get; set; }
    public long Telefono { get; set; }
}
