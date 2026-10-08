using System.Text.Json.Serialization;

namespace FitCore.Workouts.Infrastructure.ExternalServices;

public sealed class WgerExerciseResponseDto
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("results")]
    public List<WgerExerciseInfoDto> Results { get; set; } = [];
}

public sealed class WgerExerciseInfoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("category")]
    public WgerNamedDto? Category { get; set; }

    [JsonPropertyName("muscles")]
    public List<WgerMuscleDto> Muscles { get; set; } = [];

    [JsonPropertyName("muscles_secondary")]
    public List<WgerMuscleDto> MusclesSecondary { get; set; } = [];

    [JsonPropertyName("equipment")]
    public List<WgerNamedDto> Equipment { get; set; } = [];

    [JsonPropertyName("translations")]
    public List<WgerTranslationDto> Translations { get; set; } = [];
}

public class WgerNamedDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class WgerMuscleDto : WgerNamedDto
{
    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;
}

public sealed class WgerTranslationDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public int Language { get; set; }
}
