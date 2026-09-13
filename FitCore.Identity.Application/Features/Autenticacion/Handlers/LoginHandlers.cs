using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Autenticacion.Commands;
using FitCore.Identity.Application.Features.Autenticacion.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FitCore.Identity.Application.Features.Autenticacion.Handlers;

public sealed class LoginAdministradorCommandHandler
    : IRequestHandler<LoginAdministradorCommand, LoginResult?>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public LoginAdministradorCommandHandler(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResult?> Handle(
        LoginAdministradorCommand request,
        CancellationToken cancellationToken)
    {
        if (InvalidCredentials(request.Email, request.Contrasena))
        {
            return null;
        }

        var administrador = await _context.Administradores
            .FirstOrDefaultAsync(
                a => a.Email == request.Email && a.Contrasena == request.Contrasena,
                cancellationToken);

        return administrador is null
            ? null
            : new LoginResult(GenerateToken(administrador.Id, administrador.Email, administrador.NivelAcceso, "Administrador"), "Administrador");
    }

    private string GenerateToken(Guid id, string email, string level, string role)
        => JwtTokenGenerator.Generate(_configuration, id, email, level, role);

    private static bool InvalidCredentials(string email, string password)
        => string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password);
}

public sealed class LoginUsuarioCommandHandler
    : IRequestHandler<LoginUsuarioCommand, LoginResult?>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public LoginUsuarioCommandHandler(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResult?> Handle(
        LoginUsuarioCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return null;
        }

        var usuario = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email == request.Email && u.Contrasena == request.Contrasena,
                cancellationToken);

        return usuario is null
            ? null
            : new LoginResult(
                JwtTokenGenerator.Generate(_configuration, usuario.Id, usuario.Email, usuario.FirstName, "Usuario"),
                "Usuario");
    }
}

public sealed class LoginEntrenadorCommandHandler
    : IRequestHandler<LoginEntrenadorCommand, LoginResult?>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public LoginEntrenadorCommandHandler(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResult?> Handle(
        LoginEntrenadorCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return null;
        }

        var entrenador = await _context.Entrenadores
            .FirstOrDefaultAsync(
                e => e.Email == request.Email && e.Contrasena == request.Contrasena,
                cancellationToken);

        return entrenador is null
            ? null
            : new LoginResult(
                JwtTokenGenerator.Generate(_configuration, entrenador.Id, entrenador.Email, entrenador.Especialidad, "Entrenador"),
                "Entrenador");
    }
}

internal static class JwtTokenGenerator
{
    public static string Generate(
        IConfiguration configuration,
        Guid userId,
        string email,
        string level,
        string roleName)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("NivelAcceso", level),
            new Claim("Rol", roleName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FitCore.Identity.API",
            audience: configuration["Jwt:Audience"] ?? "FitCore.Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
