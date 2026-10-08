using FitCore.Identity.Application.Features.Logros.Queries;

namespace FitCore.Identity.Application.Features.Logros;

public static class CalendarioGimnasio
{
    // Colombia no tiene horario de verano: el día del gimnasio es UTC-5 todo el año.
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateOnly Dia(DateTime utc) => DateOnly.FromDateTime(utc + Desfase);

    public static DateOnly Hoy() => Dia(DateTime.UtcNow);
}

// Rachas, insignias y reto mensual a partir de los días con asistencia.
public static class CalculadoraAsistencia
{
    public const int MetaRetoMensual = 12;
    public const int DiasRecientes = 42; // Seis semanas para el calendario del frontend

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public static AsistenciaResumenResult Calcular(IEnumerable<DateOnly> fechas, DateOnly hoy)
    {
        var dias = fechas.Distinct().Order().ToList();

        // Primer día en que la racha llegó a N (para fechar las insignias de racha).
        var alcanzoRacha = new Dictionary<int, DateOnly>();
        var mejorRacha = 0;
        var racha = 0;
        for (var i = 0; i < dias.Count; i++)
        {
            racha = i > 0 && dias[i - 1].AddDays(1) == dias[i] ? racha + 1 : 1;
            mejorRacha = Math.Max(mejorRacha, racha);
            alcanzoRacha.TryAdd(racha, dias[i]);
        }

        // La racha sigue viva si el último día fue hoy o ayer (aún puede venir hoy).
        var ultima = dias.Count > 0 ? dias[^1] : (DateOnly?)null;
        var rachaActual = ultima is { } u && u >= hoy.AddDays(-1) ? racha : 0;

        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var delMes = dias.Where(d => d >= inicioMes && d <= hoy).ToList();

        DateOnly? EnPosicion(IReadOnlyList<DateOnly> lista, int n) => lista.Count >= n ? lista[n - 1] : null;
        LogroResult PorTotal(string codigo, string nombre, string descripcion, int meta)
            => Logro(codigo, nombre, descripcion, meta, dias.Count, EnPosicion(dias, meta));
        LogroResult PorRacha(string codigo, string nombre, string descripcion, int meta)
            => Logro(codigo, nombre, descripcion, meta, mejorRacha, alcanzoRacha.TryGetValue(meta, out var f) ? f : null);

        return new AsistenciaResumenResult(
            rachaActual,
            mejorRacha,
            dias.Count,
            delMes.Count,
            ultima,
            dias.Where(d => d > hoy.AddDays(-DiasRecientes)).ToList(),
            [
                PorTotal("primera-visita", "Primera visita", "Registra tu primera entrada al gimnasio.", 1),
                PorRacha("racha-7", "Semana imparable", "Entrena 7 días seguidos.", 7),
                PorRacha("racha-30", "30 días seguidos", "Mantén una racha de 30 días seguidos.", 30),
                PorTotal("asistencias-50", "Constancia", "Suma 50 asistencias.", 50),
                PorTotal("asistencias-100", "Centenario", "Suma 100 asistencias.", 100)
            ],
            [
                Logro("reto-mes-asistencia", "Reto del mes",
                    $"Asiste {MetaRetoMensual} días en {Meses[hoy.Month - 1]}.",
                    MetaRetoMensual, delMes.Count, EnPosicion(delMes, MetaRetoMensual))
            ]);
    }

    private static LogroResult Logro(string codigo, string nombre, string descripcion, int meta, int valor, DateOnly? fecha)
        => new(codigo, nombre, descripcion, meta, Math.Min(valor, meta), valor >= meta, fecha);
}
