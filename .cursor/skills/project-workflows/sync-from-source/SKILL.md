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
| `SYNC_SOURCE_AGENT_REL` | Relative path to the source's canonical rules/skills folder (default `.claude`; the source's `.cursor` is generated from it) |
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
- Scan source util tree and source `<SYNC_SOURCE_AGENT_REL>/rules|skills|tools` for **new** files not listed.
  - New file → apply include heuristics; if unsure, list under “후보” and do not auto-add.
- 소스의 룰·스킬 폴더는 git에서 제외돼 있으니 git 이력이 아니라 디스크 파일과 비교한다.
- 매니페스트의 「MyUtil이 앞선 부분」「MyUtil 단독」 표에 있는 파일은 소스 사본으로 되돌리지 않는다.

### 확장 스캔 (사용자가 요청할 때만)

사용자가 「관여하는 레포 전체」를 보라고 하면 소스 프로젝트 밖으로 넓힌다.

1. 저장소들이 모인 부모 폴더에서 사용자 커밋이 있는 레포만 고른다(git author 기준). 남의 레포 클론·참고용 사본은 뺀다.
2. Unity 프로젝트와 룰·스킬 팩 위주로 같은 Include heuristics를 적용한다. 앱·웹 레포는 목록만 보고 넘긴다.
3. 결과는 바로 반영하지 않고 계획표로 먼저 보여 준다. 다른 프로젝트에서 가져온 자산은 매니페스트 「MyUtil 단독」에 적는다.
4. 다른 사람 소유 레포의 코드는 사용자가 직접 쓴 부분만 후보로 올린다.

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
