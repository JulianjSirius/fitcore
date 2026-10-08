using FitCore.Identity.API.DTOs;
using FitCore.Identity.API.Security;
using FitCore.Identity.Application.Features.CodigosRegistro.Commands;
using FitCore.Identity.Application.Features.CodigosRegistro.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class CodigoRegistroController : ControllerBase
{
    private readonly IMediator _mediator;

    public CodigoRegistroController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CodigoRegistroResult>>> Get()
        => Ok(await _mediator.Send(new GetCodigosRegistroQuery()));

    [HttpPost]
    public async Task<ActionResult<CodigoRegistroResult>> Post([FromBody] CodigoRegistroRequest request)
    {
        if (!User.TryGetUserId(out var administradorId))
        {
            return Unauthorized();
        }

        var codigo = await _mediator.Send(
            new GenerarCodigoRegistroCommand(administradorId, request.DiasVigencia));
        return Ok(codigo);
    }
}
