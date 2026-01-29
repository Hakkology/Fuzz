using Fuzz.Domain.Models;

namespace Fuzz.Domain.Services.Interfaces;

public interface ISoundAgentService
{
    Task<FuzzResponse> GenerateMusicAsync(string prompt, string userId);
    void ClearHistory();
}
