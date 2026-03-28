using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fieldore.Infrastructure.Auth;

public sealed class TokenService(IOptions<JwtSettings> jwtOptions) : ITokenService
{
    public AuthResponse CreateToken(AuthUser authUser, AppUserProfile profile, Guid? businessId = null)
    {
        var settings = jwtOptions.Value;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, authUser.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, authUser.Email),
            new(ClaimTypes.NameIdentifier, authUser.Id.ToString()),
            new(ClaimTypes.Email, authUser.Email),
            new(ClaimTypes.Name, profile.DisplayName ?? $"{profile.FirstName} {profile.LastName}".Trim())
        };

        if (businessId.HasValue)
        {
            claims.Add(new Claim("business_id", businessId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(
            jwt,
            expiresAtUtc,
            profile.Id,
            authUser.Email,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            businessId);
    }
}
