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

스크린샷 1장만이면 이 스킬 대신 Play 중 `ScreenCapture.CaptureScreenshot` 또는 Game View 스크린샷 MCP 도구(예: ai-game-developer `screenshot-game-view`)를 쓰고, 전송은 전역 스킬 `webhook-report`에 맡긴다. 카메라 렌더 기반 캡처(`manage_camera` 등)는 UI 캔버스가 빠질 수 있어 UI 증거로 쓰지 않는다.

## Defaults

| 항목 | 기본값 |
|------|--------|
| 출력 폴더 | 프로젝트 루트 `Recordings/` (gitignored, Assets 밖) |
| 해상도 | 1080×1920 (세로) |
| FPS | 30 |
| 코덱 | MP4 (High). 웹훅 전송 시 Medium 권장 |
| 소스 | Game View (UI 포함) |
| 길이 | `durationSeconds` (≤0이면 수동, `Stop` 필요) |

## faststart 리먹스 (자동)

Unity Recorder는 `moov`(재생 길이·프레임 인덱스)를 파일 **맨 끝**에 쓴다. 데스크톱 플레이어는 문제없지만
웹 인라인 플레이어(웹훅 미리보기 등)는 앞에서부터 스트리밍하므로 `moov`를 만나기 전까지 길이를 몰라
**0:00으로 표시**한다. 파일이 깨진 게 아니라 아톰 배치 문제다.

그래서 `Stop()`·TimeInterval 자동 종료 양쪽에서 `ffmpeg -c copy -movflags +faststart` **무손실 리먹스**를
자동 실행해 `moov`를 앞으로 옮긴다. 재인코딩이 아니라 화질·길이는 그대로다.

- 먹서가 `moov`를 다 쓸 때까지 파일이 잠겨 있으므로, 열릴 때까지 기다렸다 처리한다 (최대 5초).
- 성공 시 `[AgentUnityRecorder] faststart 리먹스 완료` 로그.
- **ffmpeg가 PATH에 없으면** 경고만 남기고 **원본을 그대로 둔다** (선택 단계라 녹화 자체는 실패하지 않는다).
  이 경우 웹 플레이어에서 0:00으로 보이므로 공유 전에 수동 리먹스한다:
  `ffmpeg -i in.mp4 -c copy -movflags +faststart out.mp4`
- 검증: `moov`가 `mdat`보다 앞 offset인지 확인한다.

## Prerequisites

1. Unity Editor 연결 (MCP / Unity Skills).
2. **Play Mode** 진입 — Game View 녹화 전제. `manage_editor(action: "play")`.
3. `com.unity.recorder` 패키지 설치.
4. (선택) `ffmpeg`가 PATH에 있으면 종료 후 faststart 리먹스가 자동 실행된다.
5. 에디터 포커스가 없어도 Play가 진행되도록 Player Settings `Run In Background`를 켜 둔다.

## Workflow — timed MP4 (권장)

```
1. Play Mode 진입 (+ 필요 시 목표 화면까지 진입·연출 대기)
2. execute_code → AgentUnityRecorder.StartMovie(durationSeconds)
3. duration + 2~3초 대기 (TimeInterval 자동 종료). 대기 중 가림 팝업이 뜨면 닫고 진행
4. execute_code → AgentUnityRecorder.GetStatus() 또는 Stop()
5. path의 .mp4 존재 확인 후 사용자에게 경로 안내
6. Play Mode는 사용자가 계속 볼 때만 유지, 아니면 종료
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
- 녹화 대상을 가리는 팝업(해금·보상 알림 등)이 뜨면 녹화를 중단하지 말고 프로젝트 방식으로 닫은 뒤 이어간다. 팝업 자체가 녹화 목표면 닫지 않는다. `duration`을 한 번에 기다리지 말고 중간에 확인한다.
- Domain reload / Play 종료 시 컨트롤러 static은 날아갈 수 있다. 마지막 경로는 `EditorPrefs`에 남으므로 `GetLastOutputPath()` / `GetStatus()`로 확인한다.
- 해상도를 기본값(1080×1920)과 다르게 지정하면 시작 시 Game View `selectedSizeIndex`를 저장했다가, `Stop()` 또는 `TimeInterval` 자동 종료 때(Agent가 `Stop`을 호출하지 않아도) 자동 원복한다. 별도 처리 불필요.

## Webhook

짧은 실패/검증 클립은 **전역 스킬 `webhook-report`**로 보낸다. 매체 판단표도 그쪽에 있다.
녹화만 이 스킬이 맡고, 전송은 전역 스킬 소관이다 — Unity Editor가 떠 있을 필요가 없다.

```bash
python3 ~/.claude/skills/webhook-report/send_webhook.py \
  --key <프로젝트 룰이 지정한 키> \
  --title "연출 - 검증 클립" --description "설명" \
  --file Recordings/verify_clip.mp4
```

## Do / Don't

| Do | Don't |
|----|-------|
| `execute_code` + `AgentUnityRecorder.*` | 비활성 MenuItem에 `execute_menu_item` 의존 |
| 출력은 `Recordings/` | `Assets/` 안에 대용량 영상 저장 |
| 완료 후 경로만 안내 | 녹화 파일을 커밋 |
| 단발 스크린샷은 screenshot 경로 | Recorder로 1프레임만 대체 |
| 성공 리포트마다 긴 MP4 웹훅 | `webhook-report`의 매체 판단표 위반 |
| 녹화 중 가림 팝업은 닫고 진행 | 가림 팝업 때문에 녹화를 중단하거나 덮인 채로 방치 |

## Checklist

- [ ] Play Mode인가
- [ ] `StartMovie` / `StartImageSequence` 반환에 `error=` 없는가
- [ ] `duration + buffer` 대기 후 `exists=true`인가
- [ ] 사용자에게 **절대 경로** (`Recordings/...`) 안내했는가
- [ ] 녹화 파일을 스테이징하지 않았는가
- [ ] 웹 미리보기용이면 faststart 리먹스 로그(완료/실패)를 확인했는가

## Related

- 전역 스킬 `webhook-report` — 매체 판단(text / screenshot / recording) + 전송
- `agent-editor-tools` — Agent 전용 static / MenuItem validate false
- 구현: `Assets/CyKimExtension/Editor/Agent/AgentUnityRecorder.cs`
