using FitCore.Identity.Domain;
using FitCore.Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Infrastructure.Persistence;
public class AppDbContext : DbContext, IAppDbContext
{
    // GUIDs fijos de los planes sembrados por la migración AgregarMembresiasYPlanes.
    public static readonly Guid PlanMensualId = Guid.Parse("6f1d2c3a-0001-4a7e-9c11-000000000001");
    public static readonly Guid PlanDuoId = Guid.Parse("6f1d2c3a-0002-4a7e-9c11-000000000002");
    public static readonly Guid PlanAnualId = Guid.Parse("6f1d2c3a-0003-4a7e-9c11-000000000003");

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Entrenador> Entrenadores { get; set; }
    public DbSet<Administrador> Administradores { get; set; }
    public DbSet<CodigoRegistro> CodigosRegistro { get; set; }
    public DbSet<Plan> Planes { get; set; }
    public DbSet<Membresia> Membresias { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Asistencia> Asistencias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodigoRegistro>(entity =>
        {
            entity.HasIndex(c => c.Codigo).IsUnique();
            entity.Property(c => c.Codigo).HasMaxLength(32);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Planes");
            entity.HasIndex(p => p.Codigo).IsUnique();
            entity.Property(p => p.Codigo).HasMaxLength(20);
            entity.Property(p => p.Nombre).HasMaxLength(80);
            entity.Property(p => p.Precio).HasPrecision(12, 2);

            var fechaSemilla = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                new Plan { Id = PlanMensualId, Codigo = CodigosPlan.Mensual, Nombre = "Mensual", Precio = 120_000m, DuracionMeses = 1, MaxBeneficiarios = 1, Activo = true, FechaActualizacion = fechaSemilla },
                new Plan { Id = PlanDuoId, Codigo = CodigosPlan.Duo, Nombre = "Duo", Precio = 200_000m, DuracionMeses = 1, MaxBeneficiarios = 2, Activo = true, FechaActualizacion = fechaSemilla },
                new Plan { Id = PlanAnualId, Codigo = CodigosPlan.Anual, Nombre = "Anual", Precio = 1_200_000m, DuracionMeses = 12, MaxBeneficiarios = 1, Activo = true, FechaActualizacion = fechaSemilla });
        });

        modelBuilder.Entity<Membresia>(entity =>
        {
            entity.Property(m => m.Estado).HasMaxLength(20);
            entity.HasIndex(m => m.UsuarioTitularId);
            entity.HasIndex(m => m.AcompananteUsuarioId);
            entity.HasIndex(m => new { m.Estado, m.FechaVencimiento });
            entity.HasOne(m => m.Plan).WithMany().HasForeignKey(m => m.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasIndex(p => p.Referencia).IsUnique();
            entity.HasIndex(p => p.UsuarioId);
            entity.Property(p => p.Referencia).HasMaxLength(64);
            entity.Property(p => p.Metodo).HasMaxLength(20);
            entity.Property(p => p.Estado).HasMaxLength(20);
            entity.Property(p => p.Moneda).HasMaxLength(3);
            entity.Property(p => p.Monto).HasPrecision(12, 2);
            entity.HasOne<Membresia>().WithMany().HasForeignKey(p => p.MembresiaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Plan>().WithMany().HasForeignKey(p => p.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.ToTable("Asistencias");
            // Una sola asistencia por usuario y día, aunque escaneen el QR varias veces.
            entity.HasIndex(a => new { a.UsuarioId, a.Fecha }).IsUnique();
        });
    }
}
