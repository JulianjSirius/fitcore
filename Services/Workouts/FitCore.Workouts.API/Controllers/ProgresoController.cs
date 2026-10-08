using FitCore.Workouts.API.DTOs;
using FitCore.Workouts.API.Security;
using FitCore.Workouts.Application.Features.Progreso.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Workouts.API.Controllers;

// Mejoras de repeticiones y peso, récords personales e insignias del usuario en sesión.
[Authorize(Roles = "Usuario")]
[ApiController]
[Route("api/[controller]")]
public sealed class ProgresoController : ControllerBase
{
    private readonly ISender mediator;

    public ProgresoController(ISender mediator) => this.mediator = mediator;

    [HttpGet("mio")]
    public async Task<ActionResult<ProgresoResumenResponse>> GetMio(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var usuarioId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetMiProgresoQuery(usuarioId), cancellationToken);
        return Ok(ProgresoResumenResponse.FromResult(result));
    }
}
