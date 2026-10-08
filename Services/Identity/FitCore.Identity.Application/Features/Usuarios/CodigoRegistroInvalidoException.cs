namespace FitCore.Identity.Application.Features.Usuarios;

public sealed class CodigoRegistroInvalidoException()
    : Exception("El código de registro no es válido, ya fue usado o expiró. Pídele uno nuevo a un administrador.");
