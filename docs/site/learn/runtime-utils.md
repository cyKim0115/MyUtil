---
description: PlatformUtil, ChildUtil, Util, GameStateUtil 등 런타임 헬퍼
icon: code
---

# 런타임 유틸

주요 타입은 `Assets/CyKimExtension/` 아래에 있습니다. 네임스페이스는 파일마다 `Util`, `cyKimUnityExtensions` 등이 섞여 있으니 호출 전 using을 확인하세요.

## PlatformUtil

에디터 / DEV / MARKET 빌드와 플랫폼 문자열을 판별합니다.

```csharp
if (PlatformUtil.IsEditor()) { /* 에디터 */ }
if (PlatformUtil.IsDev()) { /* DEV 심볼 */ }
if (PlatformUtil.IsReal()) { /* MARKET_BUILD */ }
string platform = PlatformUtil.GetPlatformType(); // Editor / iOS / Android
```

## ChildUtil

Transform 트리를 이름으로 찾거나, 자기 자신 포함 전 자식을 수집합니다.

```csharp
Transform child = parent.FindChildByName("TargetName");
List<Transform> allChildren = new List<Transform>();
parent.GetTransformIncludeAllChild(ref allChildren);
```

## Util

숫자 단위 포맷, 시간 포맷, 확률 시뮬레이션입니다.

```csharp
string text = 1500f.FormatWithUnits(); // "1.5a"
string rate = 8.6421.FormatDecimalWithUnits(); // "8.64" (1000 이상은 FormatWithUnits)
double value = "1.5a".GetUnitValue();
string time = 3661L.FormatTime(); // "1h 1m"
bool hit = 30f.ProbabilitySimulate_Percent();
```

## GameStateUtil

플레이 모드 종료·앱 종료 중에는 안전하지 않은 접근을 피합니다.

```csharp
if (GameStateUtil.IsSafeToAccess) { /* 안전한 접근 */ }
```

## UI · 애니메이션 헬퍼

| 타입 | 용도 |
|------|------|
| `UILayoutUtil` (`LayoutUtil`) | LayoutGroup/ContentSizeFitter를 하위부터 순차 Rebuild, 줄 높이 고정(`SetPreferredHeight`) |
| `ProgressBarUtil` | Image fillAmount 애니메이션 (LitMotion + UniTask) |
| `ScrollRectUtil` | ScrollRect 정규화 위치 스크롤 |
| `UguiFactory` | 프리팹 없이 코드로 uGUI 세우기 (캔버스·EventSystem·Rect·Image·Text·Button·Layout) |
| `SafeArea` | `Screen.safeArea`에 맞춰 패널 앵커 조정, 초광폭 화면 비율 상한(`_maxAspect`) |

```csharp
transform.RebuildLayoutsFromBottom();
row.SetPreferredHeight(96f);
img.SetProgressWithAnimation(0.8f, 0.5f);
await scrollRect.ScrollToBottomAsync(0.3f, token);
```

{% hint style="info" %}
`SetPreferredHeight`는 `flexibleHeight`를 0으로 같이 눌러 둡니다. `childForceExpandHeight`가 켜진 HorizontalLayoutGroup은 자기 flexibleHeight를 1로 보고해서, 그냥 두면 그 줄이 부모의 남는 세로 공간을 전부 가져갑니다.
{% endhint %}

## UguiFactory

개발용 하네스나 런타임에 개수가 정해지는 목록처럼 프리팹 없이 UI를 세울 때 씁니다. 글자는 레거시 `Text`라 TMP 폰트 에셋 없이 바로 그려집니다. 폰트는 `UguiFactory.DefaultFont`(기본 내장 LegacyRuntime.ttf)를 바꾸거나 인자로 넘깁니다.

```csharp
var canvas = UguiFactory.CreateCanvas("Dev Canvas", new Vector2(1080f, 1920f));
UguiFactory.EnsureEventSystem(); // Input System이면 InputSystemUIInputModule
var column = UguiFactory.CreateVerticalGroup("Column", canvas.transform, 12f);
UguiFactory.CreateButton("시작", column.transform, Color.gray, Color.white, 36, OnClickStart);
```

## PerfStatsLogger

개발 빌드에서 1초마다 FPS·CPU/GPU 시간·드로우콜·메모리·프레임 시간 분포를 `[PerfStats]` 한 줄 로그로 남깁니다(Android는 logcat). Scripting Define Symbols에 `CYKIM_PERF_STATS`를 넣은 개발 빌드에서만 컴파일되고 스스로 만들어집니다. GPU·스레드 시간은 Player Settings의 Frame Timing Stats를 켜야 0이 아닙니다.

## LanguageUtil

PlayerPrefs 기반 언어 코드를 저장·로드합니다. 로컬라이제이션 전체 시스템이 아니라 **코드 키 유지용** 헬퍼입니다.
