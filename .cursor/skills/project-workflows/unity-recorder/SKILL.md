---
name: unity-recorder
description: >-
  Record Unity Game View (or tagged camera) to MP4 / PNG sequence via Unity Recorder
  and AgentUnityRecorder. Use when the user asks to record, capture video, movie,
  clip, image sequence, or use Unity Recorder from chat.
disable-model-invocation: true
---

# Unity Recorder (Agent)

Play Mode에서 Game View(또는 태그 카메라)를 MP4 / PNG 시퀀스로 녹화한다.
전용 MCP 도구는 없고, 프로젝트 헬퍼 `AgentUnityRecorder`를 MCP `execute_code`로 호출한다.

패키지: `com.unity.recorder`.

## When to use

- “녹화해줘”, “MP4로 뽑아줘”, “이미지 시퀀스”, “Unity Recorder로 …”
- 연출/UI 검증용 짧은 클립이 필요할 때

스크린샷 1장만이면 이 스킬 대신 `manage_camera` screenshot / `webhook-screenshot-feedback`을 쓴다.

## Defaults

| 항목 | 기본값 |
|------|--------|
| 출력 폴더 | 프로젝트 루트 `Recordings/` (gitignored, Assets 밖) |
| 해상도 | 1080×1920 (세로) |
| FPS | 30 |
| 코덱 | MP4 (High). 웹훅 전송 시 Medium 권장 |
| 소스 | Game View (UI 포함) |
| 길이 | `durationSeconds` (≤0이면 수동, `Stop` 필요) |

## Prerequisites

1. Unity Editor 연결 (MCP / Unity Skills).
2. **Play Mode** 진입 — Game View 녹화 전제. `manage_editor(action: "play")`.
3. `com.unity.recorder` 패키지 설치.

## Workflow — timed MP4 (권장)

```
1. Play Mode 진입
2. execute_code → AgentUnityRecorder.StartMovie(durationSeconds)
3. duration + 2~3초 대기 (TimeInterval 자동 종료)
4. execute_code → AgentUnityRecorder.GetStatus() 또는 Stop()
5. path의 .mp4 존재 확인 후 사용자에게 경로 안내
```

### execute_code 예

```csharp
// 10초 기본 MP4
return AgentUnityRecorder.StartMovie(10f);

// 웹훅용 짧은 클립 (Medium)
return AgentUnityRecorder.StartMovie(
    durationSeconds: 8f,
    width: 1080,
    height: 1920,
    frameRate: 30f,
    fileName: "verify_clip",
    captureAudio: false,
    cameraTag: null,
    quality: "Medium");

// 상태 / 중지
return AgentUnityRecorder.GetStatus();
return AgentUnityRecorder.Stop();
```

반환 문자열에 `path=...` 와 `recording=true|false` 가 포함된다.

## Workflow — manual (길이 미정)

1. `StartMovie(0f)` — Manual 모드.
2. 연출/조작 진행.
3. 끝나면 `Stop()`.
4. `GetStatus()`로 `exists=true` 확인.

## Workflow — PNG sequence

```csharp
return AgentUnityRecorder.StartImageSequence(5f, fileName: "seq_ui");
// → Recordings/seq_ui_0001.png ...
```

## Tagged camera

`cameraTag`에 Unity Tag 이름을 넘긴다 (`CaptureUI = true`).

```csharp
return AgentUnityRecorder.StartMovie(6f, cameraTag: "MainCamera");
```

태그가 없으면 빈/잘못된 영상이 나올 수 있다. 불확실하면 Game View(`cameraTag: null`)를 쓴다.

## Waiting

- `TimeInterval`은 Recorder가 종료한다. Agent는 **블로킹 루프를 Editor에 돌리지 말고** 채팅 쪽에서 `duration + 2~3s` 대기 후 `GetStatus`/`Stop`한다.
- Domain reload / Play 종료 시 컨트롤러 static은 날아갈 수 있다. 마지막 경로는 `EditorPrefs`에 남으므로 `GetLastOutputPath()` / `GetStatus()`로 확인한다.

## Webhook

짧은 실패/검증 클립을 보낼 때는 `webhook-report-media` 판단 후:

```csharp
using WebhookFeedbackSystem;
WebhookFeedback.SendRecording(@"C:\path\to\Recordings\verify_clip.mp4", "제목", "설명");
```

## Do / Don't

| Do | Don't |
|----|-------|
| `execute_code` + `AgentUnityRecorder.*` | 비활성 MenuItem에 `execute_menu_item` 의존 |
| 출력은 `Recordings/` | `Assets/` 안에 대용량 영상 저장 |
| 완료 후 경로만 안내 | 녹화 파일을 커밋 |
| 단발 스크린샷은 screenshot 경로 | Recorder로 1프레임만 대체 |
| 성공 리포트마다 긴 MP4 웹훅 | `webhook-report-media` 위반 |

## Checklist

- [ ] Play Mode인가
- [ ] `StartMovie` / `StartImageSequence` 반환에 `error=` 없는가
- [ ] `duration + buffer` 대기 후 `exists=true`인가
- [ ] 사용자에게 **절대 경로** (`Recordings/...`) 안내했는가
- [ ] 녹화 파일을 스테이징하지 않았는가

## Related

- `webhook-report-media` — text / screenshot / recording 판단
- `webhook-screenshot-feedback` — 웹훅 전송 API
- `agent-editor-tools` — Agent 전용 static / MenuItem validate false
- 구현: `Assets/CyKimExtension/Editor/Agent/AgentUnityRecorder.cs`
