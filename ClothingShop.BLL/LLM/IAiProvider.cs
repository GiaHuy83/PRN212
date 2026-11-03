using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL.Models;

namespace ClothingShop.BLL.LLM
{
    public interface IAiProvider
    {
        string ProviderName { get; }
        Task<(string answer, int latencyMs)> GenerateResponseAsync(string systemPrompt, string userPrompt, AppConfig config, CancellationToken ct = default);
    }
}

