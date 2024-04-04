using AlfaMicroserviceMesh.Models;

namespace AlfaMicroserviceMesh.Services;

public interface ITokenService {
    public string CreateToken(ClaimData claimData);
    public Task<Dictionary<string, string>> GetAccessData(string token);
    public bool IsValidToken(string token);
}