---
description: Tools 메뉴·단축키, 자동 가드, Agent용 에디터 도구 개요
icon: wrench
---

# 에디터 도구

사용자가 **자주** 쓰는 창·단축키는 활성 `[MenuItem]`으로 둡니다. Agent가 가끔만 돌리는 일회성 도구는 메뉴를 비활성하고 MCP로 호출합니다 → [Agent 전용 Editor 도구](../playbooks/agent-editor-tools.md).

## 메뉴·단축키

| 메뉴 / 키 | 설명 |
|-----------|------|
| `Tools/Prefab/Favorite Prefab` (`Ctrl/Cmd+Shift+F`) | 즐겨찾기 프리팹 다중 세트·드래그 재정렬 |
| `Tools/Prefab/Open Selected Prefab` (`Alt+E`) | 선택 프리팹 편집 모드 |
| `Tools/Prefab/Remove Missing Scripts` | Missing Script 제거 |
| `Tools/Prefab/Random Prefab Scatter` | 씬 오브젝트 위치에 랜덤 프리팹 배치 |
| `GameObject/Custom Create GameObject` (`Ctrl/Cmd+Shift+N`) | Empty/UI 생성 |
| `Tools/Data/Data Path Open` | `persistentDataPath` 탐색기 열기 |
| `Tools/Animation/Clip Path Remap` | 하이어라키 이름을 바꾼 뒤 여러 AnimationClip의 바인딩 경로를 한 번에 치환 (서브모듈 `Editor/Animation`, 상세는 서브모듈 README) |
| **F12** (Inspector Component Shortcut) | TMP면 텍스트 포커스, Image면 Source Image 선택 |

## 자동으로 도는 에디터 가드

메뉴 없이 `[InitializeOnLoad]`로 돕니다. 따로 부를 필요가 없습니다.

| 타입 | 하는 일 |
|------|---------|
| `EditorPlayModeRunInBackground` | 에디터 Play 동안 `Application.runInBackground`를 켜서 창 포커스가 없어도 Play가 진행됩니다. 에디터에서는 이 값이 Player Settings와 같은 칸이라, Edit 복귀 때 원래 값으로 되돌려 `ProjectSettings.asset`에 남기지 않습니다 |
| `AgentSceneDirtyBeacon` | 열린 씬·Prefab Stage의 dirty 상태를 `Temp/AgentSceneDirty.json`에 적습니다. 아래 모달 해제 도구가 읽습니다 |
| `MaterialKeywordRepair` | 셰이더 재임포트 중 키워드가 빠진 채 남은 머티리얼(알파 클립 둘레가 검게 나옴 등)을 셰이더 임포트 뒤·Play 직전에 디스크에서 다시 불러옵니다. 파일은 바뀌지 않습니다. 수동 호출: `MaterialKeywordRepair.Repair()` |
| `RendererFeatureActiveStateGuard` | Play 중 `SetActive`로 꺼진 URP 렌더러 피처를 Edit 복귀·빌드 직전에 디스크 값으로 되돌려, 꺼진 채 빌드되는 사고를 막습니다(Force Text 직렬화 전제). 수동 호출: `RendererFeatureActiveStateGuard.RestoreFromDisk()` |

## 씬 리로드 모달 해제 (`unity-modal.py`)

git pull·브랜치 전환으로 열린 씬 파일이 바뀌면 Unity가 「The open scene(s) have been modified externally」(Reload / Ignore) 네이티브 모달을 띄웁니다. 이 모달은 메인 스레드를 막아 MCP 호출이 전부 타임아웃됩니다. 바깥 파이썬 도구가 Win32 메시지로 버튼을 누릅니다 (Windows, 표준 라이브러리만).

```bash
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py            # 모달·진행 창 목록 + 씬 dirty 상태
python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py --reload   # 열린 씬이 모두 저장 상태일 때만 Reload
```

- 종료 코드 0 = 모달 없음·처리함, 2 = 사람 판단 필요, 1 = 오류
- **Ignore는 누르지 않습니다.** 메모리의 옛 씬을 나중에 저장하면 git으로 받은 변경을 덮어씁니다.
- `Tools~` 폴더는 Unity가 임포트하지 않아 `.meta`가 생기지 않습니다.

## WebhookFeedback

Discord/Slack 웹훅으로 텍스트·스크린샷·짧은 녹화 피드백을 보냅니다. URL은 `Secrets/`(gitignore)에 둡니다.

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SetActiveProvider(WebhookFeedbackProvider.Discord);
WebhookFeedback.SendText("제목", "설명");
WebhookFeedback.Send(new[] { "Assets/Screenshots/a.png" }, "제목", "설명");
WebhookFeedback.SendRecording(@"Recordings/clip.mp4", "제목", "설명");
```

| Secrets 파일 | 내용 |
|--------------|------|
| `discord_webhook_url.txt` | Discord webhook URL |
| `slack_webhook_url.txt` | Slack webhook URL |
| `webhook_active_provider.txt` | `Discord` / `Slack` / `Both` |

메뉴 `Tools/Agent/Webhook/Send Feedback`는 Agent 전용(비활성)입니다.

{% hint style="warning" %}
**에이전트 보고에는 이 C# 허브를 쓰지 않습니다.** 2026-09-08부터 매체 선택과 전송은
모두 전역 스킬 `webhook-report`(파이썬 스크립트) 소관입니다. Unity Editor가 떠 있어야만
동작하던 제약이 없어졌습니다. 이 절의 `WebhookFeedback` API는 **사람이 Unity 안에서
직접 호출할 때만** 쓰는 라이브러리 자산으로 남습니다.
{% endhint %}

## AgentUnityRecorder

Play Mode Game View → MP4 / PNG 시퀀스 / 정지컷 한 장. 출력: 루트 `Recordings/`(gitignore). 패키지 `com.unity.recorder`.

```csharp
AgentUnityRecorder.StartMovie(10f, frameRate: 60f); // 짧은 클립은 60fps
AgentUnityRecorder.GetStatus();
AgentUnityRecorder.CaptureStill("title_check");      // 1080×1920 → Recordings/title_check.png
```

`CaptureStill`은 Game View 창 크기와 무관하게 지정 해상도로 찍습니다. `ScreenCapture`는 창 크기 그대로라 작은 창에서 잘립니다. 파일은 렌더 뒤에 쓰이니 다음 호출에서 `GetStatus()`로 확인합니다.

스킬: `unity-recorder`

## Unity MCP 연결 점검

UnityMCP 포트는 에디터 재시작·도메인 리로드마다 바뀝니다. 포트를 적어 두지 말고 `python Assets/CyKimExtension/Editor/Agent/Tools~/unity-mcp-endpoint.py`로 찾습니다. ai-game-developer를 쓰면 기대 주소와 `.mcp.json`·Codex 설정이 맞는지도 봅니다(`--check`, `--fix`). 토큰은 출력하지 않습니다.

## Editor 자동화 주의

열린 Unity Editor + Unity Skills / MCP로 메뉴·코드를 실행합니다. **같은 프로젝트에 `Unity.exe -batchmode`를 돌리지 마세요** — `Library/` 잠금·임포트 꼬임의 원인입니다.
