using FitCore.Identity.API.DTOs;
using FitCore.Identity.API.Security;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Application.Features.Membresias.Commands;
using FitCore.Identity.Application.Features.Membresias.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MembresiaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembresiaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "Usuario")]
    [HttpGet("mia")]
    public async Task<ActionResult<MembresiaResult>> GetMia()
    {
        if (!User.TryGetUserId(out var usuarioId))
        {
            return Unauthorized();
        }

        var membresia = await _mediator.Send(new GetMembresiaActualQuery(usuarioId));
        return membresia is null
            ? NotFound(new { message = "Todavía no tienes una membresía." })
            : Ok(membresia);
    }

    [HttpGet("usuario/{usuarioId:guid}")]
    public async Task<ActionResult<MembresiaResult>> GetDeUsuario(Guid usuarioId)
    {
        if (!User.IsInRole("Entrenador") && !User.IsInRole("Administrador") && !User.IsSelf(usuarioId))
        {
            return Forbid();
        }

        var membresia = await _mediator.Send(new GetMembresiaActualQuery(usuarioId));
        return membresia is null
            ? NotFound(new { message = "El usuario no tiene membresías." })
            : Ok(membresia);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("pagos")]
    public async Task<ActionResult<IEnumerable<PagoResult>>> GetPagos([FromQuery] Guid? usuarioId)
        => Ok(await _mediator.Send(new GetPagosQuery(usuarioId)));

    [Authorize(Roles = "Administrador")]
    [HttpPost("pagos")]
    public async Task<ActionResult<PagoRegistradoResult>> RegistrarPago([FromBody] RegistrarPagoRequest request)
    {
        if (!User.TryGetUserId(out var administradorId))
        {
            return Unauthorized();
        }

        try
        {
            var resultado = await _mediator.Send(new RegistrarPagoManualCommand(
                request.UsuarioId,
                request.PlanId,
                request.Metodo,
                request.Referencia,
                request.AcompananteEmail,
                administradorId));
            return Ok(resultado);
        }
        catch (MembresiaException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
