---
name: particle-effect-controller
description: >-
  Parent ParticleSystem with renderer off as a hierarchy controller.
  Use when grouping multi-emitter VFX, asking if parent Stop/Play drives children,
  or authoring FX prefabs with a non-rendering root.
disable-model-invocation: true
---

# Particle Effect Controller

루트에 **렌더러 없는 `ParticleSystem`**을 두면, 그 한 시스템이 자식 이미터의 Play/Stop 컨트롤러가 된다.

C# 래퍼는 두지 않는다. Unity `ParticleSystem`의 `withChildren` 기본값이 이미 이 역할이다.

## Pattern

```
FX_Root            ParticleSystem + ParticleSystemRenderer.enabled = false
  Emitter_A        ParticleSystem (실제 그리기)
  Emitter_B        ParticleSystem (실제 그리기)
```

- 루트 렌더러는 꺼 둔다. Emission rate는 **0**이 깔끔하다 (렌더러가 꺼져 있으면 안 보이지만 불필요한 시뮬레이션을 줄인다).
- Sub Emitters가 아니다. 자식은 독립 이미터이고, **계층 Play/Stop 묶음**만 공유한다.

## API (기본값 `withChildren: true`)

| 호출 | 자식까지 |
|---|---|
| `root.Play()` / `Play(true)` | 예 |
| `root.Stop()` / `Stop(true)` | 예 |
| `root.Pause()` / `Pause(true)` | 예 |
| `root.IsAlive()` / `IsAlive(true)` | 예 |
| `root.IsAlive(false)` | **루트만** — 자식 생존과 무관 |

에디터 Particle Effect 미리보기도 이 계층을 **하나의 이펙트**로 취급한다.

## Pitfalls

- 루트 **Duration이 끝나도 자식은 자동으로 멈추지 않는다.** Duration은 그 시스템의 이미션 창일 뿐이다.
- 풀 반환·종료 대기에 `IsAlive(false)`만 쓰면 자식이 아직 살아 있어도 끝난 것으로 본다. 계층 전체면 `IsAlive(true)`, 또는 자식마다 순회.
- 자식마다 `GetComponentsInChildren<ParticleSystem>`으로 Play/Stop을 돌려도 런타임은 동작한다. 루트 컨트롤러는 에디터 미리보기와 `root.Stop()` 한 번에 가치가 있다.
