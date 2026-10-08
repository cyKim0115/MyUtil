# Sync Manifest — source project → this library

Policy for `sync-from-source`. Edit when accepting new portable assets.

Paths come from repo-root `.env` (see `.env.example`). Do not put absolute paths or other project names in this file.

## Roots

| Role | Resolution |
|------|------------|
| Source repo | `SYNC_SOURCE_ROOT` |
| Source scripts | `SYNC_SOURCE_ROOT` + `SYNC_SOURCE_SCRIPTS_REL` |
| Dest scripts | `Assets/CyKimExtension/` (this repo) |
| Source cursor | `SYNC_SOURCE_ROOT` + `SYNC_SOURCE_CURSOR_REL` |
| Dest cursor | `.cursor/` (this repo) |

## Scripts (runtime / editor)

Map: relative to source util root → relative to `CyKimExtension`.

### Tracked (compare & update)

| Source | Dest |
|--------|------|
| `Extension.cs` | `Extension.cs` |
| `ChildUtil.cs` | `ChildUtil.cs` |
| `PlatformUtil.cs` | `PlatformUtil.cs` |
| `UILayoutUtil.cs` | `UILayoutUtil.cs` |
| `Util.cs` | `Util.cs` |
| `LanguageUtil.cs` | `LanguageUtil.cs` |
| `GameStateUtil.cs` | `GameStateUtil.cs` |
| `CustomDebug.cs` | `CustomDebug.cs` |
| `LabelDictionary.cs` | `LabelDictionary.cs` |
| `ProbabilityDictionary.cs` | `ProbabilityDictionary.cs` |
| `SerailizableDictionary.cs` | `SerailizableDictionary.cs` |
| `DoubleColor.cs` | `DoubleColor.cs` |
| `ProgressBarUtil.cs` | `ProgressBarUtil.cs` |
| `ScrollRectUtil.cs` | `ScrollRectUtil.cs` |
| `Attribute/ShowIfAttribute.cs` | `Attribute/ShowIfAttribute.cs` |
| `Attribute/ShowIfAttributeDrawer.cs` | `Attribute/ShowIfAttributeDrawer.cs` |
| `Attribute/ReadOnlyProperty.cs` | `Attribute/ReadOnlyProperty.cs` |
| `Editor/DataPathUtil.cs` | `Editor/DataPathUtil.cs` |
| `Editor/DoubleColorDrawer.cs` | `Editor/DoubleColorDrawer.cs` |
| `Editor/FixResolutionScale.cs` | `Editor/FixResolutionScale.cs` |
| `Editor/FavoritePrefabWindow.cs` | `Editor/FavoritePrefabWindow.cs` |
| `Editor/PrefabEditModeShortcut.cs` | `Editor/PrefabEditModeShortcut.cs` |
| `Editor/CustomCreateGameObject.cs` | `Editor/CustomCreateGameObject.cs` |
| `Editor/RemoveMissingScriptPrefabWindow.cs` | `Editor/RemoveMissingScriptPrefabWindow.cs` |
| `Editor/RandomPrefabScatterWindow.cs` | `Editor/RandomPrefabScatterWindow.cs` |
| `Editor/InspectorComponentShortcut.cs` | `Editor/InspectorComponentShortcut.cs` |
| `Editor/AgentEditorDialogGuard.cs` | `Editor/AgentEditorDialogGuard.cs` |
| `Editor/AgentSceneDirtyBeacon.cs` | `Editor/AgentSceneDirtyBeacon.cs` |

### Extra tracked (relative to `SYNC_SOURCE_ROOT`, not util scripts root)

