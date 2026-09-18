namespace FitCore.Identity.API.DTOs;

public class EntrenadorRequest
{
    public required string Nombre { get; set; }
    public required string Especialidad { get; set; }
    public required string Horario { get; set; }
    public required string Email { get; set; }
    public required string Contrasena { get; set; }
    public required int Telefono { get; set; }
}
