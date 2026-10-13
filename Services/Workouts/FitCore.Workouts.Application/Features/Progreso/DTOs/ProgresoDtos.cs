using FitCore.Workouts.Application.Features.Progreso.Queries;

namespace FitCore.Workouts.Application.Features.Progreso.DTOs;

// Una mejora de un ejercicio, con lo que subió respecto al valor anterior.
public sealed class ProgresoResponse
{
    public Guid Id { get; init; }
    public Guid RutinaId { get; init; }
    public Guid EjercicioId { get; init; }
    public required string NombreEjercicio { get; init; }
    public int RepeticionesAnteriores { get; init; }
    public int RepeticionesNuevas { get; init; }
    public int DiferenciaRepeticiones { get; init; }
    public decimal PesoAnteriorKg { get; init; }
    public decimal PesoNuevoKg { get; init; }
    public decimal DiferenciaPesoKg { get; init; }
    public bool EsRecordPersonal { get; init; }
    public DateTime Fecha { get; init; }
}

public sealed class ProgresoResumenResponse
{
    public int TotalMejoras { get; init; }
    public int RecordsPersonales { get; init; }
    public decimal KgGanados { get; init; }
    public int RepeticionesGanadas { get; init; }
    public int MejorasEsteMes { get; init; }
    public required IReadOnlyList<ProgresoResponse> Historial { get; init; }
    public required IReadOnlyList<LogroResult> Insignias { get; init; }
    public required IReadOnlyList<LogroResult> Retos { get; init; }
}
