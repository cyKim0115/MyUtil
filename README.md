# MyUtil

Unity 개발을 위한 유틸리티 및 확장 기능 모음입니다.

## 📋 개요

이 프로젝트는 Unity 개발 시 자주 사용하는 유틸리티 클래스, 커스텀 속성(Attribute), 에디터 도구들을 모아놓은 라이브러리입니다.

## ✨ 주요 기능

### 🔧 유틸리티 클래스

#### ChildUtil
Transform의 자식 객체를 탐색하는 확장 메서드를 제공합니다.

```csharp
Transform child = parent.FindChildByName("TargetName");
List<Transform> allChildren = new List<Transform>();
parent.GetTransformIncludeAllChild(ref allChildren);
```

#### PlatformUtil
플랫폼 및 빌드 타입을 확인합니다.

```csharp
if (PlatformUtil.IsEditor()) { /* 에디터 */ }
if (PlatformUtil.IsDev()) { /* DEV 빌드 */ }
if (PlatformUtil.IsReal()) { /* MARKET_BUILD */ }
string platform = PlatformUtil.GetPlatformType(); // Editor / iOS / Android
```

#### Util
숫자 단위 포맷, 시간 포맷, 확률 시뮬레이션을 제공합니다.

```csharp
string text = 1500f.FormatWithUnits(); // "1.5a"
double value = "1.5a".GetUnitValue();
string time = 3661L.FormatTime(); // "1h 1m"
bool hit = 30f.ProbabilitySimulate_Percent();
```

#### LanguageUtil
PlayerPrefs 기반 언어 코드를 저장/로드합니다.

#### GameStateUtil
플레이 모드 종료/앱 종료 중 안전 접근 여부를 확인합니다.

```csharp
if (GameStateUtil.IsSafeToAccess) { /* 안전한 접근 */ }
```

#### UILayoutUtil
UI 레이아웃을 하위부터 순차적으로 리빌드합니다.

```csharp
transform.RebuildLayoutsFromBottom();
```

#### ProgressBarUtil / ScrollRectUtil
Image fillAmount / ScrollRect 정규화 위치 애니메이션 (LitMotion + UniTask).

```csharp
img.SetProgressWithAnimation(0.8f, 0.5f);
await scrollRect.ScrollToBottomAsync(0.3f, token);
```


### 🎨 커스텀 속성 (Attributes)

#### ReadOnlyProperty
Inspector에서 필드를 읽기 전용으로 만듭니다.

#### ShowIfAttribute
조건에 따라 필드를 표시하거나 비활성화합니다.

### 📦 데이터 구조

- **SerializableDictionary**: Inspector 직렬화 가능한 Dictionary
- **DoubleColor**: 상/하단 색상 구조체
- **LabelDictionary**: 라벨이 있는 Dictionary (`label` 기본값 `-1`)
- **ProbabilityDictionary**: 확률 Dictionary

### 🛠️ 에디터 도구

| 메뉴 | 설명 |
|------|------|
| Tools/Prefab/Favorite Prefab | 즐겨찾기 프리팹 (`Ctrl/Cmd+Shift+F`) |
| Tools/Prefab/Open Selected Prefab | 선택 프리팹 편집 (`Alt+E`) |
| Tools/Prefab/Remove Missing Scripts | Missing Script 제거 |
| Tools/Prefab/Random Prefab Scatter | 씬 오브젝트 위치에 랜덤 프리팹 배치 |
| GameObject/Custom Create GameObject | Empty/UI 생성 (`Ctrl/Cmd+Shift+N`) |
| Tools/Data/Data Path Open | persistentDataPath 탐색기 열기 |
| **F12** (Inspector Component Shortcut) | 선택 객체가 TMP면 텍스트 입력 포커스, Image면 Source Image 선택 창 |

## 📦 요구사항

- Unity 2022.3 이상
- URP, Input System, TextMeshPro (`com.unity.ugui`)

### 외부 패키지

`Window > Package Manager > + > Add package from git URL…`

#### LitMotion
- https://github.com/AnnulusGames/LitMotion
```text
https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion
```

#### UniTask
- https://github.com/Cysharp/UniTask
```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

#### Cursor IDE Support
- https://github.com/boxqkrtm/com.unity.ide.cursor
```text
https://github.com/boxqkrtm/com.unity.ide.cursor.git
```

#### Unity MCP
- https://github.com/CoplayDev/unity-mcp
```text
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
```

#### NuGetForUnity
- https://github.com/GlitchEnzo/NuGetForUnity
```text
https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
```

#### MemoryPack
- https://github.com/Cysharp/MemoryPack
1. 위 NuGetForUnity 설치
2. `NuGet > Manage NuGet Packages`에서 `MemoryPack` 검색 후 설치

## 🚀 설치 방법

1. `CyKimExtension` 폴더를 Unity 프로젝트 `Assets`에 복사
2. 필요 패키지를 위에 맞게 추가
3. Unity에서 컴파일 확인

## 🤖 Cursor Rules / Skills

Agent용 프로젝트 규칙·스킬은 `.cursor/`에 있습니다 (게임 도메인·Blender 전용 자산은 제외).

| 경로 | 내용 |
|------|------|
| `.cursor/rules/` | `.meta` 금지, Editor batchmode 금지, Agent 전용 MenuItem, 한국어 커밋, C# 컨벤션, 라이브러리 개요 |
| `.cursor/skills/project-workflows/` | 한국어 커밋, 에디터 도구 문서, Agent 전용 Editor 도구, 소스 프로젝트 `최신화` sync |

다른 Unity 프로젝트 → 이 라이브러리 동기화는 채팅에서 `최신화`만 입력하면 됩니다.  
경로 설정: 루트 `.env` (템플릿 `.env.example`). 정책:  
`.cursor/skills/project-workflows/sync-from-source/sync-manifest.md`

## 📄 라이선스

이 프로젝트는 개인 사용 목적으로 제작되었습니다.
