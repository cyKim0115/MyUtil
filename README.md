# MyUtil

Unity 개발을 위한 유틸리티 및 확장 기능 모음입니다.

📖 문서: https://cykim.gitbook.io/myutil/ *(Git Sync 연결 후 페이지가 채워집니다)*

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
string rate = 8.6421.FormatDecimalWithUnits(); // "8.64" (1000 이상은 FormatWithUnits)
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
UI 레이아웃을 하위부터 순차적으로 리빌드하고, 줄 높이를 고정합니다.

```csharp
transform.RebuildLayoutsFromBottom();
row.SetPreferredHeight(96f); // LayoutElement preferredHeight + flexibleHeight 0
```

`SetPreferredHeight`는 `flexibleHeight`를 0으로 같이 눌러 둡니다. `childForceExpandHeight`가 켜진 HorizontalLayoutGroup은 자기 flexibleHeight를 1로 보고해서, 그냥 두면 그 줄이 부모의 남는 세로 공간을 전부 가져갑니다.

#### UguiFactory
프리팹 없이 코드로 uGUI를 세우는 기본 부품입니다(캔버스·EventSystem·Rect·Image·Text·Button·Layout). 개발용 하네스나 런타임에 개수가 정해지는 목록에 씁니다. 글자는 레거시 `Text`이고 폰트는 `UguiFactory.DefaultFont`(기본 내장 LegacyRuntime.ttf)를 바꾸거나 인자로 넘깁니다.

```csharp
var canvas = UguiFactory.CreateCanvas("Dev Canvas", new Vector2(1080f, 1920f));
UguiFactory.EnsureEventSystem(); // Input System이면 InputSystemUIInputModule
var column = UguiFactory.CreateVerticalGroup("Column", canvas.transform, 12f);
UguiFactory.CreateButton("시작", column.transform, Color.gray, Color.white, 36, OnClickStart);
```

#### SafeArea
`Screen.safeArea`에 맞춰 RectTransform 앵커를 조정하는 컴포넌트입니다. Canvas 바로 아래 전체 스트레치 패널에 붙입니다. `_maxAspect`(가로/세로 비율 상한)를 주면 초광폭 화면에서 UI 폭을 그 비율로 가운데에 모읍니다.

#### PerfStatsLogger
개발 빌드에서 1초마다 FPS·CPU/GPU 시간·드로우콜·메모리·프레임 시간 분포를 `[PerfStats]` 한 줄 로그(Android는 logcat)로 남깁니다. Scripting Define Symbols에 `CYKIM_PERF_STATS`를 넣은 개발 빌드에서만 컴파일되고 스스로 만들어집니다. GPU·스레드 시간은 Player Settings의 Frame Timing Stats를 켜야 0이 아닙니다.

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
| Tools/Prefab/Favorite Prefab | 즐겨찾기 프리팹 다중 세트·드래그 재정렬 (`Ctrl/Cmd+Shift+F`) |
| Tools/Prefab/Open Selected Prefab | 선택 프리팹 편집 (`Alt+E`) |
| Tools/Prefab/Remove Missing Scripts | Missing Script 제거 |
| Tools/Prefab/Random Prefab Scatter | 씬 오브젝트 위치에 랜덤 프리팹 배치 |
| GameObject/Custom Create GameObject | Empty/UI 생성 (`Ctrl/Cmd+Shift+N`) |
| Tools/Data/Data Path Open | persistentDataPath 탐색기 열기 |
| Tools/Animation/Clip Path Remap | 하이어라키 이름을 바꾼 뒤 여러 AnimationClip의 바인딩 경로를 한 번에 치환 (서브모듈 `Editor/Animation`, 상세는 [서브모듈 README](Assets/CyKimExtension/Editor/Animation/README.md)) |
| **F12** (Inspector Component Shortcut) | 선택 객체가 TMP면 텍스트 입력 포커스, Image면 Source Image 선택 창 |
| (자동) `EditorPlayModeRunInBackground` | 에디터 Play 중에는 `Application.runInBackground`를 켜서 포커스가 없어도 시뮬레이션이 돈다. 에디터에서는 이 값이 Player Settings와 같은 칸이라 Edit 복귀 때 원래 값으로 되돌린다 |
| (자동) `MaterialKeywordRepair` | 셰이더 재임포트 중 키워드가 빠진 채 남은 머티리얼(알파 클립 둘레가 검게 나옴 등)을 셰이더 임포트 뒤·Play 직전에 디스크에서 다시 불러온다. 파일은 안 바뀐다. 수동: `MaterialKeywordRepair.Repair()` |
| (자동) `RendererFeatureActiveStateGuard` | Play 중 `SetActive`로 꺼진 URP 렌더러 피처를 Edit 복귀·빌드 직전에 디스크 값으로 되돌린다. 꺼진 채 빌드되는 사고를 막는다(Force Text 직렬화 전제). 수동: `RendererFeatureActiveStateGuard.RestoreFromDisk()` |

