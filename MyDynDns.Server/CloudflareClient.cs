using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MyDynDns.Server
{
    public class CloudflareClient
    {
        private const string CloudflareApiBaseUrl = "https://api.cloudflare.com/client/v4/";

        private readonly HttpClient _httpClient;

        public CloudflareClient(string apiToken)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(CloudflareApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }

        public async Task<string> GetZones()
        {
            var result = await _httpClient.GetAsync("zones");

            return await result.Content.ReadAsStringAsync();
        }
        public async Task<string> GetZoneDns(string zoneIdentifier)
        {
            var result = await _httpClient.GetAsync($"zones/{zoneIdentifier}/dns_records");

            return await result.Content.ReadAsStringAsync();
        }

        public async Task<string> UpdateDnsZone(string zoneName, string zoneIdentifier, string recordIdentifier, string content)
        {
            var result = await _httpClient.PutAsJsonAsync($"zones/{zoneIdentifier}/dns_records/{recordIdentifier}", new
            {
                type = "A",
                name = zoneName,
                content = content,
                ttl = 120,
                proxied = false
            });

            return await result.Content.ReadAsStringAsync();
        }
    }
}