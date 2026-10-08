---
name: agent-editor-tools
description: Agent-only Unity Editor setup/optimize tools — keep public static entry points, disable MenuItem via validate false, call via MCP execute_code. Use when adding one-shot editor tools the AI runs, or when deciding whether a Tools menu should stay visible.
---

# Agent Editor Tools

에이전트(AI)가 가끔 실행하는 Unity Editor 일회성·재생성 도구의 작성/호출 규칙이다.

## 언제 이 스킬을 쓰는가

- 폰트 아틀라스 재생성, 일괄 정리처럼 **Agent가 간헐적으로 돌리는** Editor 스크립트를 추가할 때
- 사용자 `Tools` 메뉴를 더 늘리고 싶지 않을 때
- 기존 Agent 도구의 MenuItem을 비활성할 때

## 패턴 (필수)

1. **실제 로직**은 `public static` 메서드에 둔다.
2. `[MenuItem]`은 경로 문서화용으로 두어도 되지만, **validate가 항상 `false`** 를 반환해 메뉴를 비활성한다.
3. Agent 실행은 Unity MCP **`execute_code`** 로 `TypeName.MethodName()` 호출.
4. 비활성 메뉴에 `execute_menu_item`을 의존하지 않는다 (비활성 메뉴는 실행이 막힐 수 있음).
5. Agent 전용 메뉴 경로는 `Tools/Agent/{기존카테고리}/...` 아래에 모은다 (`Tools/Agent/Recorder/Stop` 등).

```csharp
private const string MenuPath = "Tools/Agent/Feature/Agent Only Action";

[MenuItem(MenuPath)]
public static void Run()
{
    // 실제 작업
}

[MenuItem(MenuPath, true)]
private static bool ValidateRun()
{
    // Agent 전용. 사용자 Tools 메뉴에서는 비활성.
    return false;
}
```

## Agent 호출 예

```csharp
// MCP execute_code
SomeSetupTool.Run();
AgentEditorDialogGuard.PrepareSave(); // dirty 씬/프리팹 저장 후 Stage 닫기
```

MCP 호출이 전부 타임아웃이면 네이티브 모달이 메인 스레드를 막은 것일 수 있다. `python Assets/CyKimExtension/Editor/Agent/Tools~/unity-modal.py`로 확인하고, 씬 리로드 모달은 `--reload`(비콘 `AgentSceneDirtyBeacon` 기준 열린 씬이 모두 저장 상태일 때만 누름). 종료 코드 2면 사용자에게 묻고, Ignore는 누르지 않는다. 상세는 `.cursor/rules/unity-agent-editor-tools.mdc`.

## 셋업 잔여·멱등

- 기존 프리팹·UI를 복사해 셋업하면 남은 스프라이트 GUID·`m_MethodName`·비활성 오브젝트를 셋업이 정리한다. 반복 셀은 가능하면 코드로 새로 만든다.
- 셋업은 멱등이다. 두 번 실행해도 오브젝트·이벤트가 늘지 않는다.
- private `UnityEvent`는 리플렉션(`BindingFlags.NonPublic`) 대신 `SerializedObject`로 `m_PersistentCalls`를 쓴다. `GetComponent` 대신 `TryGetComponent`.
- `*_setup_done` 같은 마커 파일을 만들지 않는다.

## 활성 MenuItem을 유지하는 경우

사용자가 직접·자주 여는 도구만 활성으로 둔다.

- 에디터 윈도우 (`Favorite Prefab`, `Remove Missing Scripts` 등)
- 일상 단축키/워크플로 (`Prefab Open`, `Custom Create GameObject` 등)

이 도구들은 `Tools/` 직하에 그대로 두고, `Tools/Agent/` 아래로 옮기지 않는다.

## Checklist

1. `public static` 진입점 있는가
2. validate `return false` 로 메뉴 비활성인가
3. Agent 호출 경로가 `execute_code` + 타입.메서드인가
4. 메뉴 경로가 `Tools/Agent/...` 인가 (사용자 도구면 `Tools/` 직하)
5. `.cursor/rules/unity-agent-editor-tools.mdc` 와 모순되지 않는가
6. 복사 잔여(스프라이트 GUID·`m_MethodName`·비활성 오브젝트)를 셋업이 정리하는가
7. 두 번 실행해도 오브젝트·이벤트가 늘지 않는가
8. private `UnityEvent`는 `SerializedObject`의 `m_PersistentCalls`인가. `TryGetComponent`인가
9. `*_setup_done` 마커 파일이 없는가
