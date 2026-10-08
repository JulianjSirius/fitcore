using FitCore.Workouts.Application.Features.Progreso.Queries;
using FitCore.Workouts.Application.Features.Rutinas.Queries;

namespace FitCore.Workouts.Application.Features.Progreso;

public static class CalendarioGimnasio
{
    // Colombia no tiene horario de verano: el día del gimnasio es UTC-5 todo el año.
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateOnly Dia(DateTime utc) => DateOnly.FromDateTime(utc + Desfase);

    public static DateOnly Hoy() => Dia(DateTime.UtcNow);
}

// Insignias, totales y reto mensual a partir de las mejoras registradas.
public static class CalculadoraProgreso
{
    public const int MetaRetoMensual = 4;
    public const int MaximoHistorial = 30;

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    // Una marca supera a otra si lleva más peso o, con el mismo peso, más repeticiones.
    public static bool SuperaMarca(decimal peso, int repeticiones, decimal pesoPrevio, int repeticionesPrevias)
        => peso > pesoPrevio || peso == pesoPrevio && repeticiones > repeticionesPrevias;

    public static ProgresoResumenResult Calcular(IEnumerable<ProgresoResult> progresos, DateOnly hoy)
    {
        var ordenados = progresos.OrderBy(p => p.Fecha).ToList();
        var records = ordenados.Where(p => p.EsRecordPersonal).ToList();

        // Acumulados en el tiempo para saber cuándo se alcanzó cada meta de kg y repeticiones.
        decimal kg = 0;
        var repeticiones = 0;
        DateOnly? fechaKg = null;
        DateOnly? fechaRepeticiones = null;
        foreach (var progreso in ordenados)
        {
            kg += Math.Max(0, progreso.PesoNuevoKg - progreso.PesoAnteriorKg);
            repeticiones += Math.Max(0, progreso.RepeticionesNuevas - progreso.RepeticionesAnteriores);
            if (kg >= 50) fechaKg ??= CalendarioGimnasio.Dia(progreso.Fecha);
            if (repeticiones >= 100) fechaRepeticiones ??= CalendarioGimnasio.Dia(progreso.Fecha);
        }

        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var delMes = ordenados.Where(p => CalendarioGimnasio.Dia(p.Fecha) >= inicioMes).ToList();

        DateOnly? EnPosicion(IReadOnlyList<ProgresoResult> lista, int n)
            => lista.Count >= n ? CalendarioGimnasio.Dia(lista[n - 1].Fecha) : null;

        return new ProgresoResumenResult(
            ordenados.Count,
            records.Count,
            kg,
            repeticiones,
            delMes.Count,
            ordenados.AsEnumerable().Reverse().Take(MaximoHistorial).ToList(),
            [
                Logro("primera-mejora", "Primera mejora", "Sube las repeticiones o el peso de un ejercicio.", 1, ordenados.Count, EnPosicion(ordenados, 1)),
                Logro("primer-record", "Primer récord personal", "Supera tu mejor marca en un ejercicio.", 1, records.Count, EnPosicion(records, 1)),
                Logro("records-10", "Rompe récords", "Consigue 10 récords personales.", 10, records.Count, EnPosicion(records, 10)),
                Logro("kg-50", "+50 kg", "Suma 50 kg en mejoras de peso.", 50, (int)Math.Floor(kg), fechaKg),
                Logro("repeticiones-100", "+100 repeticiones", "Suma 100 repeticiones en mejoras.", 100, repeticiones, fechaRepeticiones)
            ],
            [
                Logro("reto-mes-progreso", "Reto de progreso",
                    $"Mejora {MetaRetoMensual} veces tus marcas en {Meses[hoy.Month - 1]}.",
                    MetaRetoMensual, delMes.Count, EnPosicion(delMes, MetaRetoMensual))
            ]);
    }

    private static LogroResult Logro(string codigo, string nombre, string descripcion, int meta, int valor, DateOnly? fecha)
        => new(codigo, nombre, descripcion, meta, Math.Min(valor, meta), valor >= meta, fecha);
}
