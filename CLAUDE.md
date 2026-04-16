# Agent Delegation Rules

## Strict delegation boundary

**The main agent handles:**
- Controllers (`aspnet/Controllers/**`)
- Mock repositories (`aspnet/Repositories/**`)
- Models (`aspnet/Models/**`)
- Program.cs and DI registrations
- Routing configuration

**The `ux-designer` sub-agent handles — exclusively:**
- All Razor views (`aspnet/Views/**`)
- The shared layout (`aspnet/Views/Shared/_Layout.cshtml`)
- CSS (`aspnet/wwwroot/css/**`)
- JavaScript (`aspnet/wwwroot/js/**`)
- Blackjack game (`aspnet/wwwroot/kocka/**`)

## Rule

> Whenever the task involves creating or modifying any file under
> `Views/`, `wwwroot/css/`, `wwwroot/js/`, or `_Layout.cshtml`,
> the main agent MUST delegate that work to the `ux-designer`
> sub-agent using the Agent tool before writing any such file itself.
> The main agent must not write `.cshtml`, `.css`, or front-end `.js`
> files directly.

**CRITICAL:** Every Agent tool call for UI work MUST include `subagent_type: "ux-designer"` explicitly. Omitting this parameter causes the spawn to use a generic agent instead of the defined `ux-designer` agent, which defeats the delegation boundary. A description alone (e.g. "ux-designer role") is not sufficient.

Correct form:
```
Agent({
  subagent_type: "ux-designer",
  description: "...",
  prompt: "..."
})
```

The main agent may read view files for context (to wire up
`@model` types, check tag helper usage, etc.) but must not edit them.

## Log location

All Lab 2 agent activity is captured automatically at:
`.github/hooks/labos2/agent_log.txt`
