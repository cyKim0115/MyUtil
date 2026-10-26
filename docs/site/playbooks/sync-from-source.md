---
description: 다른 Unity 프로젝트에서 범용 유틸·룰·스킬만 이 라이브러리로 가져오기
icon: arrows-rotate
---

# 소스 프로젝트에서 최신화

게임 프로젝트에서 검증한 **범용** 스크립트·Cursor 규칙·스킬만 MyUtil로 가져옵니다. 게임/Blender 전용은 제외합니다.

## 준비

1. 루트 `.env`를 `.env.example`에서 복사해 채웁니다.
2. 필수 변수:

| 변수 | 의미 |
|------|------|
| `SYNC_SOURCE_ROOT` | 가져올 Unity 프로젝트 루트(절대 경로) |
| `SYNC_SOURCE_SCRIPTS_REL` | 소스 안 유틸 스크립트 상대 경로 |
| `SYNC_SOURCE_AGENT_REL` | 소스의 룰·스킬 정본 폴더 상대 경로 (기본 `.claude`. 소스의 `.cursor`는 여기서 생성된 사본) |
| `SYNC_SOURCE_EXTRA_DRAWER_REL` | (선택) Dictionary Drawer 추가 경로 |

{% hint style="warning" %}
`.env`는 커밋하지 마세요. 머신 경로·다른 제품명이 스킬/커밋에 하드코딩되지 않게 합니다.
{% endhint %}

## 흐름

```mermaid
flowchart TD
  A[.env 로드] --> B[manifest 기준 diff]
  B --> C{게임 결합?}
  C -->|예| D[스킵 / 후보만 기록]
  C -->|아니오| E[Assets·.cursor에 반영]
  E --> F[manifest 갱신]
  F --> G[요약 — 커밋은 요청 시에만]
```

{% stepper %}
{% step %}
### Cursor에서 `최신화` 입력

채팅에 `최신화`, `동기화`, `유틸 가져와` 중 하나를 입력하면 Agent가 sync 스킬을 따릅니다.
{% endstep %}

{% step %}
### 후보 검토

manifest에 없는 새 파일은 자동 추가하지 않고 **후보**로 보여 줍니다. 범용인지 확인한 뒤 반영합니다. 이 라이브러리가 앞서 고친 파일과 다른 프로젝트에서 가져온 파일은 manifest에 따로 적혀 있고, 소스 사본으로 되돌리지 않습니다.
{% endstep %}

{% step %}
### (요청 시) 관여 레포 전체로 넓히기

「관여하는 레포 전체를 봐줘」처럼 요청하면 소스 프로젝트 밖으로 넓힙니다. 내 커밋이 있는 레포만 고르고, Unity 프로젝트와 룰·스킬 팩을 같은 기준으로 훑은 뒤 계획표로 먼저 보여 줍니다.
{% endstep %}

{% step %}
### `.meta` · 제외 정책

`.meta`는 만들지 않습니다. 전체 `unity-skills` 트리, Blender 전용, ProjectSettings 업그레이드 노이즈는 범위 밖입니다.
{% endstep %}
{% endstepper %}

원문 경로(에이전트용): `.cursor/skills/project-workflows/sync-from-source/` · `sync-manifest.md`
