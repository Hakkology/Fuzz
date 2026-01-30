using Fuzz.Domain.Models;

namespace Fuzz.Domain.Services.Interfaces;

public interface IFuzzAgentService
{
    Task<FuzzResponse> ProcessCommandAsync(string input, string userId, bool useTools = true);
    void ClearHistory();
    string? LastSql { get; }
}