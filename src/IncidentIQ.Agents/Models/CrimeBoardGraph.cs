using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IncidentIQ.Agents.Models;

public class CrimeBoardGraph
{
    [JsonPropertyName("incident_id")]
    public string IncidentId { get; set; } = string.Empty;

    [JsonPropertyName("root_cause_summary")]
    public string RootCauseSummary { get; set; } = string.Empty;

    [JsonPropertyName("nodes")]
    public List<Node> Nodes { get; set; } = new();

    [JsonPropertyName("edges")]
    public List<Edge> Edges { get; set; } = new();

    [JsonPropertyName("remediation")]
    public Remediation Action { get; set; } = new();
}

public class Node
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

public class Edge
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

public class Remediation
{
    [JsonPropertyName("action")]
    public string ActionDesc { get; set; } = string.Empty;

    [JsonPropertyName("requires_approval")]
    public bool RequiresApproval { get; set; }
}
