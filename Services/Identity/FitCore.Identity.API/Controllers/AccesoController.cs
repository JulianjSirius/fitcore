using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Acceso.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

// Recepción escanea el QR del usuario y envía aquí el token leído.
[Authorize(Roles = "Administrador,Entrenador")]
[ApiController]
[Route("api/[controller]")]
public class AccesoController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccesoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("validar")]
    public async Task<ActionResult<AccesoResult>> Validar([FromBody] ValidarAccesoRequest request)
        => Ok(await _mediator.Send(new ValidarTokenAccesoQuery(request.Token)));
}
