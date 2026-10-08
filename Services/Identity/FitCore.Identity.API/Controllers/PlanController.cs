using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Membresias;
using FitCore.Identity.Application.Features.Planes.Commands;
using FitCore.Identity.Application.Features.Planes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Los precios son públicos para mostrarlos antes de inscribirse.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanResult>>> Get()
        => Ok(await _mediator.Send(new GetPlanesQuery()));

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlanResult>> Put(Guid id, [FromBody] PlanUpdateRequest request)
    {
        try
        {
            var plan = await _mediator.Send(new UpdatePlanCommand(
                id, request.Nombre, request.Precio, request.DuracionMeses, request.Activo));

            return plan is null
                ? NotFound(new { message = "Plan no encontrado." })
                : Ok(plan);
        }
        catch (MembresiaException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
