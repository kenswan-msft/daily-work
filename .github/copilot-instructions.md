---
description: "Guidelines for daily-work development within workspace."
---

# daily-work

A local-first assistant, researcher, and coach for a software engineer's daily workflow. Combines todo tracking, Azure DevOps story tracking, goal coaching, helpful reminders, and AI-powered ideation — all running and stored locally.

## Architecture

This is a **.NET Aspire** application orchestrating several projects from a single AppHost:

- **DailyWork.AppHost** — Aspire orchestrator. Defines all infrastructure resources (SQL Server, Cosmos DB) and wires project references with service discovery.
- **DailyWork.Web** — Blazor Server web application. Primary UI for the dashboard, todo management, coaching, and ideation.
- **DailyWork.Api** — ASP.NET Core Web API backend. Exposes REST endpoints and AGUI chat endpoints (`.MapAGUI()` from Microsoft Agent Framework) consumed by the web app and CLI.
- **DailyWork.Cli** — .NET CLI tool using **Spectre.Console** for rich terminal UX. Connects to AGUI endpoints for chat-based features.
- **DailyWork.Agent** — AI agent logic. Hosts agent definitions, prompt orchestration, and tool integrations.
- **DailyWork.ServiceDefaults** — Shared Aspire service defaults (OpenTelemetry, health checks, resilience).
- **DailyWork.Mcp.\*** — MCP server projects exposing domain-specific tools to AI agents.

### Infrastructure (Docker via Aspire)

- **SQL Server** — Primary relational store (todos, reminders, goals). Use EF Core with Aspire's SQL Server integration (`Aspire.Microsoft.EntityFrameworkCore.SqlServer`).
- **Azure Cosmos DB Emulator** — Document store for conversations and agent state. Use Aspire's Cosmos integration (`Aspire.Microsoft.Azure.Cosmos`).

### AI Stack

- **Microsoft Agent Framework** — Primary framework for building agents ([microsoft/agent-framework/dotnet](https://github.com/microsoft/agent-framework/tree/main/dotnet))
- **ModelContextProtocol C# SDK** — MCP server/client integration ([modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk))
- **AI model access** — Docker Model Runner for local models and/or GitHub Copilot SDK
- Reference implementation: [kenswan-msft/Agentic](https://github.com/kenswan-msft/Agentic)

## Build and Run

```bash
# Run the full Aspire application (starts all projects + Docker containers)
dotnet run --project src/DailyWork.AppHost

# Build the entire solution
dotnet build DailyWork.slnx

# Run all tests
dotnet test DailyWork.slnx

# Run a single test by fully qualified name
dotnet test --filter "FullyQualifiedName~Namespace.TestClass.TestMethod"

# Run tests in a specific project
dotnet test test/DailyWork.Api.Test/DailyWork.Api.Test.csproj
```

## Conventions

### Project Structure

- Solution uses `.slnx` format with a `/SolutionItems/` folder for root config files.
- Source projects live under `src/`. Test projects live under `test/`. Project naming follows `DailyWork.{Component}` (e.g., `DailyWork.Api`, `DailyWork.Web`).
- Test projects follow `DailyWork.{Component}.Test` naming (e.g., `DailyWork.Api.Test`).
- MCP server projects follow `DailyWork.Mcp.{ToolName}` naming.

### .NET Conventions

- **Target framework**: .NET 10 (`net10.0`)
- **Central package management**: All NuGet versions defined in `Directory.Packages.props`. Individual `.csproj` files reference packages without version numbers.
- **Build settings** (via `Directory.Build.props`):
  - `TreatWarningsAsErrors`: true
  - `EnforceCodeStyleInBuild`: true
  - `LangVersion`: preview
  - `ImplicitUsings`: enable
  - `Nullable`: annotations
- **Namespaces**: File-scoped (`namespace Foo;` not `namespace Foo { }`)
- **`var` usage**: Use `var` only when the type is apparent from the right side. Use explicit types for built-in types and non-obvious assignments.
- **Expression-bodied members**: Preferred for methods when the body is a single expression.
- **EF Core migrations**: Located in a `Migrations/` folder with relaxed analyzer rules (no unused-using or expression-body warnings).

### Aspire Patterns

- AppHost defines infrastructure resources (SQL, Cosmos) with `ContainerLifetime.Persistent` and data volumes for dev stability.
- Use `.WithReference()` and `.WaitFor()` to wire dependencies between projects and resources.
- Health check endpoints at `/health` on API projects.

### Agent Communication (AGUI)

- The API exposes chat/agent interactions via **AGUI** endpoints using `.MapAGUI()` from Microsoft Agent Framework.
- Both the Blazor web app and CLI are AGUI clients — they connect to the same AGUI endpoints for all chat-based features (coaching, ideation, etc.).
- The CLI uses **Spectre.Console** for rendering rich terminal output (tables, progress, markup).

### Code Style

- Formatting and analyzers enforced via `.editorconfig` — let the editor/build handle formatting.
- Allman-style braces (opening brace on new line).
- `system` usings are NOT sorted first (`dotnet_sort_system_directives_first = false`).

## MCP Servers

The application uses custom MCP servers (`DailyWork.Mcp.*`) to expose domain-specific tools to AI agents. Additionally, the following external MCP servers are integrated via the Aspire AppHost:

### Playwright MCP Server

Used for web browsing and search capabilities, especially important when running with local models via Docker AI Model Runner (which lack built-in web access). Hosted as a Docker container in the AppHost:

```csharp
builder.AddContainer("playwright-mcp", "mcr.microsoft.com/playwright/mcp")
    .WithArgs("--port", "3000", "--headless", "--host", "0.0.0.0", "--browser", "chromium", "--isolated")
    .WithEndpoint(port: 3000, targetPort: 3000, name: "mcp", scheme: "http")
    .WithContainerRuntimeArgs("--ipc=host", "--init", "--rm")
    .WithExternalHttpEndpoints();
```

When using GitHub Copilot SDK as the model provider, web search may already be available as a built-in tool — Playwright provides this capability for local model scenarios.

### Azure DevOps MCP Server

Used for Azure DevOps work item tracking integration — querying user stories, tasks, and bugs directly from the app. Official Microsoft server: [microsoft/azure-devops-mcp](https://github.com/microsoft/azure-devops-mcp). Key tools: `list_work_items`, `get_work_item`, `batch_get_work_items`.
