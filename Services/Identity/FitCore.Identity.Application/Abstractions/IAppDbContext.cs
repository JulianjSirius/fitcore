using FitCore.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FitCore.Identity.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Entrenador> Entrenadores { get; }
    DbSet<Administrador> Administradores { get; }
    DbSet<CodigoRegistro> CodigosRegistro { get; }
    DbSet<Plan> Planes { get; }
    DbSet<Membresia> Membresias { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<Asistencia> Asistencias { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
