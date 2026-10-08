namespace FitCore.Identity.Application.Features.Usuarios;

public sealed class MembresiaInactivaException()
    : Exception("No tienes una membresía activa. Acércate a recepción para renovarla.");
