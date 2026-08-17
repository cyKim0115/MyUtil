---
name: webhook-screenshot-feedback
description: Send Unity Game View screenshot feedback (or text-only notes) through a configurable webhook provider hub. Use when the user asks to send feedback/screenshots via webhook, switch the active webhook provider, or post a titled description with optional images.
---

# Webhook Screenshot Feedback

캡처·웹훅 전송 로직은 고정 구현되어 있다. **매번 새로 코드를 생성하지 말고** 정해진 방식만 따른다.
매체(text / screenshot / recording) 선택은 `webhook-report-media`를 따른다.

**텍스트 전용 보고는 Unity Editor/MCP를 절대 거치지 않는다.** 진행 상황·완료 보고·계획표 등 텍스트만 보낼 때는
아래 "텍스트만" 절의 셸 스크립트로 직접 POST한다. Unity MCP `execute_code`로 `WebhookFeedback.SendText`를
호출하는 경로는 쓰지 않는다 (Unity 인스턴스가 없거나 연결이 끊겨도 항상 보낼 수 있어야 하고, 연결 여부로
보고가 막혀서는 안 된다). 스크린샷/녹화 첨부는 캡처 자체가 Unity에서만 가능하므로 그 경우에 한해
`WebhookFeedback.Send` / `SendRecording`을 MCP `execute_code`로 호출한다.

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
// or WebhookFeedbackProvider.Discord / Both

var current = WebhookFeedback.GetActiveProvider();
```

## API

### 텍스트만 — 항상 셸에서 직접 POST (Unity 없이)

Agent 환경(Bash/PowerShell)에서 아래 스크립트를 스크래치패드에 써서 실행한다. `Secrets/webhook_active_provider.txt`를
직접 읽어 `Discord` / `Slack` / `Both`에 맞게 보낸다. Unity가 켜져 있는지, MCP가 연결됐는지는 확인하지 않는다.

```python
import json
import urllib.request
import urllib.error
from pathlib import Path

PROJECT_ROOT = Path(r"C:\Users\cykim\repo\MyUtil")  # 대상 프로젝트 루트로 교체
SECRETS = PROJECT_ROOT / "Secrets"
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"

TITLE = "제목"
DESCRIPTION = "설명"

def read_secret(name):
    p = SECRETS / name
    return p.read_text(encoding="utf-8-sig").strip() if p.exists() else None

def active_provider():
    raw = read_secret("webhook_active_provider.txt")
    return (raw or "Discord").strip()

def post_json(url, payload):
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json", "User-Agent": UA},
    )
    try:
        with urllib.request.urlopen(req) as resp:
            print("status:", resp.status)
    except urllib.error.HTTPError as e:
        print("HTTPError:", e.code, e.read().decode("utf-8", "ignore"))
        raise

def send_discord(url):
    post_json(url, {"embeds": [{"color": 0x5865F2, "title": TITLE, "description": DESCRIPTION}]})

def send_slack(url):
    post_json(url, {
        "text": TITLE or DESCRIPTION,
        "icon_emoji": ":pepe_dance:",
        "blocks": [
            {"type": "header", "text": {"type": "plain_text", "text": TITLE or "Feedback"}},
            {"type": "section", "text": {"type": "mrkdwn", "text": DESCRIPTION}},
        ],
    })

provider = active_provider()
if provider in ("Discord", "Both"):
    url = read_secret("discord_webhook_url.txt")
    if url:
        send_discord(url)
if provider in ("Slack", "Both"):
    url = read_secret("slack_webhook_url.txt")
    if url:
        send_slack(url)
```

- Secrets `.txt`는 BOM이 붙어 있을 수 있어 `utf-8-sig`로 읽는다.
- Discord 웹훅 도메인은 Cloudflare 뒤에 있어 브라우저형 `User-Agent` 없이 POST하면 `403 error code: 1010`이 난다. 위 스크립트처럼 항상 UA를 지정한다.
- `WebhookFeedback.cs` / `DiscordWebhookTransport.cs` / `SlackWebhookTransport.cs`의 JSON 포맷을 그대로 따른 것이므로, C# 쪽 포맷이 바뀌면 이 스크립트도 맞춰 갱신한다.

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

### 녹화(MP4 등)

`Recordings/` 등 Assets 밖 경로도 절대/상대 모두 가능. Slack은 임시 URL 링크, Discord는 첨부.

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SendRecording(@"Recordings/verify_clip.mp4", "실패 클립", "타임아웃 구간");
```

녹화는 `unity-recorder` + `AgentUnityRecorder`로 만든다.
## 프로바이더 차이

| 프로바이더 | 이미지 전송 |
|---|---|
| Discord | multipart 일반 첨부(이미지 갤러리 / 영상) |
| Slack | Incoming Webhook 제약으로 단기 공개 URL 업로드 후 image block 또는 링크 |

## 확인

- 텍스트(직접 POST): 스크립트 출력의 `status:` (200번대) 또는 `HTTPError:` 로 확인한다. Unity 콘솔에는 남지 않는다.
- 스크린샷/녹화(MCP 경유): Unity 콘솔 `[WebhookFeedback]` / `[WebhookFeedbackSettings]` 로그. `read_console`에 해당 접두어 필터.

## 마무리: Screenshots 폴더 비우기

전송·확인이 끝나면 `Assets/Screenshots/`를 비운다. 상세는 `project-workflows/screenshot-folder-cleanup`.

```csharp
WebhookFeedback.ClearScreenshotsFolder();
```

## 주의

- Editor 전용 기능(`WebhookFeedback.cs` 등)은 `Tools/Agent/Webhook/Send Feedback` 메뉴가 비활성인 채로 MCP에서만 호출된다 — 단, 이는 스크린샷/녹화 첨부 경로에 한한다.
- **텍스트 전용 보고는 Unity MCP `execute_code`로 `WebhookFeedback.SendText`를 호출하지 않는다.** 항상 "텍스트만" 절의 셸 스크립트로 직접 POST한다. Unity 인스턴스 유무·MCP 연결 여부는 텍스트 보고를 막는 조건이 될 수 없다.
- 스크린샷(`Send`)·녹화(`SendRecording`)는 캡처 자체가 Unity에서만 가능하므로 MCP `execute_code`로 `WebhookFeedback.Send` / `SendRecording` / `SetActiveProvider` / `ClearScreenshotsFolder`를 호출한다.
- 커밋 메시지/로그 안내에는 특정 서비스명을 남발하지 말고, 코드의 enum/Secrets 파일명만 정확히 쓴다.
