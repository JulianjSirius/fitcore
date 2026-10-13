using FitCore.Identity.Application.Features.Entrenadores.DTOs;
using FitCore.Identity.API.Security;
using FitCore.Identity.Application.Features.Entrenadores.Commands;
using FitCore.Identity.Application.Features.Entrenadores.Queries;
using Mapster;
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

    [Authorize(Roles = "Entrenador,Administrador")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EntrenadorResponse>>> Get()
    {
        var entrenadores = await _mediator.Send(new GetEntrenadoresQuery());
        return Ok(entrenadores.Adapt<List<EntrenadorResponse>>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntrenadorResponse>> GetById(Guid id)
    {
        if (!User.IsInRole("Entrenador") && !User.IsInRole("Administrador") && !User.IsSelf(id))
        {
            return Forbid();
        }

        var entrenador = await _mediator.Send(new GetEntrenadorByIdQuery(id));

        return entrenador is null
            ? NotFound(new { message = "Entrenador no encontrado." })
            : Ok(entrenador.Adapt<EntrenadorResponse>());
    }

    [Authorize(Roles = "Administrador")]
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

        return CreatedAtAction(nameof(GetById), new { id = entrenador.Id }, entrenador.Adapt<EntrenadorResponse>());
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
}