#### Agent 에디터 가드 (`AgentEditorDialogGuard`)

MCP 작업 전 dirty 씬·프리팹 Stage를 저장/폐기해 Save·Discard·Reload 모달을 예방한다. Agent 전용(메뉴 비활성).

```csharp
AgentEditorDialogGuard.PrepareSave();    // 저장 후 Stage 닫기
AgentEditorDialogGuard.PrepareDiscard(); // 변경 폐기 후 Stage 닫기

// Stage를 닫지 않고 유지해야 할 때 (Forest Window 리빌드·라이브 프리뷰 등)
AgentEditorDialogGuard.SaveOpenPrefabStageWithoutClosing();
AgentEditorDialogGuard.SaveAssetsRespectingOpenPrefabStage(); // Stage 열려있으면 Refresh 생략
```

#### Agent 씬 리로드 모달 해제 (`AgentSceneDirtyBeacon` + `unity-modal.py`)

git pull·브랜치 전환으로 열린 씬 파일이 바뀌면 Unity가 「The open scene(s) have been modified externally」(Reload / Ignore) 네이티브 모달을 띄운다. 이 모달은 C#으로 막을 수 없고, MCP 서버도 막힌 메인 스레드 안에서 돌아 MCP 호출이 전부 타임아웃된다. 바깥 파이썬 프로세스가 Win32 메시지(`BM_CLICK`)로 버튼을 누른다. 포커스·마우스는 쓰지 않는다.

- `AgentSceneDirtyBeacon` (`[InitializeOnLoad]`, 호출 없음): 열린 씬·Prefab Stage의 dirty 여부와 Play 여부를 `Temp/AgentSceneDirty.json`에 적는다. 메인 에디터에서만 돈다(에셋 임포트 워커 제외). 스크립트 경로도 함께 적어, 브랜치 전환으로 스크립트가 빠지면 도구가 낡은 기록을 믿지 않는다.
- `Editor/Agent/Tools~/unity-modal.py` (Windows, 파이썬 표준 라이브러리만): `Tools~`는 Unity가 임포트하지 않는다.

```bash
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py            # 모달·진행 창 목록 + 씬 dirty 상태
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py --reload   # 씬 리로드 모달이면 Reload (열린 씬이 모두 저장 상태일 때만)
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py --wait 60 --reload
```

- 종료 코드: 0 = 모달 없음·처리함, 2 = 사람 판단 필요(dirty·Play 중·상태 모름), 1 = 오류
- **Ignore는 누르지 않는다.** 메모리의 옛 씬을 나중에 저장하면 git으로 받은 변경을 덮어쓴다. Reload는 저장 안 된 편집만 잃는다.
- 컴파일·임포트 진행 창(`Hold on...`, `Reloading Domain`)은 진행 막대가 있어 모달로 치지 않고 기다린다.
- 에이전트 지침 한 줄 예: 「MCP 호출이 전부 타임아웃이면 `unity-modal.py`로 모달부터 확인하고, 씬 리로드면 `--reload`, 종료 코드 2면 사용자에게 묻는다.」

