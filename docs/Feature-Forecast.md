# Features & Roadmap

Feature tracking and roadmap planning for DailyWork.

---

## ✅ Implemented Features

### CLI Chat & Commands

- **AI Chat** — Conversational assistant via AGUI streaming (`/api/chat`)
- **`/history`** — Browse and resume previous conversations with metadata
- **`/blackjack`** — Dedicated blackjack game mode with natural language dealer agent

### Goals & Todos

- **Goals MCP Server** — Full CRUD for goals with priorities, tags, target dates
- **Todos MCP Server** — CRUD for todos linked to goals; filtering by status, priority, tag, due date range
- **Tags** — Create, list (with usage counts), attach/remove from goals and todos
- **Daily Focus** — Scored ranking algorithm (due date urgency × priority × goal linkage × in-progress boost)
- **Goal Progress** — Completion percentage and todo breakdown per goal

### Blackjack

- **Blackjack MCP Server** — Persistent game engine with SQL Server storage
- **Balance Tracking** — Starting $200, 3:2 blackjack payout, 1:1 regular wins
- **Game History** — Full hand history with outcomes and balance changes

### Web Dashboard

- **Dashboard** — Overview stats, goal donut chart, daily focus list, overdue alerts
- **Goals Page** — Filterable goal cards with detail view and progress bars
- **Todos Page** — Data grid with status/priority filtering and goal associations

### Infrastructure

- **.NET Aspire AppHost** — Full orchestration with service discovery
- **SQL Server** — `goals-db` and `blackjack-db` with EF Core migrations
- **Azure Cosmos DB** — Conversation and metadata storage
- **MCP Protocol** — HTTP transport servers with auto-discovered tools

---

## 🔜 Planned Features

### Knowledge & Links Manager

> A personal knowledge base for storing helpful links, code snippets, articles, and notes — searchable and taggable.

- [ ] `DailyWork.Mcp.Knowledge` — MCP server with SQL Server storage
  - [ ] **SaveLink** — Store URL with title, description, tags, and category
  - [ ] **SaveSnippet** — Store code snippets with language, description, and tags
  - [ ] **SaveNote** — Free-form notes with markdown support
  - [ ] **Search** — Full-text search across all saved items
  - [ ] **ListByTag** — Browse items by tag
  - [ ] **ListRecent** — Recently saved items
- [ ] `KnowledgeAgent` — AI agent for natural language knowledge management
  - "Save this link about Aspire health checks"
  - "Find my notes about EF Core migrations"
  - "What links did I save about MudBlazor?"
- [ ] CLI `/links` or `/knowledge` slash command
- [ ] Web dashboard Knowledge page with search and tag cloud

### Reminders & Notifications

> Time-based and context-aware reminders that surface in both CLI and web.

- [ ] `DailyWork.Mcp.Reminders` — MCP server with scheduling
  - [ ] **CreateReminder** — Set a reminder with message, due time, and recurrence
  - [ ] **ListUpcoming** — Show reminders due within a time window
  - [ ] **SnoozeReminder** — Push a reminder forward
  - [ ] **DismissReminder** — Mark as acknowledged
- [ ] Integration with Daily Focus scoring (reminders boost focus score)
- [ ] CLI notification on startup ("You have 3 reminders today")
- [ ] Web dashboard Reminders widget

### Azure DevOps Integration

> Connect to Azure DevOps for work item tracking alongside local goals/todos.

- [ ] Integrate `microsoft/azure-devops-mcp` server in AppHost
- [ ] `DevOpsAgent` — Query and update work items via natural language
  - "What stories are assigned to me this sprint?"
  - "Update task 1234 to in progress"
- [ ] CLI `/devops` slash command
- [ ] Web dashboard DevOps widget showing current sprint items
- [ ] Sync/link ADO work items with local goals

### Weekly Review & Coaching

> AI-powered weekly review that summarizes accomplishments and suggests focus areas.

- [ ] `CoachAgent` — Analyzes goals, todos, and history to provide coaching
  - Weekly accomplishment summary
  - Stalled goal detection
  - Priority rebalancing suggestions
  - Streak tracking (consecutive productive days)
- [ ] CLI `/review` slash command
- [ ] Web dashboard Weekly Review page with charts and insights

### Developer Workflow Tools

> Tools that integrate with the local development environment.

- [ ] **Git Summary** — Summarize recent commits across local repos
- [ ] **PR Tracker** — Track open PRs and review status
- [ ] **Build Status** — Monitor CI/CD pipeline results
- [ ] **Dependency Updates** — Track outdated NuGet/npm packages

---

## 💡 Future Ideas

### Fun & Engagement

- [ ] **Daily Standup Mode** — Guided standup format (yesterday/today/blockers)
- [ ] **Achievements/Badges** — Gamification for completing goals and maintaining streaks
- [ ] **Blackjack Tournaments** — Multi-session leaderboard tracking
- [ ] **Pomodoro Timer** — Focus timer integrated with todo tracking

### Productivity Analytics

- [ ] **Time Tracking** — Optional time logging per todo/goal
- [ ] **Velocity Charts** — Todos completed per week/month trending
- [ ] **Goal Burndown** — Visual burndown chart for goal progress over time
- [ ] **Focus Score History** — Track how daily focus scores change over time

### Integration & Sync

- [ ] **GitHub Issues Sync** — Two-way sync between local todos and GitHub issues
- [ ] **Calendar Integration** — Import meetings to help with daily planning
- [ ] **Obsidian/Markdown Export** — Export knowledge base to Obsidian vault
- [ ] **Mobile PWA** — Progressive web app for on-the-go access

### Infrastructure & Quality

- [ ] **Playwright MCP** — Web browsing for research when using local models
- [ ] **Docker Model Runner** — Local AI model support as alternative to Copilot SDK
- [ ] **Offline Mode** — Full functionality without cloud AI (local models only)
- [ ] **Data Export/Import** — Backup and restore all local data
- [ ] **Multi-user Support** — Separate profiles for shared machines
