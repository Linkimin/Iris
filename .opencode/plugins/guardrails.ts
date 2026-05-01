import type { Plugin } from "@opencode-ai/plugin";

type ToolArgs = Record<string, unknown>;

const SENSITIVE_PATH_PATTERNS: RegExp[] = [
  /(^|[/\\])\.env($|[./\\])/i,
  /(^|[/\\])secrets\.json$/i,
  /(^|[/\\])appsettings\.production\.json$/i,
  /(^|[/\\])id_rsa$/i,
  /(^|[/\\])id_ed25519$/i,
  /\.pem$/i,
  /\.key$/i,
  /\.pfx$/i,
  /\.p12$/i,
];

const SENSITIVE_CONTENT_PATTERNS: RegExp[] = [
  /-----BEGIN [A-Z ]*PRIVATE KEY-----/i,
  /\bAKIA[0-9A-Z]{16}\b/,
  /\bghp_[A-Za-z0-9_]{20,}\b/,
  /\bgithub_pat_[A-Za-z0-9_]{20,}\b/,
  /\bsk-[A-Za-z0-9_-]{20,}\b/,
  /\b(api[_-]?key|access[_-]?token|secret|password)\s*[:=]\s*["']?[A-Za-z0-9_\-./+=]{12,}/i,
];

const DESTRUCTIVE_COMMAND_PATTERNS: RegExp[] = [
  /\bgit\s+push\b/i,
  /\bgit\s+clean\b/i,
  /\bgit\s+reset\s+--hard\b/i,
  /\brm\s+-rf\b/i,
  /\brm\s+-fr\b/i,
  /\bdel\s+\/[fsq]/i,
  /\brmdir\s+\/s\b/i,
  /\bRemove-Item\b.*\b-Recurse\b/i,
  /\bRemove-Item\b.*\b-Force\b/i,
  /\bdocker\s+system\s+prune\b/i,
  /\bdocker\s+volume\s+prune\b/i,
  /\bdocker\s+image\s+prune\b/i,
  /\bdocker\s+container\s+prune\b/i,
];

function asString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function normalizePath(path: string): string {
  return path.replaceAll("\\", "/").trim();
}

function isSensitivePath(path: string): boolean {
  const normalized = normalizePath(path);
  return SENSITIVE_PATH_PATTERNS.some((pattern) => pattern.test(normalized));
}

function containsSensitiveContent(text: string): boolean {
  return SENSITIVE_CONTENT_PATTERNS.some((pattern) => pattern.test(text));
}

function isDestructiveCommand(command: string): boolean {
  return DESTRUCTIVE_COMMAND_PATTERNS.some((pattern) => pattern.test(command));
}

function getPathLikeArgs(args: ToolArgs): string[] {
  const candidates = [
    args.filePath,
    args.path,
    args.filename,
    args.file,
    args.cwd,
  ];

  return candidates
    .map(asString)
    .filter((value): value is string => Boolean(value));
}

function extractPatchPaths(patchText: string): string[] {
  const paths: string[] = [];

  for (const line of patchText.split(/\r?\n/)) {
    const match = line.match(
      /^\*\*\* (?:Add File|Update File|Delete File|Move to):\s+(.+)\s*$/i,
    );

    if (match?.[1]) {
      paths.push(match[1].trim());
    }
  }

  return paths;
}

function block(reason: string): never {
  throw new Error(`[guardrails] ${reason}`);
}

export const GuardrailsPlugin: Plugin = async ({ client }) => {
  await client.app.log({
    body: {
      service: "guardrails",
      level: "info",
      message: "Guardrails plugin loaded",
    },
  });

  return {
    "tool.execute.before": async (input, output) => {
      const tool = input.tool;
      const args = (output.args ?? {}) as ToolArgs;

      if (tool === "bash") {
        const command = asString(args.command) ?? asString(args.cmd) ?? "";

        if (isDestructiveCommand(command)) {
          block(`Blocked destructive command: ${command}`);
        }

        if (containsSensitiveContent(command)) {
          block("Blocked shell command containing secret-like content.");
        }

        return;
      }

      if (tool === "read" || tool === "edit" || tool === "write") {
        for (const path of getPathLikeArgs(args)) {
          if (isSensitivePath(path)) {
            block(`Blocked access to sensitive file: ${path}`);
          }
        }

        const content = asString(args.content) ?? asString(args.newString) ?? "";

        if (content && containsSensitiveContent(content)) {
          block(`Blocked ${tool} operation containing secret-like content.`);
        }

        return;
      }

      if (tool === "apply_patch") {
        const patchText = asString(args.patchText) ?? "";

        for (const path of extractPatchPaths(patchText)) {
          if (isSensitivePath(path)) {
            block(`Blocked patch touching sensitive file: ${path}`);
          }
        }

        if (containsSensitiveContent(patchText)) {
          block("Blocked patch containing secret-like content.");
        }

        return;
      }

      if (tool === "grep" || tool === "glob") {
        for (const path of getPathLikeArgs(args)) {
          if (isSensitivePath(path)) {
            block(`Blocked ${tool} operation over sensitive path: ${path}`);
          }
        }
      }
    },
  };
};