#### Unity MCP 연결 점검 (`unity-mcp-endpoint.py`)

UnityMCP 포트는 에디터 재시작·도메인 리로드마다 바뀌므로 문서·설정에 적지 않고 이 도구로 찾는다. `~/.unity-mcp/` 상태 파일에서 이 프로젝트의 포트를 고르고, ai-game-developer를 쓰면 기대 주소(`/p/<핀>`)와 `.mcp.json`·Codex 설정이 맞는지도 본다. 토큰은 출력하지 않는다.

```bash
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-mcp-endpoint.py           # 요약
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-mcp-endpoint.py --check   # 어긋나면 exit 1 (ai-game-developer 설정 필요)
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-mcp-endpoint.py --fix     # Claude 쪽 등록·에이전트 선택 바로잡기
```

#### Agent 웹훅 피드백 (`WebhookFeedback`)

Discord/Slack 웹훅으로 텍스트·스크린샷·짧은 녹화 피드백을 보낸다. URL은 `Secrets/` (gitignore)에 두고, Agent는 MCP `execute_code`로 호출한다.

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SetActiveProvider(WebhookFeedbackProvider.Discord);
WebhookFeedback.SendText("제목", "설명");
WebhookFeedback.Send(new[] { "Assets/Screenshots/a.png" }, "제목", "설명");
WebhookFeedback.SendRecording(@"Recordings/clip.mp4", "제목", "설명");
WebhookFeedback.ClearScreenshotsFolder(); // 전송 후 Assets/Screenshots 정리
```

- Secrets: `discord_webhook_url.txt`, `slack_webhook_url.txt`, `webhook_active_provider.txt` (`Discord` | `Slack` | `Both`)
- 메뉴 `Tools/Agent/Webhook/...`는 Agent 전용(비활성)
- ⚠️ **에이전트 보고에는 쓰지 않는다.** 2026-09-08부터 전역 스킬 `webhook-report`(파이썬 스크립트)가
  매체 판단과 전송을 모두 맡는다. Unity Editor 없이 동작한다. 이 C# API는 사람이 Unity 안에서
  직접 호출할 때만 쓴다.

#### Agent Unity Recorder (`AgentUnityRecorder`)

Play Mode Game View를 MP4 / PNG 시퀀스로 녹화한다. 출력은 프로젝트 루트 `Recordings/` (gitignore). 패키지: `com.unity.recorder`.

```csharp
AgentUnityRecorder.StartMovie(10f);
AgentUnityRecorder.GetStatus();
AgentUnityRecorder.Stop();
AgentUnityRecorder.CaptureStill("title_check"); // 1080×1920 정지컷 → Recordings/title_check.png
```

- Agent 전용(메뉴 비활성). 호출: MCP `execute_code`
- `CaptureStill`은 Game View 창 크기와 무관하게 지정 해상도로 한 장을 찍는다. `ScreenCapture`는 창 크기 그대로라 작은 창에서 잘린다. 파일은 렌더 뒤 쓰이니 다음 호출에서 `GetStatus()`로 확인한다
- 짧은 클립(대략 15초 이하)은 `frameRate: 60f`로 찍는다(헬퍼 기본값 30)
- 기본값과 다른 해상도로 녹화하면 종료 시 Game View 사이즈를 자동 원복한다
- MP4 종료 시 `ffmpeg -c copy -movflags +faststart` 무손실 리먹스를 자동 실행해 웹 플레이어에서 길이가 0:00으로 보이는 문제를 막는다 (ffmpeg가 PATH에 없으면 경고만 남기고 원본 유지)
- 워크플로 스킬: `unity-recorder`

## 📦 요구사항

- Unity 6 (6000.4.12f1에서 개발·확인). `EntityId`·`GetEntityId()`를 써서 Unity 2022.3에서는 컴파일되지 않습니다
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

#### Unity Recorder
- Package Manager: `com.unity.recorder` (AgentUnityRecorder용, 이미 `Packages/manifest.json`에 포함)
- (선택) `ffmpeg` — PATH에 있으면 녹화 종료 후 faststart 리먹스 자동 실행. 없어도 녹화는 정상 동작

#### Cursor IDE Support
- https://github.com/boxqkrtm/com.unity.ide.cursor
```text
https://github.com/boxqkrtm/com.unity.ide.cursor.git
```

#### Unity MCP
- https://github.com/CoplayDev/unity-mcp
```text
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0
```
- 태그나 커밋 SHA로 고정합니다. `#main`처럼 움직이는 참조는 임포트할 때마다 새 에디터 코드가 바로 실행되고, MCP 서버(`mcpforunityserver==<같은 버전>`)와 버전이 어긋날 수 있습니다. 다른 git 패키지도 URL 끝에 `#<태그>`를 붙여 고정하기를 권장합니다.