| Source | Dest |
|--------|------|
| `Assets/GameResource/Script/Editor/Agent/Webhook/WebhookFeedback.cs` | `Editor/Agent/Webhook/WebhookFeedback.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/WebhookFeedbackSettings.cs` | `Editor/Agent/Webhook/WebhookFeedbackSettings.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/WebhookFeedbackProvider.cs` | `Editor/Agent/Webhook/WebhookFeedbackProvider.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/WebhookFeedbackJson.cs` | `Editor/Agent/Webhook/WebhookFeedbackJson.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/WebhookFeedbackMime.cs` | `Editor/Agent/Webhook/WebhookFeedbackMime.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/IWebhookFeedbackTransport.cs` | `Editor/Agent/Webhook/IWebhookFeedbackTransport.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/DiscordWebhookTransport.cs` | `Editor/Agent/Webhook/DiscordWebhookTransport.cs` |
| `Assets/GameResource/Script/Editor/Agent/Webhook/SlackWebhookTransport.cs` | `Editor/Agent/Webhook/SlackWebhookTransport.cs` |
| `Assets/GameResource/Script/Editor/AgentUnityRecorder.cs` | `Editor/Agent/AgentUnityRecorder.cs` |
| `Assets/GameResource/Script/Editor/EditorPlayModeRunInBackground.cs` | `Editor/EditorPlayModeRunInBackground.cs` |
| `.claude/tools/unity-modal.py` | `Editor/Agent/Tools~/unity-modal.py` |

Note: When syncing `AgentUnityRecorder`, **strip game-coupled APIs** (e.g. map pan / project camera controllers). Keep `StartMovie` / `StartImageSequence` / `Stop` / `GetStatus` only.

Note: `AgentUnityRecorder`의 ffmpeg faststart 리먹스(`ScheduleFaststartRemux` / `TryRemuxFaststart`)는 프로젝트 의존이 없어 추적 대상이다. ffmpeg가 없으면 경고 후 원본을 유지하는 동작을 그대로 지킨다.

Note: `unity-modal.py`는 `AgentSceneDirtyBeacon`과 짝이다(비컨이 `Temp/AgentSceneDirty.json`에 적은 dirty 상태로 씬 리로드 모달의 Reload를 누를지 판단). 둘은 함께 갱신한다. 스크립트는 `--project`, 자기 위치, 현재 폴더 순으로 `Assets`+`ProjectSettings` 폴더를 찾으므로 그대로 복사한다. `Tools~`는 Unity가 임포트하지 않아 `.meta`가 생기지 않는다.

Note: `LanguageUtil.cs`(소스는 프로젝트 PlayerPrefs 래퍼 의존), `Editor/DataPathUtil.cs`(소스는 세이브 데이터 경로 의존)는 이 라이브러리 버전(`PlayerPrefs`·`Application.persistentDataPath`)을 유지한다. 소스의 다른 변경만 골라 반영한다.

Note: If `SerializableDictionaryDrawer` lives outside the util Editor folder, use `SYNC_SOURCE_EXTRA_DRAWER_REL` and sync into `Editor/SerializableDictionaryDrawer.cs`.

### 서브모듈 (sync 대상 아님)

| 경로 | 관리 |
|------|------|
| `Editor/Animation/**` | 독립 저장소 `AnimationClipPathRemap` 서브모듈. 소스 프로젝트에 같은 스크립트가 있어도 **덮어쓰지 않는다** — 수정은 upstream에서 하고 `git submodule update`로 받는다. 관련 문서는 `Doc/AnimationClipPathRemap/`. |

### Always skip (scripts)

- `SpriteUtil.cs`, `ColorUtil.cs`, `VersionCheckUtil.cs`
- `Editor/UserDataViewerWindow.cs`
- `Editor/IslandBlockPlacementWindow.cs`
- `Editor/VoxelFloor*.cs`
- `Editor/PrefabEditEnvironmentSetup.cs` (project scene paths)
- `Editor/OneMobilePopFontAtlasSetup.cs` (project font path)
- `Editor/AnimationClipPathRemapper.cs`, `Editor/AnimationClipPathRemapEditorWindow.cs` (소스 사본. MyUtil은 서브모듈 `Editor/Animation/`을 단일 소스로 쓴다)
- `Editor/Agent/DiscordFeedbackSender.cs` (source-only Discord 하위 호환 래퍼; MyUtil은 `WebhookFeedback`만 유지)
- `Editor/AddressableUnusedEntrySweeper.cs` (소스 테이블 캐시·스크립트 경로와 문자열 주소 규칙에 묶임. 일반화하려면 경로·토큰 수집을 설정으로 빼야 함)
- `Editor/FloorBlockScatterUtility.cs`, 소스 `RandomPrefabScatterWindow`의 바닥 머티리얼 모드 (도메인 바닥 블록 파이프라인. 프리팹 스캐터만 유지)
- 소스 `Editor/Agent/AgentCaptureRun.cs` (원격 워커 잡 계약·프로젝트 씬에 묶임)
- Anything under Blender / Island / Building / character-domain folders outside util

