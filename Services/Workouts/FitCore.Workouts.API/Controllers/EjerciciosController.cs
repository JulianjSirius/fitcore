using FitCore.Workouts.API.DTOs;
using FitCore.Workouts.Application.Features.Ejercicios.Commands;
using FitCore.Workouts.Application.Features.Ejercicios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Workouts.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class EjerciciosController : ControllerBase
{
    private readonly ISender mediator;

    public EjerciciosController(ISender mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EjercicioResponse>>> Get(CancellationToken cancellationToken)
    {
        var results = await mediator.Send(new GetEjerciciosQuery(), cancellationToken);
        return Ok(results.Select(EjercicioResponse.FromResult));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EjercicioResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEjercicioByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(EjercicioResponse.FromResult(result));
    }

    [Authorize(Roles = "Entrenador,Administrador")]
    [HttpPost]
    public async Task<ActionResult<EjercicioResponse>> Post([FromBody] EjercicioRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateEjercicioCommand(
            request.Nombre, request.GrupoMuscular, request.DescripcionOrientativa), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, EjercicioResponse.FromResult(result));
    }

    [Authorize(Roles = "Entrenador,Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EjercicioResponse>> Put(Guid id, [FromBody] EjercicioRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateEjercicioCommand(
            id, request.Nombre, request.GrupoMuscular, request.DescripcionOrientativa), cancellationToken);
        return result is null ? NotFound() : Ok(EjercicioResponse.FromResult(result));
    }

    [Authorize(Roles = "Entrenador,Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteEjercicioCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
