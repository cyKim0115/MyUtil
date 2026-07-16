---
name: project-workflows
description: MyUtil repo workflow index for Korean commits, editor-tool docs, and agent-only editor tools. Use when committing, writing editor tool guides, or adding Agent-only MenuItem tools in this repository.
disable-model-invocation: true
---

# Project Workflows

Workflow index for `MyUtil` (CyKimExtension utility library).

## Available skills

- `korean-git-commit` — Korean commit message format
- `editor-tool-doc-writing` — Markdown docs for Unity editor tools
- `agent-editor-tools` — Agent-only Editor tools: disable MenuItem, call via execute_code

## Routing

- Commit message → `.cursor/rules/korean-git-commit.mdc` + `korean-git-commit`
- Editor tool guide → `editor-tool-doc-writing`
- Unity Editor automation / no CLI batchmode → `.cursor/rules/unity-editor-agent-workflow.mdc`
- Agent-only one-shot Editor tools → `.cursor/rules/unity-agent-editor-tools.mdc` + `agent-editor-tools`
