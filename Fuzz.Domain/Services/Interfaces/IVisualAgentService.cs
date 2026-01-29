using Fuzz.Domain.Models;

namespace Fuzz.Domain.Services.Interfaces;

public interface IVisualAgentService
{
    Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId);
    void ClearHistory();
}
