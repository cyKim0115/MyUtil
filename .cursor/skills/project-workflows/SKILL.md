---
name: project-workflows
description: Workflow index for Korean commits, editor-tool docs, agent-only editor tools, Unity Recorder, particle effect controllers, and source-project sync. Use when committing, writing editor tool guides, adding Agent-only MenuItem tools, recording Game View, grouping ParticleSystem hierarchies, or syncing/최신화 from a configured source Unity project. Webhook reporting lives in the global skill `webhook-report`.
disable-model-invocation: true
---

# Project Workflows

Workflow index for this util library (`CyKimExtension`).

## Available skills

- `korean-git-commit` — Korean commit message format
- `editor-tool-doc-writing` — Markdown docs for Unity editor tools
- `agent-editor-tools` — Agent-only Editor tools: disable MenuItem, call via execute_code
- `unity-recorder` — Game View MP4 / PNG sequence via `AgentUnityRecorder`
- `sync-from-source` — Sync portable utils/rules/skills from `.env`-configured source (`최신화`)
- `particle-effect-controller` — Root ParticleSystem (renderer off) as Play/Stop controller for child emitters

## Routing

- Commit message → `.cursor/rules/korean-git-commit.mdc` + `korean-git-commit`
- Editor tool guide → `editor-tool-doc-writing`
- Unity Editor automation / no CLI batchmode → `.cursor/rules/unity-editor-agent-workflow.mdc`
- Agent-only one-shot Editor tools → `.cursor/rules/unity-agent-editor-tools.mdc` + `agent-editor-tools`
- Webhook report (text / screenshot / recording, media choice included) → 전역 스킬 `webhook-report`
- Game View movie / image sequence → `unity-recorder`
- Screenshots folder cleanup → 캡처한 쪽에서 `.png`·`.png.meta`를 함께 지운다 (전용 스킬 없음)
- `최신화` / sync → `sync-from-source` (+ `sync-manifest.md`, repo-root `.env`)
- Multi-emitter VFX / parent Stop drives children / renderer-off root ParticleSystem → `particle-effect-controller`