#### NuGetForUnity
- https://github.com/GlitchEnzo/NuGetForUnity
```text
https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
```

#### MemoryPack
- https://github.com/Cysharp/MemoryPack
1. 위 NuGetForUnity 설치
2. `NuGet > Manage NuGet Packages`에서 `MemoryPack` 검색 후 설치

### 관련 패키지

#### CyKim Scroller
가상화 uGUI ScrollRect(셀 재사용, 가변 셀 크기, 루프·스냅). MyUtil의 `ScrollRectUtil`(일반 ScrollRect 위치 애니메이션)과는 다른 기능이라 별도 UPM 패키지로 둡니다. 의존성은 uGUI뿐입니다.
- https://github.com/cyKim0115/cyKimScroller
```text
https://github.com/cyKim0115/cyKimScroller.git?path=/Packages/com.cykim.scroller#v0.3.0
```

## 🚀 설치 방법

1. 이 저장소를 서브모듈까지 받습니다: `git clone --recurse-submodules` (이미 받았으면 `git submodule update --init`). `Editor/Animation`이 서브모듈입니다
2. `CyKimExtension` 폴더를 Unity 프로젝트 `Assets`에 복사. 서브모듈 안의 `.git` 파일(`Editor/Animation/.git`)은 빼고 복사합니다. 대상 저장소가 그 폴더를 별도 저장소로 착각합니다
3. 필요 패키지를 위에 맞게 추가
4. Unity에서 컴파일 확인

## 🤖 Cursor Rules / Skills

Agent용 프로젝트 규칙·스킬은 `.cursor/`에 있습니다 (게임 도메인·Blender 전용 자산은 제외). Claude Code는 루트 `CLAUDE.md`가 이 룰들을 불러오고 스킬 위치를 안내합니다.

| 경로 | 내용 |
|------|------|
| `.cursor/rules/` | `.meta` 생성·삭제 금지, Editor batchmode 금지·자동화 함정, Agent 전용 MenuItem·MCP 함정, 한국어 커밋, C# 컨벤션, 라이브러리 개요 |
| `.cursor/skills/project-workflows/` | 한국어 커밋, 에디터 도구 문서, Agent 전용 Editor 도구, Unity Recorder(영상·시퀀스·정지컷), 파티클 이펙트 컨트롤러, 소스 프로젝트 `최신화` sync |

다른 Unity 프로젝트 → 이 라이브러리 동기화는 채팅에서 `최신화`만 입력하면 됩니다.  
경로 설정: 루트 `.env` (템플릿 `.env.example`). 정책:  
`.cursor/skills/project-workflows/sync-from-source/sync-manifest.md`

## 📄 라이선스

이 프로젝트는 개인 사용 목적으로 제작되었습니다.
