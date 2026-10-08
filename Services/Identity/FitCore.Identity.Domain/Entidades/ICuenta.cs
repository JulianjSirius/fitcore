namespace FitCore.Identity.Domain;

// Datos comunes que necesita el login para cualquier tipo de cuenta.
public interface ICuenta
{
    Guid Id { get; }
    string Email { get; }
    string Contrasena { get; set; }
}