## Cursor rules

### Portable (compare; strip source-project-specific examples)

| Source rule | Dest rule | Notes |
|-------------|-----------|-------|
| `unity-assets.mdc` | `unity-assets.mdc` | Drop toon/Blender material sections |
| `unity-editor-agent-workflow.mdc` | `unity-editor-agent-workflow.mdc` | Drop spreadsheet-only rows |
| `unity-agent-editor-tools.mdc` | `unity-agent-editor-tools.mdc` | Use generic examples |
| `unity-csharp-conventions.mdc` | `unity-csharp-conventions.mdc` | Globs → `Assets/CyKimExtension/**/*.cs`; drop game Manager singletons |
| `korean-git-commit.mdc` | `korean-git-commit.mdc` | Keep this repo’s area labels (`유틸`, `에디터`, …) |

### This-repo only (never overwrite from source)

- `myutil-overview.mdc`

### Skip (domain)

- toon/material project rules, spreadsheet agent rules, localization, managers-pooling, fx-id
- production-building / user-data / project-overview / ui-system game rules
- player-prefs-system, ui-exception-handling (popup/UIManager coupled)
- All Blender-related rules

## Cursor skills

### Portable

| Source | Dest | Notes |
|--------|------|-------|
| `project-workflows/agent-editor-tools/` | same | Generic examples only |
| `project-workflows/editor-tool-doc-writing/` | same | If present and general |
| `project-workflows/korean-git-commit/` | same | Keep this repo’s labels/examples |
| `project-workflows/unity-recorder/` | same | Path → `AgentUnityRecorder`; drop game ForceEnter / map-pan examples |
| `project-workflows/particle-effect-controller/` | same | MyUtil 우선. 소스에 없으면 유지만 하고 삭제하지 않음 |

### This-repo only

- `project-workflows/sync-from-source/` (this skill)
- `project-workflows/SKILL.md` index — update routing when adding skills; do not copy source index wholesale

### Skip

- `blender-community/**`, Blender MCP workflows
- popup / manager-pooling / user-data-schema / building-presentation / spreadsheet agent workflows
- `project-onboarding/**`
- 웹훅 보고 스킬 일체 — 2026-09-08에 전역 스킬 `webhook-report`로 이관됨. 소스에 남아 있어도 동기화하지 않는다
- `project-workflows/agent-playtest/`, `project-workflows/editor-playtest/` — game bootstrap / domain scenarios (B/C)
- Source `Cheat/AgentPlaytest/**`, `Editor/Agent/Playtest/**` — playtest orchestration stays in host games
- `project-workflows/grouped-git-commit/`, `git-commit-on-finish` rule — maintained outside this util library
- Full personal `unity-skills` tree (do not vendor)

## Include heuristics (new util / rule / skill)

**Include** when all true:

- No dependency on source game managers / UserData / Table / Resource / Popup / Island
- No hardcoded project `Assets/...` paths (or can be parameterized cleanly)
- Fits personal util / editor convenience library

**Exclude** when any true:

- Name/path contains Blender, Island, Voxel, spreadsheet loader, Localize, Building presentation, character IP domains
- Requires game SO / Addressables tables / remote config

## Policies

1. **LitMotion wins**: If this library uses LitMotion and source still uses PrimeTween for the same util, do **not** regress to PrimeTween.
2. **API merge**: Prefer additive merges (keep this-library-only APIs like `PlatformUtil.IsReal()` unless source intentionally removed and user confirms).
3. **Shortcut IDs**: Rename source-project shortcut ids to `CyKimExtension/...`.
4. **No `.meta`**: Never create/edit Unity `.meta`.
5. **No auto-commit / push**.
6. **Packages**: If a synced script needs a new package (UniTask, LitMotion, TMP), update `Packages/manifest.json` + README package section.
7. **No source project names** in skills, manifests, README, or commit messages — refer only to “source project” / env vars.

## Packages reference

Documented in root `README.md`. Sync may need LitMotion, UniTask, Unity Recorder (`com.unity.recorder`); Cursor IDE / Unity MCP / NuGetForUnity / MemoryPack as docs unless scripts require them.
