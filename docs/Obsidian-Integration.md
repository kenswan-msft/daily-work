# Obsidian Integration

Feature ideas for integrating DailyWork with [Obsidian](https://obsidian.md/) vaults. The MCP architecture makes most of these straightforward — a new `DailyWork.Mcp.Obsidian` server that reads/writes the vault, plus wiring an `ObsidianAgent` into the chat orchestrator.

---

## 🔗 Two-Way Knowledge Sync

Bidirectional sync between the Knowledge MCP server and an Obsidian vault. Notes, links, and code snippets stored in DailyWork become markdown files with YAML frontmatter in the vault, and edits made in Obsidian flow back into the knowledge database.

**Example vault file:**

```markdown
---
type: snippet
language: csharp
tags: [aspire, dependency-injection]
created: 2026-04-06
source: dailywork
---

# Register Keyed Services in Aspire

​```csharp
builder.Services.AddKeyedSingleton<IMyService, MyService>("my-key");
​```
```

---

## 📅 Daily Focus → Daily Notes

Append the daily focus ranked list (with scores) into Obsidian's daily note each morning. Over time this creates a searchable log of what was prioritized and when.

**Example daily note section:**

```markdown
## Daily Focus (2026-04-06)

1. 🔴 **Finish API auth refactor** — Score: 92 (due today, high priority, linked to Q2 goals)
2. 🟡 **Review PR #48** — Score: 74 (in progress, medium priority)
3. 🟢 **Update dependency versions** — Score: 51 (no deadline, low priority)
```

---

## 🧠 Vault as Agent Context

Read from the Obsidian vault to give AI agents richer context. The chat agent could search personal notes, meeting notes, and project documentation when answering questions — essentially using the vault as a RAG source.

**Use cases:**

- "What did I write about the caching strategy last week?" → searches vault notes
- "Summarize my meeting notes from the planning session" → reads specific note
- Coaching agent references past reflections and decisions when giving advice

---

## 🕸️ Goal Graph via Backlinks

Export goals and their linked todos as interlinked markdown files. Obsidian's graph view then visualizes goal→todo→tag relationships naturally, and Dataview queries can build custom dashboards.

**Example goal file:**

```markdown
---
type: goal
priority: high
status: in-progress
progress: 65
target_date: 2026-05-01
tags: [q2, api, performance]
---

# Improve API Response Times

## Linked Todos

- [[Todo - Add response caching]]
- [[Todo - Optimize EF Core queries]]
- [[Todo - Profile hot endpoints]]
```

**Example Dataview query:**

```dataview
TABLE progress AS "Progress", target_date AS "Target", tags AS "Tags"
FROM "dailywork/goals"
WHERE status = "in-progress"
SORT progress DESC
```

---

## 💬 Conversation Archival

Export AI chat conversations as timestamped vault notes. This creates a searchable history of every coaching session, ideation brainstorm, or research thread.

**Example conversation note:**

```markdown
---
type: conversation
agent: knowledge
messages: 12
created: 2026-04-06T14:30:00Z
tags: [research, caching, redis]
---

# Caching Strategy Research (Apr 6)

**Me:** What are the tradeoffs between in-memory and distributed caching for our API?

**Agent:** For the DailyWork API, there are a few factors to consider...
```

---

## 📊 Weekly Review Notebooks

When the weekly review/coaching feature lands, generate a structured weekly review note in the vault — accomplishments, stalled goals, focus score trends, and completed todos.

**Example weekly review:**

```markdown
---
type: weekly-review
week: 2026-W15
todos_completed: 14
goals_progressed: 3
focus_score_avg: 78
---

# Weekly Review — Week 15, 2026

## Accomplishments
- Completed 14 todos across 3 goals
- Shipped API auth refactor (Goal: Q2 Security Hardening → 100%)
- Knowledge base: saved 8 new snippets, 3 links

## Stalled Goals
- ⚠️ **Improve API Response Times** — no progress in 5 days

## Focus Score Trend
Mon: 82 → Tue: 91 → Wed: 74 → Thu: 68 → Fri: 75 (avg: 78)

## Next Week Suggestions
- Prioritize the stalled performance goal
- 3 todos due by Wednesday
```

---

## 🏷️ Unified Tag Taxonomy

Sync the tag system across both platforms. Tags created in DailyWork appear in Obsidian and vice versa, giving one consistent taxonomy for organizing everything.

**Sync behavior:**

- New tag in DailyWork → created in Obsidian vault (tag page or frontmatter)
- Tag renamed in either system → propagated to the other
- Tag usage counts reflect combined usage across both systems
- Browse-by-tag in DailyWork includes Obsidian-only notes in results

---

## 📝 Standup Notes

The planned daily standup mode pulls yesterday's completed todos and today's focus items, then scaffolds a standup note in the vault — ready for copy/paste into Teams or Slack.

**Example standup note:**

```markdown
---
type: standup
date: 2026-04-06
---

# Standup — April 6, 2026

## Yesterday
- ✅ Finished API auth refactor
- ✅ Reviewed PR #48
- ✅ Updated NuGet dependencies

## Today
- 🎯 Profile API hot endpoints (focus score: 92)
- 🎯 Start caching layer design (focus score: 74)

## Blockers
- None
```

---

## Implementation Notes

### Approach: `DailyWork.Mcp.Obsidian`

A new MCP server following the existing pattern:

```
src/DailyWork.Mcp.Obsidian/
├── Program.cs              # MCP server setup with HTTP transport
├── Tools/
│   ├── VaultReadTools.cs   # Search, read, list vault notes
│   ├── VaultWriteTools.cs  # Create, update, append notes
│   ├── SyncTools.cs        # Knowledge/goal/todo sync operations
│   └── ExportTools.cs      # Daily focus, standup, weekly review export
└── Services/
    ├── VaultService.cs     # File system operations on the vault
    └── FrontmatterParser.cs # YAML frontmatter read/write
```

### Configuration

Vault path configured via Aspire AppHost or `appsettings.json`:

```json
{
  "Obsidian": {
    "VaultPath": "~/Documents/MyVault",
    "DailyWorkFolder": "dailywork",
    "DailyNotesFolder": "daily",
    "SyncEnabled": true
  }
}
```

### Existing Resources

- [Community Obsidian MCP server](https://github.com/MarkusPfworker/obsidian-mcp) — potential starting point or external integration
- Obsidian's [Local REST API plugin](https://github.com/coddingtonbear/obsidian-local-rest-api) — alternative to direct file system access
