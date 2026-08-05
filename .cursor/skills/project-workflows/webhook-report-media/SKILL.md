---
name: webhook-report-media
description: >-
  Decide whether a webhook progress report should be text-only, include
  screenshot(s), or attach a short recording. Use when posting milestone /
  verification webhook feedback and choosing media for WebhookFeedback.
disable-model-invocation: true
---

# Webhook Report Media

단계 완료·검증 보고를 `WebhookFeedback`으로 보낼 때 **첨부 매체**를 고른다.  
전송 API는 `webhook-screenshot-feedback`을 따른다. 이 스킬은 **판단만** 담당한다.

## Decision table

| 상황 | 매체 | 이유 |
|------|------|------|
| API/스킬/문서만 추가, UI·연출 변화 없음 | **Text only** | 시각 증거 불필요 |
| Game View 상태·UI가 바뀜 | **Screenshot** (1장) | 진입/상태 확인을 한 장으로 충분 |
| 동작 전후 비교가 필요 | **Screenshot** (성공 1 / 실패 1) | 전후 상태 확인 |
| 실패·타임아웃 + 짧은 클립 경로 있음 | **Short recording** (≤10s) 또는 실패 스크린샷 | 동작 이상 구간 공유 |
| 긴 풀 플레이 영상 분석 | **하지 않음** (텍스트+키프레임) | 웹훅·토큰 비용 대비 ROI 낮음 |

## Hard limits

- 스크린샷: 메시지당 **최대 3장** (보통 1장).
- 녹화: **실패·타임아웃·시각 회귀**일 때만. 길이 ≤ **10초**. 웹훅용은 `quality: Medium` 권장.
- 성공한 인프라 단계(컴파일·스킬 문서)는 기본 **텍스트**.
- Secrets/URL을 본문에 넣지 않는다.

## Procedure

1. 이번 보고의 **검증 대상**이 코드 계약인지 / 화면 상태인지 / 시간축 동작인지 분류한다.
2. 위 표로 `text` | `screenshot` | `recording` 선택.
3. `screenshot`: Play Mode면 `manage_camera` screenshot → 2–3초 대기 → `WebhookFeedback.Send(...)`.
4. `recording`: `Recordings/` 경로 → `WebhookFeedback.SendRecording(...)`.
5. `text`: `WebhookFeedback.SendText(title, description)`.
6. 전송 후 필요 시 `WebhookFeedback.ClearScreenshotsFolder()`.

## Title / body template

```text
title: {작업명} 완료 / 실패
description:
- 항목: ...
- 결과: ok / failed
- 주요 API: ...
- 첨부 판단: text|screenshot|recording (이유 한 줄)
```

## Examples

- 스킬·문서만 추가 → **text**.
- Game View 확인이 목적 → **screenshot**.
- 짧은 실패 클립 MP4가 있음 → **recording** (없으면 screenshot).
