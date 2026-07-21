---
name: sync-from-source
description: >-
  Sync reusable Unity utils, Cursor rules, and skills from a configured source
  Unity project into this util library. Use when the user says 최신화, sync,
  동기화, or asks to update CyKimExtension / .cursor assets from another project.
---

# Sync From Source

Configured **source** project에서 **범용** 유틸·룰·스킬만 이 저장소로 가져온다. 게임/Blender 전용은 제외.

## Config (required)

1. Ensure repo-root `.env` exists (copy from `.env.example` if missing).
2. Read variables (never hardcode machine paths or other project names in skills/manifest):

| Variable | Meaning |
|----------|---------|
| `SYNC_SOURCE_ROOT` | Absolute path to source Unity project root |
| `SYNC_SOURCE_SCRIPTS_REL` | Relative path to util scripts folder under source |
| `SYNC_SOURCE_CURSOR_REL` | Relative path to source `.cursor` (default `.cursor`) |
| `SYNC_SOURCE_EXTRA_DRAWER_REL` | Optional extra path for dictionary drawer |

3. If `SYNC_SOURCE_ROOT` or `SYNC_SOURCE_SCRIPTS_REL` is empty → **stop** and ask the user to fill `.env`.
4. Confirm `SYNC_SOURCE_ROOT` exists on disk before diffing.
5. Destination is always **this workspace** (`Assets/CyKimExtension/`, `.cursor/`).

## Before starting

1. Read `sync-manifest.md` in this skill folder.
2. Load `.env` from the repository root.
3. Do not write other product/repo names into commits, skills, or summaries beyond generic terms (“source project”).

## Procedure

### 1. Diff candidates

- Compare each **scripts** / **rules** / **skills** entry in the manifest (source → dest).
- Scan source util tree and source `.cursor/rules|skills` for **new** files not listed.
  - New file → apply include heuristics; if unsure, list under “후보” and do not auto-add.

### 2. Apply updates

1. Check exclude patterns and do-not-overwrite policies first.
2. If source copy is game-coupled (project-specific `Assets/...` paths, managers, popups) → skip or port a **generalized** version.
3. Prefer this library when already ahead (e.g. LitMotion vs PrimeTween).
4. Never create or edit `.meta` files.
5. Do not copy Blender skills/rules or source-only domain workflows.

### 3. Manifest maintenance

- Newly accepted portable assets → add to `sync-manifest.md` in the same change.
- Skipped one-offs → note in the summary.

### 4. Finish

1. Summarize: updated / added / skipped / candidates.
2. Update `README.md` only if documented surface changed.
3. **Do not commit** unless the user asks (`korean-git-commit`).

## User prompt shortcuts

- `최신화`
- `동기화` / `sync`
- `유틸 가져와`

## Out of scope

- Full `unity-skills` tree, `.agents/` mirror, spreadsheet loaders, localization, user-data, island/voxel, toon material, Blender
- Unity `ProjectSettings` / version upgrade noise
- Pushing to remote
