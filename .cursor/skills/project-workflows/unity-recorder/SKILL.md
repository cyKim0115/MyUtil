---
name: unity-recorder
description: >-
  Record Unity Game View (or tagged camera) to MP4 / PNG sequence / a single PNG still
  at a fixed resolution via Unity Recorder and AgentUnityRecorder. Use when the user asks
  to record, capture video, movie, clip, image sequence, a target-resolution screenshot,
  or use Unity Recorder from chat.
disable-model-invocation: true
---

# Unity Recorder (Agent)

Play Mode에서 Game View(또는 태그 카메라)를 MP4 / PNG 시퀀스로 녹화한다.
전용 MCP 도구는 없고, 프로젝트 헬퍼 `AgentUnityRecorder`를 MCP `execute_code`로 호출한다.

패키지: `com.unity.recorder`.

## When to use

- “녹화해줘”, “MP4로 뽑아줘”, “이미지 시퀀스”, “Unity Recorder로 …”
- 연출/UI 검증용 짧은 클립이 필요할 때. 짧은 클립(대략 15초 이하)은 **60fps**로 찍는다.
- 목표 해상도 그대로의 스크린샷 1장이 필요할 때 → `CaptureStill`

스크린샷 1장은 `AgentUnityRecorder.CaptureStill()`로 찍는다. Game View 창 크기와 무관하게 지정 해상도(기본 1080×1920)로 렌더하므로, 목표 해상도에서만 보이는 레이아웃 문제가 그대로 찍힌다. `ScreenCapture.CaptureScreenshot`은 창 크기 그대로라 작은 창에서는 아래가 잘린 그림이 나온다. 카메라 렌더 기반 캡처(`manage_camera` 등)는 UI 캔버스가 빠질 수 있어 UI 증거로 쓰지 않는다. 전송은 전역 스킬 `webhook-report`에 맡긴다.

## Defaults

| 항목 | 기본값 |
|------|--------|
| 출력 폴더 | 프로젝트 루트 `Recordings/` (gitignored, Assets 밖) |
| 해상도 | 1080×1920 (세로) |
| FPS | 헬퍼 기본값 30. 짧은 클립(대략 15초 이하)은 `frameRate: 60f`를 넘긴다. 긴 게임플레이 녹화만 30 |
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
5. 에디터 포커스가 없어도 Play는 진행된다. `EditorPlayModeRunInBackground`가 Play 동안만 `runInBackground`를 켜고, Edit 복귀 때 원래 값으로 돌린다. Player Settings를 손으로 켤 필요가 없다. 이 스크립트가 없는 프로젝트면 Play 진입 뒤 `execute_code`로 `Application.runInBackground = true;`를 부르고, 끝나면 되돌린다(에디터에서는 이 값이 PlayerSettings와 같은 칸이다).

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
    frameRate: 60f, // 짧은 클립은 60. 생략하면 헬퍼 기본값 30이다
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
// → Recordings/seq_ui_<프레임 번호 4자리>.png ...
```

## Workflow — still (스크린샷 1장)

```csharp
return AgentUnityRecorder.CaptureStill("title_check");
// → Recordings/title_check.png (1080×1920). 해상도를 바꾸면 Game View 사이즈는 끝나고 원복된다
return AgentUnityRecorder.CaptureStill("tablet", width: 2048, height: 1536);
```

- Play Mode에서만 동작한다. Recorder가 렌더된 프레임을 받아 쓰므로 **호출 직후에는 파일이 아직 없다.** 몇 백 ms 뒤 다음 호출에서 `GetStatus()`의 `exists=true`를 확인한다.
- 팝업·연출을 연 호출과 같은 호출에서 찍지 않는다. 열림 애니메이션 첫 프레임이 찍힌다. 다음 호출에서 찍는다.

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

짧은 실패/검증 클립과 정지컷은 **전역 스킬 `webhook-report`**로 보낸다. 매체 판단표도 그쪽에 있다.
녹화만 이 스킬이 맡고, 전송은 전역 스킬 소관이다 — Unity Editor가 떠 있을 필요가 없다.

전송 명령은 여기 옮겨 적지 않는다. 채널 키(프로젝트 룰이 지정), `--agent <내 이름>`, OS별 인터프리터(Windows는 `python`), 한글 본문은 `--description-file`로 넘기는 규칙까지 전역 스킬 원문을 그대로 따른다. 첨부는 `--file Recordings/<파일>`이다.

## Do / Don't

| Do | Don't |
|----|-------|
| `execute_code` + `AgentUnityRecorder.*` | 비활성 MenuItem에 `execute_menu_item` 의존 |
| 출력은 `Recordings/` | `Assets/` 안에 대용량 영상 저장 |
| 완료 후 경로만 안내 | 녹화 파일을 커밋 |
| 단발 정지컷은 `CaptureStill` (목표 해상도) | 창 크기 그대로인 `ScreenCapture`를 해상도 증거로 쓰기 |
| 짧은 클립은 `frameRate: 60f` | 짧은 연출·UI를 기본 30fps로 찍기 |
| 매체 판단은 `webhook-report` 표 기준 | 성공 리포트마다 긴 MP4 웹훅 |
| 녹화 중 가림 팝업은 닫고 진행 | 가림 팝업 때문에 녹화를 중단하거나 덮인 채로 방치 |

## Checklist

- [ ] Play Mode인가
- [ ] `StartMovie` / `StartImageSequence` / `CaptureStill` 반환에 `error=` 없는가
- [ ] 짧은 클립이면 `frameRate: 60f`를 넘겼는가 (헬퍼 기본값은 30)
- [ ] `duration + buffer` 대기 후(정지컷은 다음 호출에서) `exists=true`인가
- [ ] 사용자에게 **절대 경로** (`Recordings/...`) 안내했는가
- [ ] 녹화 파일을 스테이징하지 않았는가
- [ ] 웹 미리보기용이면 faststart 리먹스 로그(완료/실패)를 확인했는가

## Related

- 전역 스킬 `webhook-report` — 매체 판단(text / screenshot / recording) + 전송
- `agent-editor-tools` — Agent 전용 static / MenuItem validate false
- 구현: `Assets/CyKimExtension/Editor/Agent/AgentUnityRecorder.cs`
