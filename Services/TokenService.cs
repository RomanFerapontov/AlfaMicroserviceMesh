using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AlfaMicroserviceMesh.Services;

public class TokenService : ITokenService {
    private readonly IConfiguration _config;
    private readonly string signingKey;
    private readonly string issuer;
    private readonly string audience;

    public TokenService(IConfiguration config) {
        _config = config;
        signingKey = _config["JWT:SigningKey"]!;
        issuer = _config["JWT:Issuer"] ?? "";
        audience = _config["JWT:Audience"] ?? "";
    }

    public string CreateToken(ClaimData claimData) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        List<Claim> claims = [
            new Claim("uid", claimData.Uid),
            new Claim("role", claimData.Role),
        ];

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescription = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(7),
            SigningCredentials = creds,
            Issuer = issuer,
            Audience = audience,
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescription);

        return tokenHandler.WriteToken(token);
    }

    public async Task<Dictionary<string, string>> GetAccessData(string token) {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!IsValidToken(token))
            throw new MicroserviceException(["Invalid token"], 403, "AUTHORIZATION_ERROR");

        var tokenData = tokenHandler.ReadToken(token).ToString()!.Split(".")[1];

        Dictionary<string, string> accessData = [];

        var tokenVariables = await tokenData.DeserializeAsync<Dictionary<string, object>>()!;

        foreach (var row in tokenVariables)
        {
            accessData[row.Key] = row.Value.ToString()!;
        }

        return accessData;
    }

    public bool IsValidToken(string token) {
        try {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var validationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken _);
            return true;
        }
        catch {
            return false;
        }
    }
}