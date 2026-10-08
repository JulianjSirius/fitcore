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
    public Task<ActionResult<object>> LoginAdministrador([FromBody] LoginRequest request)
        => Login(TipoCuenta.Administrador, request);

    [HttpPost("login/usuario")]
    public Task<ActionResult<object>> LoginUsuario([FromBody] LoginRequest request)
        => Login(TipoCuenta.Usuario, request);

    [HttpPost("login/entrenador")]
    public Task<ActionResult<object>> LoginEntrenador([FromBody] LoginRequest request)
        => Login(TipoCuenta.Entrenador, request);

    private async Task<ActionResult<object>> Login(TipoCuenta tipoCuenta, LoginRequest request)
    {
        var result = await _mediator.Send(
            new LoginCommand(tipoCuenta, request.Email, request.Contrasena));

        return result is null
            ? Unauthorized(new { message = "Credenciales invalidas" })
            : Ok(new { token = result.Token, role = result.Role });
    }
}
