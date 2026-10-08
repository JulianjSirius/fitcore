using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using FitCore.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.API.Desarrollo;

// Dueño fijo para que la colección de Postman (postman/) corra sin preparar nada.
// Solo se activa con SemillaPostman:Habilitada (appsettings.Development.json).
public static class SemillaDesarrollo
{
    public const string DuenoEmail = "postman.dueno@fitcore.com";
    public const string DuenoContrasena = "Postman123!";

    public static async Task AsegurarDuenoPostmanAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Administradores.AnyAsync(a => a.Email == DuenoEmail))
        {
            return;
        }

        context.Administradores.Add(new Administrador
        {
            Id = Guid.NewGuid(),
            Nombre = "Postman",
            LastName = "Dueño",
            Email = DuenoEmail,
            Contrasena = passwordHasher.Hash(DuenoContrasena),
            Telefono = 3000000000,
            NivelAcceso = "Dueño",
            EstaActivo = true
        });
        await context.SaveChangesAsync();
    }
}
