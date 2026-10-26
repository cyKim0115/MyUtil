---
description: 일회성 Editor 도구를 메뉴에 노출하지 않고 MCP로 호출하는 패턴
icon: robot
---

# Agent 전용 Editor 도구

AI/Agent가 **가끔** 실행하는 셋업·일괄 정리 도구는 `Tools` 메뉴를 늘리지 않습니다. 진입점은 `public static`으로 두고, MenuItem validate가 `false`를 반환해 메뉴를 비활성합니다. 호출은 Unity MCP `execute_code`로 `TypeName.MethodName()` 합니다.

## 패턴

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

{% hint style="danger" %}
비활성 메뉴에 `execute_menu_item`을 의존하지 마세요. 메뉴가 막힐 수 있습니다. 항상 `execute_code` + 타입.메서드입니다.
{% endhint %}

Agent 전용 메뉴는 `Tools/Agent/<카테고리>/...` 아래에 모읍니다 (예: `Tools/Agent/Recorder/Stop`). 사용자가 자주 여는 도구만 `Tools/` 직하에 활성으로 둡니다.

## MCP가 전부 타임아웃일 때

Unity 네이티브 모달(씬 리로드 대화상자 등)이 메인 스레드를 막은 경우가 많습니다. MCP로는 풀 수 없으니 바깥 도구로 확인합니다 → [에디터 도구](../learn/editor-tools.md)의 「씬 리로드 모달 해제」.

## 자주 걸리는 MCP 함정

- `execute_code`는 메서드 본문만 받습니다. 클래스 선언이 필요하면 전체 코드를 받는 다른 서버 도구를 씁니다.
- UnityMCP 포트는 에디터 재시작·도메인 리로드마다 바뀝니다. 포트를 적어 두지 말고 `python Assets/CyKimExtension/Editor/Agent/Tools~/unity-mcp-endpoint.py`로 확인합니다.
- 에디터 창 캡처에 `GrabPixels`(리플렉션)를 쓰지 않습니다. D3D12 에디터가 크래시합니다.
- 선택 변경·`SendEvent` 결과는 같은 호출 안에서 읽으면 한 박자 늦습니다. 다음 호출에서 읽습니다.

## 언제 활성 메뉴를 유지하나

사용자가 직접·자주 여는 것만 활성으로 둡니다.

- 에디터 윈도우: Favorite Prefab, Remove Missing Scripts 등
- 일상 단축키: Prefab Open, Custom Create GameObject 등

## 체크리스트

1. `public static` 진입점이 있는가
2. validate가 `return false`인가
3. Agent 호출이 `execute_code`인가
4. CLI `-batchmode` / `-executeMethod` 전용 진입점만 두지 않았는가

원문 경로(에이전트용, 복붙하지 않음):

- `.cursor/skills/project-workflows/agent-editor-tools/`
- `.cursor/rules/unity-agent-editor-tools.mdc`
- `.cursor/rules/unity-editor-agent-workflow.mdc`
