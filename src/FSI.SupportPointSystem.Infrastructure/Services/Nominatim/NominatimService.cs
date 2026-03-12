using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using FSI.SupportPointSystem.Infrastructure.Services.Nominatim.Models;
using System.Net.Http.Json;

namespace FSI.SupportPointSystem.Infrastructure.Services.Nominatim;

public class NominatimService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    public NominatimService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Address?> ObterEnderecoPorCoordenadasAsync(string lat, string lon)
    {
        var url = $"reverse?format=jsonv2&lat={lat}&lon={lon}";
        var response = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);

        if (response?.Address == null) return null;

        try
        {
            return Address.Create(
                street: response.Address.Road ?? "Não identificado",
                number: response.Address.HouseNumber ?? "S/N",
                complement: null,
                neighborhood: response.Address.Suburb ?? "Não identificado",
                city: response.Address.City ?? "Não identificado",
                state: response.Address.Iso31662Lvl4?.Split('-').Last() ?? "SP",
                zipCode: response.Address.Postcode ?? "00000000"
            );
        }
        catch
        {
            return null; 
        }
    }
}