using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IncidentIQ.McpServers;

public class FabricIQMcpTool
{
    private readonly HttpClient _httpClient;
    private readonly string _workspaceId;
    private readonly string _accessToken;

    public FabricIQMcpTool(string workspaceId, string accessToken)
    {
        _httpClient = new HttpClient();
        _workspaceId = workspaceId;
        _accessToken = accessToken;
    }

    [Description("Executes a Kusto Query Language (KQL) query against Microsoft Fabric Real-Time Intelligence (OneLake) to analyze server telemetry and logs.")]
    public async Task<string> ExecuteKqlQueryAsync(string kqlQuery)
    {
        if (string.IsNullOrEmpty(_workspaceId) || string.IsNullOrEmpty(_accessToken))
        {
            // High-quality mock for hackathon when real credentials aren't provided yet
            if (kqlQuery.Contains("CPU", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL: CPU pegged at 100% on Auth-Service due to thread starvation. (Mocked response)";
            
            return "Anomaly Detected: Database connection locks detected in AppInsights trace. (Mocked response)";
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Fabric SQL / KQL endpoint
            var url = $"https://api.fabric.microsoft.com/v1/workspaces/{_workspaceId}/kql/execute";
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
            
            return $"Error querying Fabric: {response.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            var datasetPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "TelemetryDataset.json");
            if (System.IO.File.Exists(datasetPath))
            {
                return await System.IO.File.ReadAllTextAsync(datasetPath);
            }
            return $"Fabric Connection Error: {ex.Message}";
        }
    }
}
