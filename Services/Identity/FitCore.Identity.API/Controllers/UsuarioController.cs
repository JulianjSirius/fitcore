using FitCore.Identity.API.DTOs;
using FitCore.Identity.API.Security;
using FitCore.Identity.Application.Features.Logros.Queries;
using FitCore.Identity.Application.Features.Usuarios;
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

    [Authorize(Roles = "Entrenador,Administrador")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Get()
    {
        var usuarios = await _mediator.Send(new GetUsuariosQuery());
        return Ok(usuarios.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> GetById(Guid id)
    {
        if (!User.IsInRole("Entrenador") && !User.IsInRole("Administrador") && !User.IsSelf(id))
        {
            return Forbid();
        }

        var usuario = await _mediator.Send(new GetUsuarioByIdQuery(id));

        return usuario is null
            ? NotFound(new { message = "Usuario no encontrado." })
            : Ok(ToResponse(usuario));
    }

    // Token de 1 minuto con el id del usuario para pintar el QR de entrada.
    [Authorize(Roles = "Usuario")]
    [HttpGet("me/qr-token")]
    public async Task<ActionResult<TokenAccesoQrResult>> GetQrToken()
    {
        if (!User.TryGetUserId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _mediator.Send(new GenerarTokenAccesoQrQuery(usuarioId)));
        }
        catch (MembresiaInactivaException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    // Rachas, insignias y reto mensual de asistencia del usuario en sesión.
    [Authorize(Roles = "Usuario")]
    [HttpGet("me/asistencia")]
    public async Task<ActionResult<AsistenciaResumenResult>> GetMiAsistencia()
    {
        if (!User.TryGetUserId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await _mediator.Send(new GetMiAsistenciaQuery(usuarioId)));
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Post([FromBody] CrearUsuarioRequest request)
    {
        try
        {
            var usuario = await _mediator.Send(new CreateUsuarioCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Contrasena,
                request.telefono,
                request.CodigoRegistro));

            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, ToResponse(usuario));
        }
        catch (CodigoRegistroInvalidoException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
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
