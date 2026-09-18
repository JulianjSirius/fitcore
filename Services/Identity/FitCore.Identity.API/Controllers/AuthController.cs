using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Autenticacion.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login/administrador")]
    public async Task<ActionResult<object>> LoginAdministrador([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(
            new LoginAdministradorCommand(request.Email, request.Contrasena));

        return result is null
            ? Unauthorized(new { message = "Credenciales invalidas" })
            : Ok(new { token = result.Token, role = result.Role });
    }

    [HttpPost("login/usuario")]
    public async Task<ActionResult<object>> LoginUsuario([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(
            new LoginUsuarioCommand(request.Email, request.Contrasena));

        return result is null
            ? Unauthorized(new { message = "Credenciales invalidas" })
            : Ok(new { token = result.Token, role = result.Role });
    }

    [HttpPost("login/entrenador")]
    public async Task<ActionResult<object>> LoginEntrenador([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(
            new LoginEntrenadorCommand(request.Email, request.Contrasena));

        return result is null
            ? Unauthorized(new { message = "Credenciales invalidas" })
            : Ok(new { token = result.Token, role = result.Role });
    }
}
