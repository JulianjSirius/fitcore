using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Usuarios.Commands;
using FitCore.Identity.Application.Features.Usuarios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuarioController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Get()
    {
        var usuarios = await _mediator.Send(new GetUsuariosQuery());
        return Ok(usuarios.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> GetById(Guid id)
    {
        var usuario = await _mediator.Send(new GetUsuarioByIdQuery(id));

        return usuario is null
            ? NotFound(new { message = "Usuario no encontrado." })
            : Ok(ToResponse(usuario));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Post([FromBody] UsuarioRequest request)
    {
        var usuario = await _mediator.Send(new CreateUsuarioCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Contrasena,
            request.telefono));

        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, ToResponse(usuario));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UsuarioRequest request)
    {
        var updated = await _mediator.Send(new UpdateUsuarioCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Contrasena,
            request.telefono));

        return updated
            ? NoContent()
            : NotFound(new { message = "Usuario no encontrado." });
    }

    private static UsuarioResponse ToResponse(UsuarioResult usuario)
        => new()
        {
            Id = usuario.Id,
            FirstName = usuario.FirstName,
            LastName = usuario.LastName,
            Email = usuario.Email,
            telefono = usuario.Telefono
        };
}
