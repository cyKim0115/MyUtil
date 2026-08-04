---
description: 일회성 Editor 도구를 메뉴에 노출하지 않고 MCP로 호출하는 패턴
icon: robot
---

# Agent 전용 Editor 도구

AI/Agent가 **가끔** 실행하는 셋업·일괄 정리 도구는 `Tools` 메뉴를 늘리지 않습니다. 진입점은 `public static`으로 두고, MenuItem validate가 `false`를 반환해 메뉴를 비활성합니다. 호출은 Unity MCP `execute_code`로 `TypeName.MethodName()` 합니다.

## 패턴

```csharp
private const string MenuPath = "Tools/Feature/Agent Only Action";

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
