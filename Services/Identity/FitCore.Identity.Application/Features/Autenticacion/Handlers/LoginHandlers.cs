using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Application.Features.Autenticacion.Commands;
using FitCore.Identity.Application.Features.Autenticacion.Queries;
using FitCore.Identity.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FitCore.Identity.Application.Features.Autenticacion.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IAppDbContext context,
        IConfiguration configuration,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return null;
        }

        // Solo los administradores llevan NivelAcceso en el token.
        return request.TipoCuenta switch
        {
            TipoCuenta.Administrador => await LoginAsync(
                _context.Administradores, request, a => a.NivelAcceso, cancellationToken),
            TipoCuenta.Usuario => await LoginAsync(
                _context.Users, request, _ => null, cancellationToken),
            TipoCuenta.Entrenador => await LoginAsync(
                _context.Entrenadores, request, _ => null, cancellationToken),
            _ => null
        };
    }

    private async Task<LoginResult?> LoginAsync<TCuenta>(
        DbSet<TCuenta> cuentas,
        LoginCommand request,
        Func<TCuenta, string?> nivelAcceso,
        CancellationToken cancellationToken)
        where TCuenta : class, ICuenta
    {
        var cuenta = await cuentas.FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);
        if (cuenta is null || !_passwordHasher.Verify(cuenta.Contrasena, request.Contrasena, out var needsRehash))
        {
            return null;
        }

        // Migra contraseñas antiguas en texto plano (o con parámetros débiles) al hash actual.
        if (needsRehash)
        {
            cuenta.Contrasena = _passwordHasher.Hash(request.Contrasena);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var rol = request.TipoCuenta.ToString();
        return new LoginResult(
            JwtTokenGenerator.Generate(_configuration, cuenta.Id, cuenta.Email, nivelAcceso(cuenta), rol),
            rol);
    }
}

internal static class JwtTokenGenerator
{
    public static string Generate(
        IConfiguration configuration,
        Guid userId,
        string email,
        string? nivelAcceso,
        string roleName)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("Rol", roleName)
        };

        // Antes se enviaba el nombre del usuario o la especialidad del entrenador
        // como NivelAcceso, lo que permitía falsear "Dueño".
        if (nivelAcceso is not null)
        {
            claims.Add(new Claim("NivelAcceso", nivelAcceso));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FitCore.Identity.API",
            audience: configuration["Jwt:Audience"] ?? "FitCore.Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
