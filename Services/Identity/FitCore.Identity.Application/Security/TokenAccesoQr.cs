using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FitCore.Identity.Application.Security;

// Token corto que se pinta como QR para entrar al gimnasio. Usa una clave y una
// audiencia distintas a las del login, así ninguno de los dos sirve como el otro.
internal static class TokenAccesoQr
{
    public const string Audiencia = "FitCore.Acceso";
    public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(1);

    public static (string Token, DateTime ExpiraEn) Generar(IConfiguration configuration, Guid usuarioId)
    {
        var expiraEn = DateTime.UtcNow.Add(Duracion);
        var token = new JwtSecurityToken(
            issuer: Emisor(configuration),
            audience: Audiencia,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: new SigningCredentials(Clave(configuration), SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    // Devuelve el id del usuario si la firma, la audiencia y la expiración son válidas.
    public static Guid? Validar(IConfiguration configuration, string token)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Emisor(configuration),
                ValidateAudience = true,
                ValidAudience = Audiencia,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = Clave(configuration),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private static string Emisor(IConfiguration configuration)
        => configuration["Jwt:Issuer"] ?? "FitCore.Identity.API";

    private static SymmetricSecurityKey Clave(IConfiguration configuration)
        => new(Encoding.UTF8.GetBytes(configuration["QrAcceso:Key"] ?? string.Empty));
}
