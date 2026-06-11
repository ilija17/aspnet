import * as path from "node:path";
import * as fs from "node:fs/promises";

const LOG_DIR = ".github/hooks/labos2";
const LOG_FILE = "agent_log.txt";

interface EventProperties {
  timestamp?: number;
  sessionID?: string;
  messageID?: string;
  assistantMessageID?: string;
  callID?: string;
  prompt?: { text: string };
  tool?: string;
  input?: Record<string, unknown>;
  structured?: Record<string, unknown>;
  result?: unknown;
  command?: string;
  output?: string;
  exit?: number;
  info?: { id?: string; title?: string };
  delivery?: string;
}

interface Event {
  id: string;
  type: string;
  properties?: EventProperties;
}

const RELEVANT_EVENTS = new Set([
  "session.created",
  "session.next.prompted",
  "session.next.tool.called",
  "session.next.tool.success",
  "session.next.tool.failed",
  "session.next.shell.started",
  "session.next.shell.ended",
  "session.next.step.started",
  "session.next.step.ended",
  "session.next.step.failed",
]);

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
    event: async ({ event }: { event: Event }) => {
      if (!RELEVANT_EVENTS.has(event.type)) return;

      const p = event.properties || {};

      await append({
        event_id: event.id,
        event_type: event.type,
        session_id: p.sessionID,
        cwd: worktree,
        timestamp: p.timestamp,
        message_id: p.messageID,
        assistant_message_id: p.assistantMessageID,
        call_id: p.callID,
        prompt: p.prompt?.text,
        delivery: p.delivery,
        tool_name: p.tool,
        tool_input: p.input,
        tool_output: p.structured,
        tool_result: p.result,
        shell_command: p.command,
        shell_output: p.output?.slice(0, 4096),
        exit_code: p.exit,
      });
    },
  };
}
