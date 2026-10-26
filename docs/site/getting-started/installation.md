---
description: CyKimExtension을 Unity 프로젝트에 넣는 방법
icon: download
---

# 설치

{% stepper %}
{% step %}
### 서브모듈까지 받기

`Assets/CyKimExtension/Editor/Animation`(AnimationClip 경로 치환 도구)은 별도 저장소를 서브모듈로 넣은 것입니다. `git clone --recurse-submodules`로 받거나, 이미 받았다면 `git submodule update --init`을 실행합니다.
{% endstep %}

{% step %}
### CyKimExtension 복사

이 저장소의 `Assets/CyKimExtension/` 폴더를 대상 Unity 프로젝트의 `Assets/` 아래로 복사합니다. 서브모듈 안의 `.git` 파일(`Editor/Animation/.git`)은 빼고 복사합니다. 그대로 두면 대상 저장소가 그 폴더를 별도 저장소로 착각합니다.

{% hint style="warning" %}
`.meta` 파일은 Unity가 임포트 시 자동 생성합니다. 수동으로 만들지 마세요.
{% endhint %}
{% endstep %}

{% step %}
### Unity 버전·패키지 확인

- **Unity 6** (6000.4.12f1에서 개발·확인). `EntityId`·`GetEntityId()`를 써서 Unity 2022.3에서는 컴파일되지 않습니다
- 프로젝트에 **URP**, **Input System**, **TextMeshPro**(`com.unity.ugui`)가 있어야 합니다
{% endstep %}

{% step %}
### 외부 패키지 추가

기능에 따라 LitMotion, UniTask, Unity MCP 등이 필요합니다. 목록과 git URL은 [외부 패키지](packages.md)를 보세요.
{% endstep %}

{% step %}
### 컴파일 확인

Unity Editor에서 스크립트 컴파일이 끝나는지 확인합니다. 콘솔에 빨간 에러가 없으면 설치가 끝난 상태입니다.
{% endstep %}
{% endstepper %}

## 설치 후 바로 확인할 것

| 확인 | 방법 |
|------|------|
| 런타임 유틸 | `PlatformUtil.IsEditor()` 등 호출이 컴파일되는지 |
| 에디터 메뉴 | `Tools/Prefab/Favorite Prefab` (`Ctrl/Cmd+Shift+F`) |
| Inspector | `ReadOnlyProperty`, `ShowIf` Attribute가 Drawer와 함께 동작하는지 |

다음: [외부 패키지](packages.md) · [라이브러리 개요](../learn/overview.md)
