using FitCore.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Entrenador> Entrenadores { get; }
    DbSet<Administrador> Administradores { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
