import type { Plugin } from "@opencode-ai/plugin";

const DOTNET_FILE_PATTERNS: RegExp[] = [
  /\.cs$/i,
  /\.csproj$/i,
  /\.sln$/i,
  /\.slnx$/i,
  /Directory\.Build\.props$/i,
  /Directory\.Build\.targets$/i,
  /Directory\.Packages\.props$/i,
  /global\.json$/i,
  /\.editorconfig$/i,
];

const VERIFY_COMMAND_HINT = [
  "dotnet build",
  "dotnet test",
  "dotnet format --verify-no-changes",
];

function isDotnetRelevantPath(path: string): boolean {
  return DOTNET_FILE_PATTERNS.some((pattern) => pattern.test(path));
}

function formatPathList(paths: string[], limit = 12): string {
  const visible = paths.slice(0, limit);
  const hiddenCount = paths.length - visible.length;

  const lines = visible.map((path) => `- \`${path}\``);

  if (hiddenCount > 0) {
    lines.push(`- ...and ${hiddenCount} more file(s)`);
  }

  return lines.join("\n");
}

export const DotnetVerifyReminderPlugin: Plugin = async ({ client }) => {
  const changedDotnetFiles = new Set<string>();

  await client.app.log({
    body: {
      service: "dotnet-verify-reminder",
      level: "info",
      message: "Dotnet verification reminder plugin loaded",
    },
  });

  return {
    "file.edited": async (input) => {
      const path =
        typeof input.path === "string"
          ? input.path
          : typeof input.filePath === "string"
            ? input.filePath
            : undefined;

      if (!path || !isDotnetRelevantPath(path)) {
        return;
      }

      changedDotnetFiles.add(path);

      await client.app.log({
        body: {
          service: "dotnet-verify-reminder",
          level: "info",
          message: `Marked .NET-relevant file as changed: ${path}`,
        },
      });
    },

    "tool.execute.after": async (input, output) => {
      const tool = input.tool;
      const args = (output.args ?? {}) as Record<string, unknown>;

      if (tool !== "edit" && tool !== "write" && tool !== "apply_patch") {
        return;
      }

      if (tool === "apply_patch") {
        const patchText =
          typeof args.patchText === "string" ? args.patchText : "";

        for (const line of patchText.split(/\r?\n/)) {
          const match = line.match(
            /^\*\*\* (?:Add File|Update File|Delete File|Move to):\s+(.+)\s*$/i,
          );

          const path = match?.[1]?.trim();

          if (path && isDotnetRelevantPath(path)) {
            changedDotnetFiles.add(path);
          }
        }

        return;
      }

      const path =
        typeof args.filePath === "string"
          ? args.filePath
          : typeof args.path === "string"
            ? args.path
            : undefined;

      if (path && isDotnetRelevantPath(path)) {
        changedDotnetFiles.add(path);
      }
    },

    "session.idle": async () => {
      if (changedDotnetFiles.size === 0) {
        return;
      }

      const files = [...changedDotnetFiles].sort();

      await client.app.log({
        body: {
          service: "dotnet-verify-reminder",
          level: "warn",
          message: [
            ".NET-relevant files were changed.",
            "",
            "Changed files:",
            formatPathList(files),
            "",
            "Before claiming completion, run verification:",
            ...VERIFY_COMMAND_HINT.map((command) => `- ${command}`),
            "",
            "Preferred command: /verify",
          ].join("\n"),
        },
      });

      changedDotnetFiles.clear();
    },
  };
};