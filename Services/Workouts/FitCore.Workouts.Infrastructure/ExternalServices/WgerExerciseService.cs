using System.Net.Http.Json;
using System.Net;
using System.Text.RegularExpressions;
using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitCore.Workouts.Infrastructure.ExternalServices;

public sealed class WgerExerciseService(
    HttpClient httpClient,
    IWorkoutsDbContext context,
    ILogger<WgerExerciseService> logger) : IWgerExerciseService
{
    public async Task<int> PopulateIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Ejercicios.AnyAsync(cancellationToken))
        {
            return 0;
        }

        WgerExerciseResponseDto? response = null;
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                response = await httpClient.GetFromJsonAsync<WgerExerciseResponseDto>(
                    "api/v2/exerciseinfo/?language=2&limit=200",
                    cancellationToken);
                break;
            }
            catch (HttpRequestException exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(exception, "Wger no respondió en el intento {Attempt}. Reintentando.", attempt);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                logger.LogWarning(exception, "La descarga de Wger agotó el tiempo en el intento {Attempt}. Reintentando.", attempt);
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
        }

        var exercises = response?.Results ?? [];

        var entities = exercises
            .Select(MapToEntity)
            .Where(exercise => exercise is not null)
            .Cast<Ejercicio>()
            .ToList();

        if (entities.Count == 0)
        {
            logger.LogWarning("Wger no devolvió ejercicios en español para poblar la biblioteca.");
            return 0;
        }

        context.Ejercicios.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Se poblaron {ExerciseCount} ejercicios en español desde Wger.", entities.Count);
        return entities.Count;
    }

    private static Ejercicio? MapToEntity(WgerExerciseInfoDto source)
    {
        var translation = source.Translations.FirstOrDefault(item => item.Language == 4)
            ?? source.Translations.FirstOrDefault();
        if (translation is null || string.IsNullOrWhiteSpace(translation.Name))
        {
            return null;
        }

        var muscleGroup = source.Muscles.FirstOrDefault()?.Name
            ?? source.Muscles.FirstOrDefault()?.NameEn
            ?? source.Category?.Name
            ?? "General";
        var description = StripHtml(translation.Description);
        if (string.IsNullOrWhiteSpace(description))
        {
            description = $"Ejercicio para {muscleGroup}.";
        }

        return new Ejercicio
        {
            Id = Guid.NewGuid(),
            Nombre = translation.Name.Trim(),
            GrupoMuscular = muscleGroup.Trim(),
            DescripcionOrientativa = description
        };
    }

    private static string StripHtml(string value)
    {
        var withoutTags = Regex.Replace(value, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(Regex.Replace(withoutTags, @"\s+", " ")).Trim();
    }
}
