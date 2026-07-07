import * as path from "node:path";
import * as fs from "node:fs/promises";

const LOG_DIR = ".github/hooks/labos2";
const LOG_FILE = "agent_log.txt";

export default async function ({ project, directory }: { project: { worktree?: string }; directory: string }) {
  const worktree = project?.worktree || directory;
  const logPath = path.join(worktree, LOG_DIR, LOG_FILE);

  async function append(data: unknown) {
    try {
      const entry = JSON.stringify(data);
      const dir = path.dirname(logPath);
      await fs.mkdir(dir, { recursive: true });
      await fs.appendFile(logPath, entry + "\n", "utf-8");
    } catch {
      // fail silently — logging must not break the tool
    }
  }

  return {
    event: async ({ event }: { event: { id?: string; type?: string; properties?: Record<string, unknown> } }) => {
      await append({
        session_id: (event.properties as Record<string,unknown> | null)?.sessionID,
        cwd: worktree,
        event_id: event.id,
        event_type: event.type,
        properties: event.properties,
      });
    },

    "tool.execute.before": async (input: Record<string, unknown>, output: Record<string, unknown>) => {
      await append({
        session_id: (input as Record<string,unknown>)?.sessionID,
        cwd: worktree,
        hook_event_name: "PreToolUse",
        tool_name: input?.tool,
        tool_input: output,
        call_id: (input as Record<string,unknown>)?.callID,
      });
    },
  };
};
