---
description: Tools 메뉴·단축키·WebhookFeedback 개요
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
| **F12** (Inspector Component Shortcut) | TMP면 텍스트 포커스, Image면 Source Image 선택 |

## WebhookFeedback

Discord/Slack 웹훅으로 텍스트·스크린샷 피드백을 보냅니다. URL은 `Secrets/`(gitignore)에 둡니다.

```csharp
using WebhookFeedbackSystem;

WebhookFeedback.SetActiveProvider(WebhookFeedbackProvider.Discord);
WebhookFeedback.SendText("제목", "설명");
WebhookFeedback.Send(new[] { "Assets/Screenshots/a.png" }, "제목", "설명");
```

| Secrets 파일 | 내용 |
|--------------|------|
| `discord_webhook_url.txt` | Discord webhook URL |
| `slack_webhook_url.txt` | Slack webhook URL |
| `webhook_active_provider.txt` | `Discord` 또는 `Slack` |

메뉴 `Tools/Agent/Webhook/Send Feedback`는 Agent 전용(비활성)입니다. 사람용 상세 워크플로는 Cursor 스킬 원문 경로: `.cursor/skills/project-workflows/webhook-screenshot-feedback/`

## Editor 자동화 주의

열린 Unity Editor + Unity Skills / MCP로 메뉴·코드를 실행합니다. **같은 프로젝트에 `Unity.exe -batchmode`를 돌리지 마세요** — `Library/` 잠금·임포트 꼬임의 원인입니다.
