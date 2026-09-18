using FitCore.Workouts.API.DTOs;
using FitCore.Workouts.API.Security;
using FitCore.Workouts.Application.Features.Rutinas.Commands;
using FitCore.Workouts.Application.Features.Rutinas.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Workouts.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class RutinasController : ControllerBase
{
    private readonly ISender mediator;

    public RutinasController(ISender mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RutinaResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId(out var currentUserId) ? currentUserId : (Guid?)null;
        var role = User.GetRole();
        var requestedUserId = role == "Usuario" ? userId : null;
        var results = await mediator.Send(new GetRutinasQuery(requestedUserId), cancellationToken);
        return Ok(results.Select(RutinaResponse.FromResult));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RutinaResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRutinaByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(RutinaResponse.FromResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<RutinaResponse>> Post([FromBody] RutinaRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var creatorId) || string.IsNullOrWhiteSpace(User.GetRole()))
        {
            return Unauthorized(new { message = "El JWT no contiene un identificador o rol válido." });
        }

        try
        {
            var result = await mediator.Send(new CreateRutinaCommand(
                request.Nombre,
                request.Descripcion,
                request.NivelDificultad,
                request.UsuarioId,
                request.PermitirEdicionEntrenador,
                request.ToInputs(),
                creatorId,
                User.GetRole()!), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, RutinaResponse.FromResult(result));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbid(exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RutinaResponse>> Put(Guid id, [FromBody] RutinaRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorId) || string.IsNullOrWhiteSpace(User.GetRole()))
        {
            return Unauthorized();
        }

        try
        {
            var result = await mediator.Send(new UpdateRutinaCommand(
                id,
                request.Nombre,
                request.Descripcion,
                request.NivelDificultad,
                request.PermitirEdicionEntrenador,
                request.ToInputs(),
                actorId,
                User.GetRole()!), cancellationToken);
            return result is null ? Forbid() : Ok(RutinaResponse.FromResult(result));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorId) || string.IsNullOrWhiteSpace(User.GetRole()))
        {
            return Unauthorized();
        }

        var deleted = await mediator.Send(new DeleteRutinaCommand(id, actorId, User.GetRole()!), cancellationToken);
        return deleted ? NoContent() : Forbid();
    }
}
