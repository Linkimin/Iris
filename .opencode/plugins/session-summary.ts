import type { Plugin } from "@opencode-ai/plugin";

const COMPACTION_CONTEXT = `
## Session Continuity Requirements

When compacting or summarizing this session, preserve the following information if available:

1. Current task and active workflow stage:
   - spec
   - design
   - plan
   - implement
   - verify
   - review
   - audit

2. User-approved artifacts:
   - saved specifications
   - saved designs
   - saved implementation plans
   - saved audits

3. Files changed:
   - source files
   - test files
   - documentation files
   - agent memory files
   - OpenCode configuration files

4. Architecture constraints:
   - dependency direction
   - layer ownership
   - forbidden shortcuts
   - public contract constraints

5. Verification evidence:
   - commands run
   - command results
   - failed checks
   - skipped checks
   - verification limits

6. Review and audit results:
   - P0 issues
   - P1 issues
   - P2 backlog
   - final readiness decision

7. Project memory state:
   - PROJECT_LOG updates
   - overview updates
   - local_notes unresolved issues
   - mem_library durable decisions

8. Next safe step:
   - what should be done next
   - what must not be skipped
   - what requires user approval

Do not preserve private reasoning.
Do not preserve secrets, credentials, tokens, keys, production configs, or real customer data.
Do not summarize away architecture boundaries.
Do not claim verification passed unless commands were actually run.
`;

export const SessionSummaryPlugin: Plugin = async ({ client }) => {
  await client.app.log({
    body: {
      service: "session-summary",
      level: "info",
      message: "Session summary plugin loaded",
    },
  });

  return {
    "experimental.session.compacting": async (_input, output) => {
      if (!Array.isArray(output.context)) {
        return;
      }

      output.context.push(COMPACTION_CONTEXT);
    },

    "session.compacted": async () => {
      await client.app.log({
        body: {
          service: "session-summary",
          level: "info",
          message: "Session compaction completed. Continuity requirements were applied if supported by the runtime.",
        },
      });
    },
  };
};