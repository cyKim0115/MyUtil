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
| `UILayoutUtil` (`LayoutUtil`) | LayoutGroup/ContentSizeFitter를 하위부터 순차 Rebuild |
| `ProgressBarUtil` | Image fillAmount 애니메이션 (LitMotion + UniTask) |
| `ScrollRectUtil` | ScrollRect 정규화 위치 스크롤 |

```csharp
transform.RebuildLayoutsFromBottom();
img.SetProgressWithAnimation(0.8f, 0.5f);
await scrollRect.ScrollToBottomAsync(0.3f, token);
```

## LanguageUtil

PlayerPrefs 기반 언어 코드를 저장·로드합니다. 로컬라이제이션 전체 시스템이 아니라 **코드 키 유지용** 헬퍼입니다.
