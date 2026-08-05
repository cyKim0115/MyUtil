---
name: project-workflows
description: Workflow index for Korean commits, editor-tool docs, agent-only editor tools, webhook feedback/media, Unity Recorder, screenshot cleanup, and source-project sync. Use when committing, writing editor tool guides, adding Agent-only MenuItem tools, sending webhook feedback, choosing report media, recording Game View, clearing Screenshots, or syncing/최신화 from a configured source Unity project.
disable-model-invocation: true
---

# Project Workflows

Workflow index for this util library (`CyKimExtension`).

## Available skills

- `korean-git-commit` — Korean commit message format
- `editor-tool-doc-writing` — Markdown docs for Unity editor tools
- `agent-editor-tools` — Agent-only Editor tools: disable MenuItem, call via execute_code
- `webhook-screenshot-feedback` — Discord/Slack webhook hub for screenshot/text/recording feedback
- `webhook-report-media` — Choose text vs screenshot vs short recording for webhook reports
- `unity-recorder` — Game View MP4 / PNG sequence via `AgentUnityRecorder`
- `screenshot-folder-cleanup` — Clear `Assets/Screenshots` after webhook/playtest captures
- `sync-from-source` — Sync portable utils/rules/skills from `.env`-configured source (`최신화`)

## Routing

- Commit message → `.cursor/rules/korean-git-commit.mdc` + `korean-git-commit`
- Editor tool guide → `editor-tool-doc-writing`
- Unity Editor automation / no CLI batchmode → `.cursor/rules/unity-editor-agent-workflow.mdc`
- Agent-only one-shot Editor tools → `.cursor/rules/unity-agent-editor-tools.mdc` + `agent-editor-tools`
- Webhook screenshot / text / recording → `webhook-screenshot-feedback`
- Webhook media choice (text|screenshot|recording) → `webhook-report-media`
- Game View movie / image sequence → `unity-recorder`
- Screenshots folder cleanup → `screenshot-folder-cleanup`
- `최신화` / sync → `sync-from-source` (+ `sync-manifest.md`, repo-root `.env`)
