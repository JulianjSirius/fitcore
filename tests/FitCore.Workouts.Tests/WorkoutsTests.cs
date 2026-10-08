using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Tests;

[Collection(WorkoutsCollection.Nombre)]
public sealed class BaseDeDatosTests(WorkoutsApiFactory factory)
{
    [Fact]
    public async Task Migraciones_aplicadas_y_modelo_al_dia()
    {
        Assert.Empty(await factory.EnBaseDeDatosAsync(db => db.Database.GetPendingMigrationsAsync()));
        Assert.False(await factory.EnBaseDeDatosAsync(db => Task.FromResult(db.Database.HasPendingModelChanges())));
    }

    [Fact]
    public async Task Health_responde_200()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Al_arrancar_con_la_base_vacia_se_carga_la_biblioteca()
        => Assert.True(await factory.EnBaseDeDatosAsync(db => db.Ejercicios.AnyAsync()));
}

[Collection(WorkoutsCollection.Nombre)]
public sealed class EjerciciosTests(WorkoutsApiFactory factory)
{
    private static object Nuevo(string nombre = "Remo") =>
        new { nombre, grupoMuscular = "Espalda", descripcionOrientativa = "Tirar hacia el abdomen" };

    private async Task<Guid> CrearAsync()
    {
        var response = await factory.Como("Entrenador").PostAsJsonAsync("/api/Ejercicios", Nuevo($"Remo {Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GET_sin_sesion_401_con_sesion_200()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/Ejercicios")).StatusCode);
        var lista = await factory.Como("Usuario").GetFromJsonAsync<JsonElement>("/api/Ejercicios");
        Assert.True(lista.GetArrayLength() >= 2);
    }

    [Theory]
    [InlineData("Entrenador", HttpStatusCode.Created)]
    [InlineData("Administrador", HttpStatusCode.Created)]
    [InlineData("Usuario", HttpStatusCode.Forbidden)]
    public async Task POST_segun_rol(string rol, HttpStatusCode esperado)
        => Assert.Equal(esperado, (await factory.Como(rol).PostAsJsonAsync("/api/Ejercicios", Nuevo())).StatusCode);

    [Fact]
    public async Task GET_por_id_200_e_inexistente_404()
    {
        var id = await CrearAsync();
        Assert.Equal(HttpStatusCode.OK, (await factory.Como("Usuario").GetAsync($"/api/Ejercicios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Como("Usuario").GetAsync($"/api/Ejercicios/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task PUT_como_entrenador_actualiza_y_como_usuario_403()
    {
        var id = await CrearAsync();
        var ok = await factory.Como("Entrenador").PutAsJsonAsync($"/api/Ejercicios/{id}", Nuevo("Remo con barra"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Remo con barra", (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nombre").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Usuario").PutAsJsonAsync($"/api/Ejercicios/{id}", Nuevo())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Como("Entrenador").PutAsJsonAsync($"/api/Ejercicios/{Guid.NewGuid()}", Nuevo())).StatusCode);
    }

    [Fact]
    public async Task DELETE_como_administrador_204_luego_404_y_como_usuario_403()
    {
        var id = await CrearAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Usuario").DeleteAsync($"/api/Ejercicios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await factory.Como("Administrador").DeleteAsync($"/api/Ejercicios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Como("Administrador").DeleteAsync($"/api/Ejercicios/{id}")).StatusCode);
    }
}

[Collection(WorkoutsCollection.Nombre)]
public sealed class RutinasTests(WorkoutsApiFactory factory)
{
    private async Task<Guid[]> EjerciciosAsync(int cantidad)
    {
        var lista = await factory.Como("Usuario").GetFromJsonAsync<JsonElement>("/api/Ejercicios");
        return lista.EnumerateArray().Take(cantidad).Select(e => e.GetProperty("id").GetGuid()).ToArray();
    }

    private async Task<object> RutinaAsync(Guid usuarioId, bool permitirEntrenador = false, Guid[]? ejercicios = null)
    {
        ejercicios ??= await EjerciciosAsync(2);
        return new
        {
            nombre = "Piernas y pecho",
            descripcion = "Rutina de prueba",
            nivelDificultad = "Media",
            usuarioId,
            permitirEdicionEntrenador = permitirEntrenador,
            rutinaEjercicios = ejercicios.Select((id, i) => new
            {
                ejercicioId = id, series = 3, repeticiones = 10, tiempoDescansoSegundos = 60, ordenAparicion = i + 1
            })
        };
    }

    private async Task<Guid> CrearComoUsuarioAsync(Guid usuarioId, bool permitirEntrenador = false)
    {
        var response = await factory.Como("Usuario", usuarioId).PostAsJsonAsync("/api/Rutinas", await RutinaAsync(usuarioId, permitirEntrenador));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task POST_usuario_crea_su_rutina_con_ejercicios_en_orden()
    {
        var usuarioId = Guid.NewGuid();
        var response = await factory.Como("Usuario", usuarioId).PostAsJsonAsync("/api/Rutinas", await RutinaAsync(usuarioId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var rutina = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(usuarioId, rutina.GetProperty("creadorId").GetGuid());
        var orden = rutina.GetProperty("rutinaEjercicios").EnumerateArray().Select(e => e.GetProperty("ordenAparicion").GetInt32());
        Assert.Equal([1, 2], orden);
    }

    [Fact]
    public async Task POST_usuario_para_otra_persona_responde_403()
    {
        var response = await factory.Como("Usuario").PostAsJsonAsync("/api/Rutinas", await RutinaAsync(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_administrador_no_crea_rutinas_403()
    {
        var response = await factory.Como("Administrador").PostAsJsonAsync("/api/Rutinas", await RutinaAsync(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_entrenador_crea_para_un_usuario_201()
    {
        var response = await factory.Como("Entrenador").PostAsJsonAsync("/api/Rutinas", await RutinaAsync(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task POST_con_ejercicio_inexistente_repetido_o_vacio_responde_400()
    {
        var usuarioId = Guid.NewGuid();
        var cliente = factory.Como("Usuario", usuarioId);
        var uno = (await EjerciciosAsync(1))[0];

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Rutinas", await RutinaAsync(usuarioId, ejercicios: [Guid.NewGuid()]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Rutinas", await RutinaAsync(usuarioId, ejercicios: [uno, uno]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Rutinas", await RutinaAsync(usuarioId, ejercicios: []))).StatusCode);
    }

    [Fact]
    public async Task GET_usuario_ve_solo_las_suyas_entrenador_ve_todas()
    {
        var ana = Guid.NewGuid();
        var beto = Guid.NewGuid();
        var deAna = await CrearComoUsuarioAsync(ana);
        var deBeto = await CrearComoUsuarioAsync(beto);

        var deAnaLista = await factory.Como("Usuario", ana).GetFromJsonAsync<JsonElement>("/api/Rutinas");
        var ids = deAnaLista.EnumerateArray().Select(r => r.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(deAna, ids);
        Assert.DoesNotContain(deBeto, ids);

        var todas = await factory.Como("Entrenador").GetFromJsonAsync<JsonElement>("/api/Rutinas");
        var idsTodas = todas.EnumerateArray().Select(r => r.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(deAna, idsTodas);
        Assert.Contains(deBeto, idsTodas);
    }

    [Fact]
    public async Task GET_por_id_200_e_inexistente_404()
    {
        var id = await CrearComoUsuarioAsync(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await factory.Como("Usuario").GetAsync($"/api/Rutinas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Como("Usuario").GetAsync($"/api/Rutinas/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task PUT_duenio_200_otro_usuario_403_entrenador_segun_permiso()
    {
        var duenio = Guid.NewGuid();
        var sinPermiso = await CrearComoUsuarioAsync(duenio, permitirEntrenador: false);
        var conPermiso = await CrearComoUsuarioAsync(duenio, permitirEntrenador: true);
        var cambios = await RutinaAsync(duenio, ejercicios: await EjerciciosAsync(1));

        Assert.Equal(HttpStatusCode.OK, (await factory.Como("Usuario", duenio).PutAsJsonAsync($"/api/Rutinas/{sinPermiso}", cambios)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Usuario").PutAsJsonAsync($"/api/Rutinas/{sinPermiso}", cambios)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Entrenador").PutAsJsonAsync($"/api/Rutinas/{sinPermiso}", cambios)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.Como("Entrenador").PutAsJsonAsync($"/api/Rutinas/{conPermiso}", cambios)).StatusCode);
    }

    [Fact]
    public async Task DELETE_duenio_204_otro_usuario_403()
    {
        var duenio = Guid.NewGuid();
        var id = await CrearComoUsuarioAsync(duenio);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.Como("Usuario").DeleteAsync($"/api/Rutinas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await factory.Como("Usuario", duenio).DeleteAsync($"/api/Rutinas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Como("Usuario", duenio).GetAsync($"/api/Rutinas/{id}")).StatusCode);
    }
}
