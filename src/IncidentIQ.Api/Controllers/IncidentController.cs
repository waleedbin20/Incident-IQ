using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IncidentIQ.Agents;

using Microsoft.Extensions.Logging;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentController : ControllerBase
{
    private readonly SwarmOrchestrator _orchestrator;
    private readonly ILogger<IncidentController> _logger;

    public IncidentController(SwarmOrchestrator orchestrator, ILogger<IncidentController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public record InvestigateRequest(string incident_id, string description, string repo_name);

    [HttpPost("investigate")]
    public async Task<IActionResult> Investigate([FromBody] InvestigateRequest req)
    {
        _logger.LogInformation("Received request to investigate incident.");
        try
        {
            var result = await _orchestrator.InvestigateAsync(req.description, req.repo_name ?? "microsoft/TypeScript");
            _logger.LogInformation("Investigation completed successfully.");
            return Content(result, "application/json");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Investigation failed. Falling back to mock data.");
            // For hackathon, return a fallback mock graph if Azure AI isn't configured
            var fallback = @"{
              ""incident_id"": ""P1-9942"",
              ""root_cause_summary"": ""Error: " + ex.Message + @"\nFallback: PR #12 introduced a regression causing thread starvation on Auth-Service, leading to user checkout timeouts."",
              ""nodes"": [
                { ""id"": ""symptom_1"", ""type"": ""user_report"", ""label"": ""Jira: Checkout Timeouts"" },
                { ""id"": ""infra_1"", ""type"": ""server"", ""label"": ""Fabric: Auth-Service 100% CPU"" },
                { ""id"": ""cause_1"", ""type"": ""code"", ""label"": ""GitHub: PR #12 (Auth.cs)"" }
              ],
              ""edges"": [
                { ""source"": ""symptom_1"", ""target"": ""infra_1"", ""label"": ""caused by"" },
                { ""source"": ""infra_1"", ""target"": ""cause_1"", ""label"": ""traced to"" }
              ],
              ""remediation"": {
                ""action"": ""Rollback GitHub PR #12"",
                ""requires_approval"": true
              }
            }";
            return Content(fallback, "application/json");
        }
    }
}
