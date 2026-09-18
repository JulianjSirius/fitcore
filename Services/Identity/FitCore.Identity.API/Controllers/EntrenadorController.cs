using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Entrenadores.Commands;
using FitCore.Identity.Application.Features.Entrenadores.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EntrenadorController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntrenadorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EntrenadorResponse>>> Get()
    {
        var entrenadores = await _mediator.Send(new GetEntrenadoresQuery());
        return Ok(entrenadores.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntrenadorResponse>> GetById(Guid id)
    {
        var entrenador = await _mediator.Send(new GetEntrenadorByIdQuery(id));

        return entrenador is null
            ? NotFound(new { message = "Entrenador no encontrado." })
            : Ok(ToResponse(entrenador));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPost]
    public async Task<ActionResult<EntrenadorResponse>> Post([FromBody] EntrenadorRequest request)
    {
        var entrenador = await _mediator.Send(new CreateEntrenadorCommand(
            request.Nombre,
            request.Especialidad,
            request.Horario,
            request.Email,
            request.Contrasena,
            request.Telefono));

        return CreatedAtAction(nameof(GetById), new { id = entrenador.Id }, ToResponse(entrenador));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] EntrenadorRequest request)
    {
        var updated = await _mediator.Send(new UpdateEntrenadorCommand(
            id,
            request.Nombre,
            request.Especialidad,
            request.Horario,
            request.Email,
            request.Contrasena,
            request.Telefono));

        return updated
            ? NoContent()
            : NotFound(new { message = "Entrenador no encontrado." });
    }

    private static EntrenadorResponse ToResponse(EntrenadorResult entrenador)
        => new()
        {
            Id = entrenador.Id,
            Nombre = entrenador.Nombre,
            Especialidad = entrenador.Especialidad,
            Horario = entrenador.Horario,
            Email = entrenador.Email,
            Telefono = entrenador.Telefono
        };
}
