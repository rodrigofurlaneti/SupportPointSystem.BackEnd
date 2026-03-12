using System.Text.Json.Serialization;
namespace FSI.SupportPointSystem.Infrastructure.Services.Nominatim.Models
{
    public class NominatimAddress
    {
        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; set; }

        [JsonPropertyName("road")]
        public string Road { get; set; } = string.Empty;

        [JsonPropertyName("suburb")]
        public string? Suburb { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("county")]
        public string County { get; set; }

        [JsonPropertyName("state_district")]
        public string StateDistrict { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("ISO3166-2-lvl4")]
        public string Iso31662Lvl4 { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }
    }
}
