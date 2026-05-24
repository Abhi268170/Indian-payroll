using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Payroll.Api.Tests.Infrastructure;

// Generates signed test bearer tokens using an in-memory RSA key.
// The key NEVER appears in config files — it is registered into the test WAF only.
public sealed class TestJwtFactory
{
    private readonly RsaSecurityKey _signingKey;
    private readonly string _issuer;

    public TestJwtFactory(string issuer = "https://localhost")
    {
        RSA rsa = RSA.Create(2048);
        _signingKey = new RsaSecurityKey(rsa);
        _issuer = issuer;
    }

    public RsaSecurityKey SigningKey => _signingKey;

    public string CreateToken(
        Guid userId,
        string email,
        string[] roles,
        Guid? tenantId = null,
        string? tenantSchema = null,
        TimeSpan? lifetime = null)
    {
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
        ];

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        if (tenantSchema is not null)
        {
            claims.Add(new Claim("tenant_schema", tenantSchema));
        }

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _issuer,
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256),
        };

        JsonWebTokenHandler handler = new();
        return handler.CreateToken(descriptor);
    }
}
