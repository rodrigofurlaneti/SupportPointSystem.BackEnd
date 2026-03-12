using FSI.SupportPointSystem.Domain.ValueObjects;

namespace FSI.SupportPointSystem.Domain.Interfaces.Services
{
    public interface IGeocodingService
    {
        Task<Address?> ObterEnderecoPorCoordenadasAsync(string lat, string lon);
    }
}
