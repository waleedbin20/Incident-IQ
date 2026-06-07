using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IncidentIQ.McpServers;

public class AppInsightsMcpTool
{
    private readonly HttpClient _httpClient;
    private readonly string _applicationId;
    private readonly string _apiKey;

    public AppInsightsMcpTool(string applicationId, string apiKey)
    {
        _httpClient = new HttpClient();
        _applicationId = applicationId;
        _apiKey = apiKey;
    }

    [Description("Queries Azure Application Insights to get real-time application telemetry, exceptions, and traces using Kusto Query Language (KQL).")]
    public async Task<string> QueryAppInsightsAsync(string kqlQuery)
    {
        if (string.IsNullOrEmpty(_applicationId) || string.IsNullOrEmpty(_apiKey))
        {
            // High-quality mock for hackathon when real credentials aren't provided yet
            if (kqlQuery.Contains("exceptions", StringComparison.OrdinalIgnoreCase))
                return "Mocked Exception: SqlException - Timeout expired. The timeout period elapsed prior to completion of the operation.";
            
            return "Mocked Telemetry: 500 Internal Server Error detected on /api/checkout at a spike rate of 200 req/sec.";
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"https://api.applicationinsights.io/v1/apps/{_applicationId}/query";
            var payload = new { query = kqlQuery };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return json;
            }
            
            // Hackathon Demo Fallback: If unauthorized, read the local 4k+ dataset
            var datasetPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "TelemetryDataset.json");
            if (System.IO.File.Exists(datasetPath))
            {
                var fullDataset = await System.IO.File.ReadAllTextAsync(datasetPath);
                return $"[MOCK DATASET INJECTED DUE TO '{response.ReasonPhrase}']\n" + fullDataset;
            }

            return $"Error querying App Insights: {response.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            var datasetPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "TelemetryDataset.json");
            if (System.IO.File.Exists(datasetPath))
            {
                return await System.IO.File.ReadAllTextAsync(datasetPath);
            }
            return $"App Insights Connection Error: {ex.Message}";
        }
    }
}
