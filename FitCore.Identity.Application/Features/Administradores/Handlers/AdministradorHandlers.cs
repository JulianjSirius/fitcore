using FitCore.Identity.Application.Features.Administradores.Commands;
using FitCore.Identity.Application.Features.Administradores.Queries;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Administradores.Handlers;

public sealed class GetAdministradoresQueryHandler
    : IRequestHandler<GetAdministradoresQuery, IReadOnlyList<AdministradorResult>>
{
    private readonly IAppDbContext _context;

    public GetAdministradoresQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<AdministradorResult>> Handle(
        GetAdministradoresQuery request,
        CancellationToken cancellationToken)
        => await _context.Administradores
            .Select(a => new AdministradorResult(
                a.Id,
                a.Nombre,
                a.LastName,
                a.Email,
                a.Telefono,
                a.NivelAcceso))
            .ToListAsync(cancellationToken);
}

public sealed class GetAdministradorByIdQueryHandler
    : IRequestHandler<GetAdministradorByIdQuery, AdministradorResult?>
{
    private readonly IAppDbContext _context;

    public GetAdministradorByIdQueryHandler(IAppDbContext context) => _context = context;

    public async Task<AdministradorResult?> Handle(
        GetAdministradorByIdQuery request,
        CancellationToken cancellationToken)
        => await _context.Administradores
            .Where(a => a.Id == request.Id)
            .Select(a => new AdministradorResult(
                a.Id,
                a.Nombre,
                a.LastName,
                a.Email,
                a.Telefono,
                a.NivelAcceso))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateAdministradorCommandHandler
    : IRequestHandler<CreateAdministradorCommand, AdministradorResult>
{
    private readonly IAppDbContext _context;

    public CreateAdministradorCommandHandler(IAppDbContext context) => _context = context;

    public async Task<AdministradorResult> Handle(
        CreateAdministradorCommand request,
        CancellationToken cancellationToken)
    {
        var administrador = new Administrador
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            LastName = request.LastName,
            Email = request.Email,
            Contrasena = request.Contrasena,
            Telefono = request.Telefono,
            NivelAcceso = request.NivelAcceso
        };

        _context.Administradores.Add(administrador);
        await _context.SaveChangesAsync(cancellationToken);

        return new AdministradorResult(
            administrador.Id,
            administrador.Nombre,
            administrador.LastName,
            administrador.Email,
            administrador.Telefono,
            administrador.NivelAcceso);
    }
}

public sealed class UpdateAdministradorCommandHandler
    : IRequestHandler<UpdateAdministradorCommand, bool>
{
    private readonly IAppDbContext _context;

    public UpdateAdministradorCommandHandler(IAppDbContext context) => _context = context;

    public async Task<bool> Handle(
        UpdateAdministradorCommand request,
        CancellationToken cancellationToken)
    {
        var administrador = await _context.Administradores.FindAsync([request.Id], cancellationToken);
        if (administrador is null)
        {
            return false;
        }

        administrador.Nombre = request.Nombre;
        administrador.LastName = request.LastName;
        administrador.Email = request.Email;
        administrador.Contrasena = request.Contrasena;
        administrador.Telefono = request.Telefono;
        administrador.NivelAcceso = request.NivelAcceso;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class DeleteAdministradorCommandHandler
    : IRequestHandler<DeleteAdministradorCommand, bool>
{
    private readonly IAppDbContext _context;

    public DeleteAdministradorCommandHandler(IAppDbContext context) => _context = context;

    public async Task<bool> Handle(
        DeleteAdministradorCommand request,
        CancellationToken cancellationToken)
    {
        var administrador = await _context.Administradores.FindAsync([request.Id], cancellationToken);
        if (administrador is null)
        {
            return false;
        }

        _context.Administradores.Remove(administrador);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
