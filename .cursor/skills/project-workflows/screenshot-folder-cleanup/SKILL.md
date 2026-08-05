---
name: screenshot-folder-cleanup
description: Clear Assets/Screenshots after webhook/playtest captures are done. Use when finishing screenshot feedback, Unity Game View captures, or when the user asks to empty/clean the Screenshots folder.
---

# Screenshot Folder Cleanup

캡처·웹훅 전송이 **끝난 뒤** `Assets/Screenshots/` 임시 파일을 비운다. gitignore 대상이지만 로컬에 쌓이지 않게 마무리한다.

## 언제

- `webhook-screenshot-feedback` 전송 완료 후
- Game View / `manage_camera` 스크린샷으로 검증·보고를 마친 후
- 사용자가 스크린샷 폴더 비우기·정리를 요청할 때

전송·확인 **전에** 지우지 않는다.

## 방법

고정 API만 호출한다. 매번 삭제 스크립트를 새로 쓰지 않는다.

```csharp
using WebhookFeedbackSystem;

var deleted = WebhookFeedback.ClearScreenshotsFolder();
```

- 경로: `Assets/Screenshots` (`WebhookFeedback.ScreenshotsFolderPath`)
- 폴더·`Assets/Screenshots.meta`는 유지
- `.gitkeep`은 유지
- 그 외 파일(`.png`, `.meta` 등) 삭제 후 `AssetDatabase.Refresh`
- 콘솔: `[WebhookFeedback] Screenshots 폴더 정리 완료`

## 웹훅 워크플로와의 순서

```text
캡처 → (필요 시 대기) → WebhookFeedback.Send → 콘솔 확인 → ClearScreenshotsFolder
```

상세 송신 절차는 `project-workflows/webhook-screenshot-feedback`를 따른다.

## 주의

- Editor 전용. MenuItem `Tools/Agent/Webhook/Clear Screenshots Folder`는 validate `false`(Agent 전용).
- 커밋에 `Assets/Screenshots/**`를 넣지 않는다 (`.gitignore`됨).
