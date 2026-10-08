using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitCore.Workouts.Infrastructure.Persistence;

public sealed class WorkoutsDbContextFactory : IDesignTimeDbContextFactory<WorkoutsDbContext>
{
    public WorkoutsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WORKOUTS_CONNECTION")
            ?? throw new InvalidOperationException(
                "Define la variable de entorno WORKOUTS_CONNECTION antes de ejecutar 'dotnet ef'.");

        var optionsBuilder = new DbContextOptionsBuilder<WorkoutsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WorkoutsDbContext(optionsBuilder.Options);
    }
}
