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

Note: If `SerializableDictionaryDrawer` lives outside the util Editor folder, use `SYNC_SOURCE_EXTRA_DRAWER_REL` and sync into `Editor/SerializableDictionaryDrawer.cs`.

### Always skip (scripts)

- `SpriteUtil.cs`, `ColorUtil.cs`, `VersionCheckUtil.cs`
- `Editor/UserDataViewerWindow.cs`
- `Editor/IslandBlockPlacementWindow.cs`
- `Editor/VoxelFloor*.cs`
- `Editor/PrefabEditEnvironmentSetup.cs` (project scene paths)
- `Editor/OneMobilePopFontAtlasSetup.cs` (project font path)
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

### This-repo only

- `project-workflows/sync-from-source/` (this skill)
- `project-workflows/SKILL.md` index — update routing when adding skills; do not copy source index wholesale

### Skip

- `blender-community/**`, Blender MCP workflows
- popup / manager-pooling / user-data-schema / building-presentation / spreadsheet agent workflows
- `project-onboarding/**`
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

Documented in root `README.md`. Sync may need LitMotion, UniTask; Cursor IDE / Unity MCP / NuGetForUnity / MemoryPack as docs unless scripts require them.
