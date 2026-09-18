using FitCore.Identity.API.DTOs;
using FitCore.Identity.Application.Features.Administradores.Commands;
using FitCore.Identity.Application.Features.Administradores.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AdministradorController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdministradorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdministradorResponse>>> Get()
    {
        var administradores = await _mediator.Send(new GetAdministradoresQuery());
        return Ok(administradores.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdministradorResponse>> GetById(Guid id)
    {
        var administrador = await _mediator.Send(new GetAdministradorByIdQuery(id));

        return administrador is null
            ? NotFound(new { message = "Administrador no encontrado." })
            : Ok(ToResponse(administrador));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPost]
    public async Task<ActionResult<AdministradorResponse>> Post([FromBody] AdministradorRequest request)
    {
        var administrador = await _mediator.Send(new CreateAdministradorCommand(
            request.Nombre,
            request.LastName,
            request.Email,
            request.Contrasena,
            request.Telefono,
            request.NivelAcceso));

        return CreatedAtAction(
            nameof(GetById),
            new { id = administrador.Id },
            ToResponse(administrador));
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] AdministradorRequest request)
    {
        var updated = await _mediator.Send(new UpdateAdministradorCommand(
            id,
            request.Nombre,
            request.LastName,
            request.Email,
            request.Contrasena,
            request.Telefono,
            request.NivelAcceso));

        return updated
            ? NoContent()
            : NotFound(new { message = "Administrador no encontrado." });
    }

    [Authorize(Policy = "SoloAdministrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteAdministradorCommand(id));

        return deleted
            ? NoContent()
            : NotFound(new { message = "Administrador no encontrado." });
    }

    private static AdministradorResponse ToResponse(AdministradorResult administrador)
        => new()
        {
            Id = administrador.Id,
            Nombre = administrador.Nombre,
            LastName = administrador.LastName,
            Email = administrador.Email,
            Telefono = administrador.Telefono,
            NivelAcceso = administrador.NivelAcceso
        };
}
