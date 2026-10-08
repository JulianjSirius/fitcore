using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitCore.Workouts.Application.Features.Progreso;
using FitCore.Workouts.Application.Features.Rutinas.Queries;

namespace FitCore.Workouts.Tests;

// Peso por ejercicio, mejoras al editar una rutina y GET /api/Progreso/mio.
[Collection(WorkoutsCollection.Nombre)]
public sealed class ProgresoTests(WorkoutsApiFactory factory)
{
    private async Task<Guid> EjercicioAsync()
    {
        var lista = await factory.Como("Usuario").GetFromJsonAsync<JsonElement>("/api/Ejercicios");
        return lista.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static object Rutina(Guid usuarioId, Guid ejercicioId, int repeticiones, decimal pesoKg, bool permitirEntrenador = false) => new
    {
        nombre = "Fuerza",
        descripcion = "Rutina de progreso",
        nivelDificultad = "Intermedio",
        usuarioId,
        permitirEdicionEntrenador = permitirEntrenador,
        rutinaEjercicios = new[]
        {
            new { ejercicioId, series = 4, repeticiones, pesoKg, tiempoDescansoSegundos = 90, ordenAparicion = 1 }
        }
    };

    private async Task<Guid> CrearAsync(Guid usuarioId, Guid ejercicioId, int repeticiones, decimal pesoKg, bool permitirEntrenador = false)
    {
        var response = await factory.Como("Usuario", usuarioId)
            .PostAsJsonAsync("/api/Rutinas", Rutina(usuarioId, ejercicioId, repeticiones, pesoKg, permitirEntrenador));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rutina = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(pesoKg, rutina.GetProperty("rutinaEjercicios")[0].GetProperty("pesoKg").GetDecimal());
        Assert.Empty(rutina.GetProperty("progresos").EnumerateArray());
        return rutina.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> EditarAsync(Guid usuarioId, Guid rutinaId, Guid ejercicioId, int repeticiones, decimal pesoKg)
    {
        var response = await factory.Como("Usuario", usuarioId)
            .PutAsJsonAsync($"/api/Rutinas/{rutinaId}", Rutina(usuarioId, ejercicioId, repeticiones, pesoKg));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("progresos");
    }

    [Fact]
    public async Task Subir_repeticiones_y_peso_devuelve_la_mejora_con_la_diferencia()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var rutinaId = await CrearAsync(usuarioId, ejercicioId, 10, 40m);

        var progreso = Assert.Single((await EditarAsync(usuarioId, rutinaId, ejercicioId, 12, 45.5m)).EnumerateArray());
        Assert.Equal(10, progreso.GetProperty("repeticionesAnteriores").GetInt32());
        Assert.Equal(12, progreso.GetProperty("repeticionesNuevas").GetInt32());
        Assert.Equal(2, progreso.GetProperty("diferenciaRepeticiones").GetInt32());
        Assert.Equal(40m, progreso.GetProperty("pesoAnteriorKg").GetDecimal());
        Assert.Equal(45.5m, progreso.GetProperty("pesoNuevoKg").GetDecimal());
        Assert.Equal(5.5m, progreso.GetProperty("diferenciaPesoKg").GetDecimal());
        Assert.True(progreso.GetProperty("esRecordPersonal").GetBoolean());
    }

    [Fact]
    public async Task Bajar_o_dejar_igual_no_registra_mejora()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var rutinaId = await CrearAsync(usuarioId, ejercicioId, 10, 40m);

        Assert.Empty((await EditarAsync(usuarioId, rutinaId, ejercicioId, 10, 40m)).EnumerateArray());
        Assert.Empty((await EditarAsync(usuarioId, rutinaId, ejercicioId, 8, 35m)).EnumerateArray());
    }

    [Fact]
    public async Task Volver_a_una_marca_ya_alcanzada_es_mejora_pero_no_record()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var rutinaId = await CrearAsync(usuarioId, ejercicioId, 10, 40m);

