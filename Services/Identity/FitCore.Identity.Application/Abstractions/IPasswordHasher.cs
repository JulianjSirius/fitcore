namespace FitCore.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    // needsRehash = true cuando el valor guardado es texto plano (registros antiguos)
    // o usa parámetros más débiles que los actuales.
    bool Verify(string storedValue, string password, out bool needsRehash);
}
