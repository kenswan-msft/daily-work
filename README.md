# daily-work

A local-first assistant, researcher, and coach for a software engineer's daily workflow. Combines todo tracking, goal management, knowledge capture, AI-powered coaching, and fun — all running and stored locally.

Built with **.NET Aspire**, **Microsoft Agent Framework**, and **MCP (Model Context Protocol)**.

![.NET 10](https://img.shields.io/badge/.NET-10-blue)
![License: MIT](https://img.shields.io/badge/License-MIT-green)

---

## Features

### 🤖 AI Chat & Agents

- **Conversational assistant** via AGUI streaming — available in both CLI and web
- **Specialized agents** for goals, knowledge, blackjack, GitHub, .NET SDK, file system, projects, and Microsoft Docs
- **Agent orchestration** — top-level chat agent delegates to domain-specific sub-agents automatically
- **Conversation history** — browse, search, and resume previous conversations

### 🎯 Goals & Todos

- Full CRUD for goals and todos with priorities, tags, and target dates
- **Daily Focus** — smart ranked list using a scoring algorithm (due date urgency × priority × goal linkage × in-progress boost)
- Goal progress tracking with completion percentages and todo breakdowns

### 📚 Knowledge Base

- Save and organize **links**, **code snippets**, and **notes** with tags
- Search across all knowledge items by title, description, content, URL, or tag
- Browse by tag or recency with type filtering

### 🃏 Blackjack

- Persistent game engine with SQL Server storage
- $200 starting balance, 3:2 blackjack payout, 1:1 regular wins
- Full hand history tracking with balance changes

### 🌐 Web Dashboard

- **Dashboard** — overview stats, goal donut chart, daily focus list, overdue alerts
- **Goals page** — filterable goal cards with detail view and progress bars
- **Todos page** — data grid with status/priority filtering and goal associations
- **Knowledge page** — search bar, tag cloud, type filter, and card grid

### 💻 CLI (`daily`)

- Rich terminal UI powered by Spectre.Console
- Slash commands: `/history`, `/blackjack`, `/knowledge`
- Auto-launches the Aspire AppHost and waits for API readiness

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server container)
- [GitHub Copilot](https://github.com/features/copilot) access (for AI model provider)

---

## Installation

### Clone the repository

```bash
git clone https://github.com/kenswan/daily-work.git
cd daily-work
```

### Build the solution

```bash
dotnet build DailyWork.slnx
```

### Install the CLI tool (optional)

The `daily` command is a global .NET tool that provides a rich terminal chat interface.

**macOS / Linux:**

```bash
scripts/install.sh
```

**Windows (PowerShell):**

```powershell
scripts/install.ps1
```

The install script:
1. Packs the CLI project as a NuGet package with a timestamped version
2. Installs it as a global .NET tool named `daily`
3. Writes configuration to `~/.dailywork/config.json`

---

## Getting Started

### Run the full application

Start all services (API, web dashboard, MCP servers, SQL Server) with a single command:

```bash
dotnet run --project src/DailyWork.AppHost
```

This launches the .NET Aspire orchestrator which:
- Starts a **SQL Server** container (port 63141) with 6 databases
- Launches **7 MCP servers** for domain-specific tools
- Starts the **API** (https://localhost:7048) with AGUI chat endpoints
- Starts the **Web dashboard** (https://localhost:7200)
- Opens the **Aspire dashboard** for monitoring all resources

### Use the web dashboard

Navigate to **https://localhost:7200** to access the Blazor web application with:
- Goal and todo management
- Knowledge base browser
- AI chat interface
- Daily focus overview

### Use the CLI

If you installed the CLI tool, run it from any directory:

```bash
daily
```

The CLI will auto-detect whether the AppHost is running and launch it if needed, then connect to the AGUI chat endpoint.

### API endpoints

The REST API is available at **https://localhost:7048**:

| Endpoint | Description |
|----------|-------------|
| `POST /api/chat` | AGUI streaming chat endpoint |
| `GET /api/conversations` | List conversation metadata |
| `GET /api/conversations/{id}/messages` | Get messages for a conversation |
| `GET /openapi/v1.json` | OpenAPI spec (dev only) |

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     DailyWork.AppHost                         │
│                   (.NET Aspire Orchestrator)                  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐   ┌──────────────┐   ┌─────────────────┐   │
│  │ DailyWork   │   │ DailyWork    │   │ DailyWork.Cli   │   │
│  │ .Web        │──▶│ .Api         │   │ (global tool)   │   │
│  │ (Blazor)    │   │ (AGUI/REST)  │◀──│                 │   │
│  └─────────────┘   └──────┬───────┘   └─────────────────┘   │
│                           │                                  │
│                    ┌──────┴───────┐                           │
│                    │ DailyWork    │                           │
│                    │ .Agents      │                           │
│                    │ (9 agents)   │                           │
│                    └──────┬───────┘                           │
│                           │ MCP Protocol (HTTP)              │
│         ┌─────────┬───────┼───────┬──────────┐               │
│         ▼         ▼       ▼       ▼          ▼               │
│     ┌───────┐ ┌───────┐ ┌────┐ ┌──────┐ ┌────────┐          │
│     │Goals  │ │Know-  │ │BJ  │ │File  │ │Projects│ ...      │
│     │MCP    │ │ledge  │ │MCP │ │Sys   │ │MCP     │          │
│     └───┬───┘ └───┬───┘ └─┬──┘ └──┬───┘ └───┬────┘          │
│         ▼         ▼       ▼       ▼         ▼               │
│     ┌─────────────────────────────────────────┐              │
│     │         SQL Server (Docker)             │              │
│     │  goals-db │ knowledge-db │ blackjack-db │              │
│     │  filesystem-db │ projects-db │ conversations-db         │
│     └─────────────────────────────────────────┘              │
└──────────────────────────────────────────────────────────────┘
```

### Projects

| Project | Description |
|---------|-------------|
| **DailyWork.AppHost** | Aspire orchestrator — defines all infrastructure and wires service dependencies |
| **DailyWork.Api** | ASP.NET Core Web API — REST endpoints and AGUI chat streaming |
| **DailyWork.Web** | Blazor Server app — dashboard, goals, todos, knowledge UI (MudBlazor) |
| **DailyWork.Cli** | .NET global tool — rich terminal chat with Spectre.Console |
| **DailyWork.Agents** | Agent definitions, factories, and AI orchestration logic |
| **DailyWork.ServiceDefaults** | Shared Aspire defaults (OpenTelemetry, health checks, resilience) |
| **DailyWork.Mcp.Goals** | MCP server — goals, todos, tags, daily focus |
| **DailyWork.Mcp.Knowledge** | MCP server — links, snippets, notes, search |
| **DailyWork.Mcp.Blackjack** | MCP server — blackjack game engine |
| **DailyWork.Mcp.FileSystem** | MCP server — file system operations |
| **DailyWork.Mcp.Projects** | MCP server — .NET project tracking |
| **DailyWork.Mcp.GitHub** | MCP server — GitHub issues and pull requests |
| **DailyWork.Mcp.DotNet** | MCP server — .NET SDK info and NuGet package tools |
| **DailyWork.Mcp.Shared** | Shared CLI runner utilities for MCP servers |

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Orchestration | .NET Aspire 13.1 |
| AI Agents | Microsoft Agents.AI (RC2), GitHub Copilot SDK |
| MCP | ModelContextProtocol C# SDK v1.0.0 |
| Web UI | Blazor Server, MudBlazor 9.1 |
| CLI | Spectre.Console 0.54 |
| Database | SQL Server with EF Core 10 |
| Observability | OpenTelemetry (OTLP, ASP.NET Core, HTTP, Runtime) |
| Resilience | Microsoft.Extensions.Http.Resilience, Service Discovery (Yarp) |

---

## Local Development & Debugging

### Project structure

```
daily-work/
├── src/
│   ├── DailyWork.AppHost/          # Aspire orchestrator
│   ├── DailyWork.Api/              # REST API + AGUI endpoints
│   ├── DailyWork.Web/              # Blazor Server dashboard
│   ├── DailyWork.Cli/              # Global CLI tool
│   ├── DailyWork.Agents/           # Agent definitions & factories
│   ├── DailyWork.ServiceDefaults/  # Shared Aspire config
│   ├── DailyWork.Mcp.Goals/        # MCP servers...
│   ├── DailyWork.Mcp.Knowledge/
│   ├── DailyWork.Mcp.Blackjack/
│   ├── DailyWork.Mcp.FileSystem/
│   ├── DailyWork.Mcp.Projects/
│   ├── DailyWork.Mcp.GitHub/
│   ├── DailyWork.Mcp.DotNet/
│   └── DailyWork.Mcp.Shared/
├── test/                           # Test projects (mirrors src/)
├── docs/                           # Feature roadmap
├── scripts/                        # CLI install scripts
├── Directory.Build.props           # Shared build settings
├── Directory.Packages.props        # Central package management
└── DailyWork.slnx                  # Solution file
```

### Build commands

```bash
# Build the entire solution
dotnet build DailyWork.slnx

# Run all tests
dotnet test DailyWork.slnx

# Run a specific test project
dotnet test test/DailyWork.Api.Test/DailyWork.Api.Test.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Namespace.TestClass.TestMethod"
```

### Running & debugging

**Full stack (recommended):**

```bash
dotnet run --project src/DailyWork.AppHost
```

This starts everything. The Aspire dashboard provides real-time visibility into all services, logs, traces, and metrics.

**Individual projects:**

You can also run individual projects for focused development, but they will need their dependencies (SQL Server, MCP servers) to be available:

```bash
# API only (requires SQL Server and MCP servers)
dotnet run --project src/DailyWork.Api

# Web only (requires API)
dotnet run --project src/DailyWork.Web
```

**IDE debugging:**

Set **DailyWork.AppHost** as the startup project in your IDE (Visual Studio, Rider, VS Code). The Aspire orchestrator will start all dependent services. You can attach debuggers to individual projects from the Aspire dashboard.

### Aspire dashboard

When running via the AppHost, the Aspire dashboard is available with:
- **Resource monitoring** — status of all services and containers
- **Structured logs** — centralized logging across all projects
- **Distributed traces** — end-to-end request tracing
- **Metrics** — runtime and HTTP metrics via OpenTelemetry

### Database

SQL Server runs in Docker via Aspire with:
- **Persistent lifetime** — container survives AppHost restarts
- **Data volume** — data persists across container recreations
- **Port**: 63141

Six databases are provisioned automatically:

| Database | Used by |
|----------|---------|
| `goals-db` | Goals MCP server, API (read) |
| `knowledge-db` | Knowledge MCP server, API (read) |
| `blackjack-db` | Blackjack MCP server |
| `filesystem-db` | FileSystem MCP server |
| `projects-db` | Projects MCP server, API (read) |
| `conversations-db` | API (conversation history) |

EF Core migrations run automatically on startup in development.

### Testing

The project uses **xUnit v3** with **NSubstitute** for mocking and **WebApplicationFactory** for API functional tests.

```bash
# Run all tests
dotnet test DailyWork.slnx

# Run with detailed output
dotnet test DailyWork.slnx --verbosity normal
```

Test projects mirror the source structure under `test/`:

| Test Project | Covers |
|-------------|--------|
| `DailyWork.Api.Test` | API endpoints (WebApplicationFactory) |
| `DailyWork.Agents.Test` | Agent factories and chat logic |
| `DailyWork.Cli.Test` | CLI commands |
| `DailyWork.Web.Test` | Blazor components (bunit) |
| `DailyWork.Mcp.*.Test` | Individual MCP server tools (6 projects) |

### Code style

Code style is enforced at build time via `.editorconfig` and `Directory.Build.props`:
- **Warnings as errors** — all warnings fail the build
- **Analyzers** — 50+ CA/IDE rules enabled
- **File-scoped namespaces** required
- **Allman-style braces** (opening brace on new line)
- **`var`** only when the type is apparent from the right side

### Configuration

**CLI configuration** is stored at `~/.dailywork/config.json` (created by the install script):

```json
{
  "AppHostProjectPath": "/path/to/src/DailyWork.AppHost",
  "DailyWorkApiOptions": {
    "BaseAddress": "https://localhost:7048",
    "ChatEndpoint": "/api/chat"
  }
}
```

**NuGet packages** are centrally managed in `Directory.Packages.props` — individual `.csproj` files reference packages without version numbers.

---

## Roadmap

See [docs/Feature-Forecast.md](docs/Feature-Forecast.md) for the full feature roadmap, including planned features for reminders, Azure DevOps integration, weekly coaching, developer workflow tools, and more.

---

## License

[MIT](LICENSE) © Ken Swan
