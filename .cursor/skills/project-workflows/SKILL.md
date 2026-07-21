---
name: project-workflows
description: Workflow index for Korean commits, editor-tool docs, agent-only editor tools, and source-project sync. Use when committing, writing editor tool guides, adding Agent-only MenuItem tools, or syncing/최신화 from a configured source Unity project.
disable-model-invocation: true
---

# Project Workflows

Workflow index for this util library (`CyKimExtension`).

## Available skills

- `korean-git-commit` — Korean commit message format
- `editor-tool-doc-writing` — Markdown docs for Unity editor tools
- `agent-editor-tools` — Agent-only Editor tools: disable MenuItem, call via execute_code
- `sync-from-source` — Sync portable utils/rules/skills from `.env`-configured source (`최신화`)

## Routing

- Commit message → `.cursor/rules/korean-git-commit.mdc` + `korean-git-commit`
- Editor tool guide → `editor-tool-doc-writing`
- Unity Editor automation / no CLI batchmode → `.cursor/rules/unity-editor-agent-workflow.mdc`
- Agent-only one-shot Editor tools → `.cursor/rules/unity-agent-editor-tools.mdc` + `agent-editor-tools`
- `최신화` / sync → `sync-from-source` (+ `sync-manifest.md`, repo-root `.env`)
