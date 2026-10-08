// Punto de entrada único para el frontend: enruta cada /api/... al microservicio que
// corresponde. CORS y la autenticación JWT los sigue resolviendo cada servicio; el
// gateway reenvía las peticiones (incluidas las OPTIONS de preflight) sin modificarlas.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapReverseProxy();

app.Run();

// Visible para WebApplicationFactory en los proyectos de pruebas.
public partial class Program;
