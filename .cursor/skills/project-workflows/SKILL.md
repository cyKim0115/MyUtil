---
name: project-workflows
description: MyUtil repo workflow index for Korean commits and editor-tool documentation. Use when committing changes or writing editor tool guides in this repository.
disable-model-invocation: true
---

# Project Workflows

Workflow index for `MyUtil` (CyKimExtension utility library).

## Available skills

- `korean-git-commit` — Korean commit message format
- `editor-tool-doc-writing` — Markdown docs for Unity editor tools

## Routing

- Commit message → `.cursor/rules/korean-git-commit.mdc` + `korean-git-commit`
- Editor tool guide → `editor-tool-doc-writing`
- Unity Editor automation / no CLI batchmode → `.cursor/rules/unity-editor-agent-workflow.mdc`
