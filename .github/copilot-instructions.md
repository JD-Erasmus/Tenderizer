# Copilot Instructions

## General Guidelines
- Mark configuration/templates tasks complete in `feature/tenderworkflow/TASKS.md`.
- Always reference the `Docs` folder; do not generate or edit EF Core migration files.
- For document uploads, keep the outer request generic but require strongly typed per-route metadata contracts; routes should not use untyped metadata dictionaries directly.
- For this repository phase, controlled breaking changes are acceptable to accelerate model alignment; strict backward compatibility is not required during refactor.

## Code Style
- Use specific formatting rules.
- Follow naming conventions.
- Prefer minimal JavaScript; if JavaScript is needed, it should live in a feature-based or model-based file rather than inline.