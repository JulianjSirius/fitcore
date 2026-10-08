namespace FitCore.Workouts.Application.Abstractions;

public interface IWgerExerciseService
{
    Task<int> PopulateIfEmptyAsync(CancellationToken cancellationToken = default);
}
