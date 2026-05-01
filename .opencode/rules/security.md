# Iris Security Rules

## Secrets

Never read, print, store, or modify:

- `.env`
- `.env.*`
- production configs;
- private keys;
- API tokens;
- credentials;
- user secrets;
- real customer data.

Allowed:

- `.env.example`
- redacted sample configs
- documentation examples

## Destructive Commands

Never run without explicit approval:

- `git push`
- `git clean`
- `git reset --hard`
- destructive file removal
- `docker system prune`
- destructive database commands

## Data Handling

Do not copy secrets into docs, tests, logs, prompts, memory, or review output.

Use placeholders:

```text
<REDACTED>
<API_KEY>
<CONNECTION_STRING>
```

## Supply Chain

Do not add packages casually.

Before adding a dependency, verify existing alternatives, ownership project, central package management, and approved plan scope.
