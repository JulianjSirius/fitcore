using FitCore.Workouts.Application.Features.Progreso.DTOs;
using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.DTOs;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using Mapster;

namespace FitCore.Workouts.Tests;

public sealed class MapeoTests
{
    private static readonly TypeAdapterConfig Config = CrearConfig();

    private static TypeAdapterConfig CrearConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(RutinaMapeo).Assembly);
        return config;
    }

    private static RutinaResult Rutina() => new(
        Guid.NewGuid(), "Pierna", "Día de pierna", "Intermedio", Guid.NewGuid(), Guid.NewGuid(), true,
        DateTime.UtcNow, null,
        [new RutinaEjercicioResult(Guid.NewGuid(), "Sentadilla", "Piernas", 4, 10, 60m, 90, 1)]);

    private static ProgresoResult Progreso() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sentadilla", 8, 10, 50m, 62.5m, true, DateTime.UtcNow);

    [Fact]
    public void La_configuracion_compila()
        => Config.Compile();

    [Fact]
    public void Progreso_calcula_las_diferencias()
    {
        var response = Progreso().Adapt<ProgresoResponse>(Config);

        Assert.Equal(2, response.DiferenciaRepeticiones);
        Assert.Equal(12.5m, response.DiferenciaPesoKg);
        Assert.Equal("Sentadilla", response.NombreEjercicio);
    }

    [Fact]
    public void Rutina_sin_edicion_no_trae_progresos()
    {
        var rutina = Rutina();
        var response = rutina.Adapt<RutinaResponse>(Config);

        Assert.Equal(rutina.Id, response.Id);
        Assert.Equal("Sentadilla", Assert.Single(response.RutinaEjercicios).Nombre);
        Assert.Empty(response.Progresos);
    }

    [Fact]
    public void Rutina_actualizada_trae_la_rutina_y_sus_progresos()
    {
        var rutina = Rutina();
        var response = new RutinaActualizadaResult(rutina, [Progreso()]).Adapt<RutinaResponse>(Config);

        Assert.Equal(rutina.Id, response.Id);
        Assert.Equal(rutina.Nombre, response.Nombre);
        Assert.Equal(rutina.CreadorId, response.CreadorId);
        Assert.Single(response.RutinaEjercicios);
        Assert.Equal(2, Assert.Single(response.Progresos).DiferenciaRepeticiones);
    }

    [Fact]
    public void El_peso_de_la_peticion_se_redondea_a_dos_decimales()
    {
        var request = new RutinaEjercicioRequest { EjercicioId = Guid.NewGuid(), Series = 3, Repeticiones = 12, PesoKg = 20.456m, TiempoDescansoSegundos = 60, OrdenAparicion = 1 };

        var input = new List<RutinaEjercicioRequest> { request }.Adapt<List<RutinaEjercicioInput>>(Config);

        Assert.Equal(new RutinaEjercicioInput(request.EjercicioId, 3, 12, 20.46m, 60, 1), Assert.Single(input));
    }
}
