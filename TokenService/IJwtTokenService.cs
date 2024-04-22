using AlfaMicroserviceMesh.Models;

namespace AlfaMicroserviceMesh.TokenService;

public interface IJwtTokenService {
    public string CreateToken(ClaimData claimData);
    public Task<Dictionary<string, string>> GetAccessData(string token);
    public bool IsValidToken(string token);
}