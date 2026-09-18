using FitCore.Identity.Domain;
using FitCore.Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Infrastructure.Persistence;
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Entrenador> Entrenadores { get; set; }
    public DbSet<Administrador> Administradores { get; set; }
}                                                                                               