---
name: webhook-screenshot-feedback
description: Send Unity Game View screenshot feedback (or text-only notes) through a configurable webhook provider hub. Use when the user asks to send feedback/screenshots via webhook, switch the active webhook provider, or post a titled description with optional images.
---

# Webhook Screenshot Feedback

캡처·웹훅 전송 로직은 고정 구현되어 있다. **매번 새로 코드를 생성하지 말고** `WebhookFeedback` API만 호출한다.

## 사전 준비

- URL은 프로젝트 루트 `Secrets/` 아래 gitignore된 파일에 둔다.
  - `Secrets/discord_webhook_url.txt`
  - `Secrets/slack_webhook_url.txt`
- 활성 프로바이더는 `Secrets/webhook_active_provider.txt` (`Discord` | `Slack`)다.
- 송신 허브: `Assets/CyKimExtension/Editor/Agent/Webhook/WebhookFeedback.cs`

## 활성 프로바이더 설정

스킬/에이전트는 아래처럼 허브에서 전환한다. URL을 코드에 하드코딩하지 않는다.

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SetActiveProvider(WebhookFeedbackProvider.Slack);
// or WebhookFeedbackProvider.Discord

var current = WebhookFeedback.GetActiveProvider();
```

## API

### 텍스트만

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SendText("피드백 제목", "논의할 내용 요약");
```

### 스크린샷 + 제목/설명

1) MCP `manage_camera`로 Game View 캡처 (`camera` 미지정 시 Overlay UI 포함).

```json
{
  "action": "screenshot",
  "screenshot_file_name": "playtest_1.png"
}
```

- 저장: `Assets/Screenshots/<filename>`
- 비동기이므로 2~3초 대기 후 전송
- 같은 의도 여러 장은 캡처만 반복하고 **전송은 한 번**

2) 허브 호출

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.Send(
    new[]
    {
        "Assets/Screenshots/playtest_1.png",
        "Assets/Screenshots/playtest_2.png",
    },
    "피드백 제목",
    "변경 버전 설명");
```

활성 프로바이더와 무관하게 특정 채널로 보내려면:

```csharp
WebhookFeedback.Send(
    WebhookFeedbackProvider.Slack,
    new[] { "Assets/Screenshots/playtest_1.png" },
    "제목",
    "설명");
```

## 프로바이더 차이

| 프로바이더 | 이미지 전송 |
|---|---|
| Discord | multipart 일반 첨부(갤러리) |
| Slack | Incoming Webhook 제약으로 단기 공개 URL 업로드 후 image block |

## 확인

Unity 콘솔 `[WebhookFeedback]` / `[WebhookFeedbackSettings]` 로그. `read_console`에 해당 접두어 필터.

## 주의

- Editor 전용. `Tools/Agent/Webhook/Send Feedback` 메뉴는 비활성.
- Agent 호출은 MCP `execute_code`로 `WebhookFeedback.Send` / `SendText` / `SetActiveProvider`.
- 커밋 메시지/로그 안내에는 특정 서비스명을 남발하지 말고, 코드의 enum/Secrets 파일명만 정확히 쓴다.
