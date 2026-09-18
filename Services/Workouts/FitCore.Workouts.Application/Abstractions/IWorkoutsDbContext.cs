using FitCore.Workouts.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Abstractions;

public interface IWorkoutsDbContext
{
    DbSet<Rutina> Rutinas { get; }
    DbSet<Ejercicio> Ejercicios { get; }
    DbSet<RutinaEjercicio> RutinaEjercicios { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
