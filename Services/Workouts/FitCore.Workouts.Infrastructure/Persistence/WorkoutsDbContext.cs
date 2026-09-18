using FitCore.Workouts.Domain.Entidades;
using FitCore.Workouts.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Infrastructure.Persistence;

public class WorkoutsDbContext : DbContext, IWorkoutsDbContext
{
    public WorkoutsDbContext(DbContextOptions<WorkoutsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rutina> Rutinas => Set<Rutina>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
    public DbSet<RutinaEjercicio> RutinaEjercicios => Set<RutinaEjercicio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rutina>(entity =>
        {
            entity.ToTable("Rutinas");
            entity.HasKey(rutina => rutina.Id);

            entity.Property(rutina => rutina.Nombre).IsRequired();
            entity.Property(rutina => rutina.Descripcion).IsRequired();
            entity.Property(rutina => rutina.NivelDificultad).IsRequired();
            entity.Property(rutina => rutina.FechaCreacion).IsRequired();
        });

        modelBuilder.Entity<Ejercicio>(entity =>
        {
            entity.ToTable("Ejercicios");
            entity.HasKey(ejercicio => ejercicio.Id);

            entity.Property(ejercicio => ejercicio.Nombre).IsRequired();
            entity.Property(ejercicio => ejercicio.GrupoMuscular).IsRequired();
            entity.Property(ejercicio => ejercicio.DescripcionOrientativa).IsRequired();
        });

        modelBuilder.Entity<RutinaEjercicio>(entity =>
        {
            entity.ToTable("RutinaEjercicios");
            entity.HasKey(rutinaEjercicio => new
            {
                rutinaEjercicio.RutinaId,
                rutinaEjercicio.EjercicioId
            });

            entity.HasOne(rutinaEjercicio => rutinaEjercicio.Rutina)
                .WithMany(rutina => rutina.RutinaEjercicios)
                .HasForeignKey(rutinaEjercicio => rutinaEjercicio.RutinaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rutinaEjercicio => rutinaEjercicio.Ejercicio)
                .WithMany(ejercicio => ejercicio.RutinaEjercicios)
                .HasForeignKey(rutinaEjercicio => rutinaEjercicio.EjercicioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(rutinaEjercicio => rutinaEjercicio.Series).IsRequired();
            entity.Property(rutinaEjercicio => rutinaEjercicio.Repeticiones).IsRequired();
            entity.Property(rutinaEjercicio => rutinaEjercicio.TiempoDescansoSegundos).IsRequired();
            entity.Property(rutinaEjercicio => rutinaEjercicio.OrdenAparicion).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