        await EditarAsync(usuarioId, rutinaId, ejercicioId, 10, 50m); // récord
        await EditarAsync(usuarioId, rutinaId, ejercicioId, 10, 30m); // baja
        var progreso = Assert.Single((await EditarAsync(usuarioId, rutinaId, ejercicioId, 10, 50m)).EnumerateArray());
        Assert.False(progreso.GetProperty("esRecordPersonal").GetBoolean());
    }

    [Fact]
    public async Task La_mejora_hecha_por_el_entrenador_cuenta_para_el_duenio_de_la_rutina()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var rutinaId = await CrearAsync(usuarioId, ejercicioId, 10, 20m, permitirEntrenador: true);

        var response = await factory.Como("Entrenador")
            .PutAsJsonAsync($"/api/Rutinas/{rutinaId}", Rutina(usuarioId, ejercicioId, 10, 25m, permitirEntrenador: true));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var mio = await factory.Como("Usuario", usuarioId).GetFromJsonAsync<JsonElement>("/api/Progreso/mio");
        Assert.Equal(1, mio.GetProperty("totalMejoras").GetInt32());
    }

    [Fact]
    public async Task Peso_negativo_o_excesivo_responde_400()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var cliente = factory.Como("Usuario", usuarioId);

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Rutinas", Rutina(usuarioId, ejercicioId, 10, -1m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Rutinas", Rutina(usuarioId, ejercicioId, 10, 1000.5m))).StatusCode);
    }

    [Fact]
    public async Task Mi_progreso_resume_mejoras_records_e_insignias()
    {
        var usuarioId = Guid.NewGuid();
        var ejercicioId = await EjercicioAsync();
        var rutinaId = await CrearAsync(usuarioId, ejercicioId, 10, 40m);
        await EditarAsync(usuarioId, rutinaId, ejercicioId, 12, 40m);
        await EditarAsync(usuarioId, rutinaId, ejercicioId, 12, 50m);

        var mio = await factory.Como("Usuario", usuarioId).GetFromJsonAsync<JsonElement>("/api/Progreso/mio");
        Assert.Equal(2, mio.GetProperty("totalMejoras").GetInt32());
        Assert.Equal(2, mio.GetProperty("recordsPersonales").GetInt32());
        Assert.Equal(10m, mio.GetProperty("kgGanados").GetDecimal());
        Assert.Equal(2, mio.GetProperty("repeticionesGanadas").GetInt32());
        Assert.Equal(2, mio.GetProperty("historial").GetArrayLength());
        // El historial va del más reciente al más antiguo.
        Assert.Equal(50m, mio.GetProperty("historial")[0].GetProperty("pesoNuevoKg").GetDecimal());

        var insignias = mio.GetProperty("insignias").EnumerateArray().ToDictionary(i => i.GetProperty("codigo").GetString()!);
        Assert.True(insignias["primer-record"].GetProperty("obtenido").GetBoolean());
        Assert.False(insignias["records-10"].GetProperty("obtenido").GetBoolean());
        Assert.Equal(2, insignias["records-10"].GetProperty("progreso").GetInt32());
    }

    [Fact]
    public async Task Mi_progreso_solo_para_el_rol_Usuario()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Entrenador").GetAsync("/api/Progreso/mio")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/Progreso/mio")).StatusCode);
    }
}

public sealed class CalculadoraProgresoTests
{
    private static ProgresoResult Mejora(DateTime fecha, int repsAntes, int repsDespues, decimal pesoAntes, decimal pesoDespues, bool record = false)
        => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sentadilla", repsAntes, repsDespues, pesoAntes, pesoDespues, record, fecha);

    [Theory]
    [InlineData(50, 8, 45, 12, true)]   // más peso gana aunque haya menos repeticiones
    [InlineData(45, 12, 45, 10, true)]  // mismo peso, más repeticiones
    [InlineData(45, 10, 45, 10, false)] // igual no supera
    [InlineData(40, 20, 45, 1, false)]  // menos peso no supera
    public void Supera_marca(decimal peso, int reps, decimal pesoPrevio, int repsPrevias, bool esperado)
        => Assert.Equal(esperado, CalculadoraProgreso.SuperaMarca(peso, reps, pesoPrevio, repsPrevias));

    [Fact]
    public void Insignia_de_50_kg_se_fecha_cuando_el_acumulado_llega_a_50()
    {
        var hoy = new DateOnly(2026, 10, 20);
        var r = CalculadoraProgreso.Calcular(
        [
            Mejora(new DateTime(2026, 9, 1, 15, 0, 0, DateTimeKind.Utc), 10, 10, 0, 30),
            Mejora(new DateTime(2026, 10, 5, 15, 0, 0, DateTimeKind.Utc), 10, 10, 30, 55),
        ], hoy);

        var kg = r.Insignias.Single(i => i.Codigo == "kg-50");
        Assert.True(kg.Obtenido);
        Assert.Equal(new DateOnly(2026, 10, 5), kg.FechaObtencion);
        Assert.Equal(1, r.MejorasEsteMes);
        Assert.Equal(1, Assert.Single(r.Retos).Progreso);
    }
}
