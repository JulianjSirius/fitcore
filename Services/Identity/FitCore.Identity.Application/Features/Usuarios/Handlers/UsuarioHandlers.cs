using FitCore.Identity.Application.Features.Usuarios.Commands;
using FitCore.Identity.Application.Features.Usuarios.Queries;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Usuarios.Handlers;

public sealed class GetUsuariosQueryHandler
    : IRequestHandler<GetUsuariosQuery, IReadOnlyList<UsuarioResult>>
{
    private readonly IAppDbContext _context;

    public GetUsuariosQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IReadOnlyList<UsuarioResult>> Handle(
        GetUsuariosQuery request,
        CancellationToken cancellationToken)
        => await _context.Users
            .Select(u => new UsuarioResult(
                u.Id, u.FirstName, u.LastName, u.Email, u.telefono))
            .ToListAsync(cancellationToken);
}

public sealed class GetUsuarioByIdQueryHandler
    : IRequestHandler<GetUsuarioByIdQuery, UsuarioResult?>
{
    private readonly IAppDbContext _context;

    public GetUsuarioByIdQueryHandler(IAppDbContext context) => _context = context;

    public async Task<UsuarioResult?> Handle(
        GetUsuarioByIdQuery request,
        CancellationToken cancellationToken)
        => await _context.Users
            .Where(u => u.Id == request.Id)
            .Select(u => new UsuarioResult(
                u.Id, u.FirstName, u.LastName, u.Email, u.telefono))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateUsuarioCommandHandler
    : IRequestHandler<CreateUsuarioCommand, UsuarioResult>
{
    private readonly IAppDbContext _context;

    public CreateUsuarioCommandHandler(IAppDbContext context) => _context = context;

    public async Task<UsuarioResult> Handle(
        CreateUsuarioCommand request,
        CancellationToken cancellationToken)
    {
        var usuario = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Contrasena = request.Contrasena,
            telefono = request.Telefono
        };

        _context.Users.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return new UsuarioResult(
            usuario.Id, usuario.FirstName, usuario.LastName, usuario.Email, usuario.telefono);
    }
}

public sealed class UpdateUsuarioCommandHandler
    : IRequestHandler<UpdateUsuarioCommand, bool>
{
    private readonly IAppDbContext _context;

    public UpdateUsuarioCommandHandler(IAppDbContext context) => _context = context;

    public async Task<bool> Handle(
        UpdateUsuarioCommand request,
        CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.FindAsync([request.Id], cancellationToken);
        if (usuario is null)
        {
            return false;
        }

        usuario.FirstName = request.FirstName;
        usuario.LastName = request.LastName;
        usuario.Email = request.Email;
        usuario.Contrasena = request.Contrasena;
        usuario.telefono = request.Telefono;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
