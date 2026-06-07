# 🚨 Incident-IQ - Autonomous P1 War Room

[![Agents League 2026](https://img.shields.io/badge/Agents%20League-2026-blue)](https://github.com/microsoft/agentsleague)
[![Track](https://img.shields.io/badge/Track-Enterprise%20Agents-purple)](https://github.com/microsoft/agentsleague/tree/main/starter-kits/3-enterprise-agents)
[![Built with](https://img.shields.io/badge/Built%20with-GitHub%20Copilot-green)](https://github.com/features/copilot)
[![Microsoft IQ](https://img.shields.io/badge/Microsoft%20IQ-Fabric%20IQ-orange)](https://learn.microsoft.com/en-us/fabric/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

> **When systems go down, seconds matter.** Incident-IQ replaces the chaotic 30-person manual incident response call with a multi-agent swarm. It autonomously translates unstructured user complaints, live public GitHub code changes, and Microsoft Fabric server telemetry into a visual, causal "Crime Board" graph — cutting P1 resolution times from hours to seconds.

---

## 🎬 Demo

> 📹 _[Watch the demo video →](https://www.loom.com/share/33c4410f2ce64c83a8231df8c13116f0)_

**Try it yourself:** Clone the repo, configure your API keys, run `dotnet run`, and launch the Angular frontend.

---

## 🌟 Features

### 🧠 Multi-Agent Deep Reasoning

Incident-IQ utilizes a `RoundRobinGroupChatManager` from the `.NET 10 Microsoft.Agents.AI.Workflows` SDK. Four specialized agents collaborate to investigate the outage:

| Agent Role        | Responsibility       | Microsoft IQ / Tool Integration                                                                                                                     |
| ----------------- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Commander**     | Master Orchestrator  | Coordinates the swarm, resolves contradictions, and generates the final JSON Crime Board graph.                                                     |
| **Support-Agent** | User Impact Analysis | Parses raw unstructured user complaints to identify structured, real-time user symptoms (e.g., checkout timeouts).                                  |
| **Infra-Agent**   | Telemetry Analysis   | Queries **Microsoft Fabric IQ** (OneLake/KQL) and **Azure Application Insights** to hunt for server anomalies like CPU spikes or thread starvation. |
| **DevOps-Agent**  | Code Change Tracking | Dynamically queries **Live Public GitHub Repositories** for recently merged Pull Requests that align with the incident timeline.                    |

### 🕸️ Visual "Crime Board" Graph

The output of the swarm is not a block of text, but a strict JSON schema that maps the exact causal chain of the outage. This JSON is pushed via SignalR to a live **D3.js Angular visualization**, rendering nodes (PRs, Servers, Users) and their causal edges.

### 🔍 Live "Glass Box" Tracing

Trusting AI during a P1 outage is difficult. Incident-IQ intercepts the internal LLM reasoning stream and broadcasts every internal thought, tool execution, and agent response directly to the frontend via SignalR. You watch the agents think in real-time.

### 🔌 Microsoft Fabric IQ Integration

Fully integrated with **Fabric IQ** and **Azure Application Insights**, the `Infra-Agent` is equipped with MCP tools that execute Kusto Query Language (KQL) queries directly against your enterprise OneLake clusters and live telemetry endpoints.

---

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js](https://nodejs.org/) v18+ & Angular CLI (for Frontend)
- [Azure AI Foundry](https://ai.azure.com/) Project (for GPT-4o model)

### Installation

````bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/incident-iq.git
cd incident-iq

# Configure your keys
# 1. Create a new file named `appsettings.json` in the `src/IncidentIQ.Api/` directory.
# 2. Input your Azure AI Foundry, GitHub PAT, Fabric IQ, and AppInsights credentials using the exact JSON structure below:
```json
{
  "AI": {
    "Endpoint": "https://YOUR_RESOURCE_NAME.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "ModelId": "gpt-4o"
  },
  "GitHub": {
    "PersonalAccessToken": "YOUR_GITHUB_PAT"
  },
  "Fabric": {
    "WorkspaceId": "YOUR_WORKSPACE_ID",
    "KqlDatabaseId": "YOUR_KQL_DB_ID",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "AppInsights": {
    "AppId": "YOUR_APP_ID",
    "ApiKey": "YOUR_API_KEY"
  }
}
```

> **Note:** `appsettings.json` is ignored by git to protect your secrets.

### Run the Application

```bash
# Start the Backend (in Terminal 1)
cd src/IncidentIQ.Api
dotnet run

# Start the Frontend (in Terminal 2, from the project root)
cd ui/incident-iq-app
npm install
npm start
```
npm run start

```

Open **http://localhost:4200** in your browser and click **"Initiate Investigation"**. 🚨

---

## 📁 Project Structure

```

incident-iq/
├── src/
│ ├── IncidentIQ.Api/ # ASP.NET Core 10 Web API
│ │ ├── Controllers/ # REST Endpoints
│ │ ├── Hubs/ # SignalR real-time hubs for Glass Box tracing
│ │ ├── Program.cs # DI Container & Configuration
│ │ └── appsettings.json # Credential configuration (Azure, Fabric, AppInsights, GitHub)
│ ├── IncidentIQ.Agents/ # Agent Framework Logic
│ │ ├── Prompts/ # Text-based system prompts for all 4 agents
│ │ └── SwarmOrchestrator.cs # GroupChatManager & WorkflowBuilder logic
│ ├── IncidentIQ.McpServers/ # Tool Integrations
│ │ ├── AppInsightsMcpTool.cs # Azure Monitor Application Insights Integration
│ │ ├── FabricIQMcpTool.cs # Microsoft Fabric KQL Integration
│ │ └── GitHubMcpTool.cs # GitHub PR Integration
│ └── IncidentIQ.Web/ # Angular 19 Frontend
│ ├── src/app/
│ │ ├── glass-box/ # Live trace visualization component
│ │ └── crime-board/ # D3.js node graph renderer
├── .gitignore
├── LICENSE
└── README.md

````

---

## 🤖 GitHub Copilot Usage

GitHub Copilot was instrumental throughout the development of Incident-IQ. Here's how AI-assisted development shaped this project:

### 🧠 Architecture & Design
- Brainstormed the `RoundRobinGroupChatManager` topology for the agent swarm.
- Designed the causal JSON schema that the Commander agent must output to feed the D3.js graph.
- Assisted in mapping out the exact reasoning lifecycle for P1 resolution.

> **High-Level Architecture Diagram:**
> We have documented the full system design (including Frontend, Backend API, Orchestrator, Agent Swarm, and external System connections) in an Excalidraw diagram.
> 📂 **View the diagram:** [.github/docs/architecture.excalidraw](.github/docs/architecture.excalidraw)

### ⚡ Code Acceleration
- Generated the boilerplate for the `SignalR` hubs to stream `AgentResponseUpdateEvent` directly to the frontend.
- Refactored legacy MCP mock tools into strongly-typed `AITool` definitions compatible with the new `.NET 10 Microsoft.Extensions.AI` SDK.
- Handled DI registration and appsettings configuration bindings effortlessly.

### 🎨 Creative Exploration
- Helped fine-tune the system prompts for the sub-agents to ensure strict, zero-hallucination analysis of the telemetry data.

---

## 🔌 API Reference

### `POST /api/incident/investigate`
Trigger the autonomous agent swarm.

**Request:**
```json
{
  "incident_id": "P1-9942",
  "description": "Users are reporting checkout timeouts."
}
````

**Response:** Streams live SignalR events, concluding with the final Crime Board JSON graph payload.

---

## 🏆 Evaluation Criteria Alignment

| Criteria                            | Weight | How Incident-IQ Delivers                                                                                                                                     |
| ----------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Accuracy & Relevance**            | 20%    | Directly addresses the Enterprise Agents track. Integrates seamlessly with **Microsoft Fabric IQ** to solve a real-world enterprise pain point (P1 outages). |
| **Reasoning & Multi-step Thinking** | 20%    | Utilizes a 4-agent swarm. Support finds the symptom, DevOps finds the trigger, Infra verifies the metric, and Commander mathematically correlates them.      |
| **Creativity & Originality**        | 15%    | Moving away from traditional text-chat into a visual, causal node graph (the "Crime Board") backed by a live streaming "Glass Box".                          |
| **User Experience & Presentation**  | 15%    | Polished Angular UI with D3.js physics-based graphs and a live terminal interface for tracking the LLM's thought process.                                    |
| **Reliability & Safety**            | 20%    | Uses local, in-memory `.NET` orchestration preventing state-leaks. The Commander is strictly constrained to output JSON, preventing hallucinations.          |

---

## 🛠️ Technologies

| Technology          | Purpose                                      |
| ------------------- | -------------------------------------------- |
| .NET 10             | Core Backend Framework                       |
| Microsoft.Agents.AI | Multi-Agent Orchestration (Group Chat)       |
| Microsoft Fabric IQ | Real-time Server Telemetry & KQL Analytics   |
| Azure App Insights  | Application exceptions and tracing           |
| Azure AI Foundry    | Cloud LLM Infrastructure (GPT-4o)            |
| SignalR             | Real-time "Glass Box" telemetry streaming    |
| Angular 19          | Frontend Web Application                     |
| D3.js               | Crime Board node graph physics and rendering |

---

## 🎯 Use Cases

1. **Enterprise Incident Management** — Automatically triaging Sev-1 outages across massive microservice architectures.
2. **Security Operations (SecOps)** — Correlating intrusion alerts with suspicious commits and firewall logs.
3. **Customer Success** — Mapping a spike in negative feedback directly to a specific deployment or infrastructure degradation.

---

## 📝 License

[MIT License](LICENSE) — free to use, modify, and distribute.

---

<p align="center">
  <strong>🚨 Incident-IQ: Because your War Room shouldn't be a War Zone. 🚨</strong>
</p>
