---
description: ReadOnlyProperty, ShowIf, SerializableDictionary 등
icon: tags
---

# Attribute · 데이터 구조

## Attribute

### ReadOnlyProperty

Inspector에서 필드를 읽기 전용으로 표시합니다. Drawer는 에디터 어셈블리에 함께 둡니다.

### ShowIfAttribute

조건 필드에 따라 다른 필드를 숨기거나 비활성화합니다.

```csharp
public enum ActionOnConditionFail { DontDraw, JustDisable }
public enum ConditionOperator { And, Or }

// 예: 조건이 거짓이면 필드를 그리지 않음
[ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(useAdvanced))]
public float advancedValue;
```

## 데이터 구조

| 타입 | 설명 |
|------|------|
| `SerializableDictionary` | Inspector에서 직렬화 가능한 Dictionary |
| `DoubleColor` | 상·하단 색상 쌍 (+ Drawer) |
| `LabelDictionary` | 라벨이 있는 Dictionary (`label` 기본값 `-1`) |
| `ProbabilityDictionary` | 가중치/확률 Dictionary |

{% hint style="info" %}
Dictionary Drawer가 소스 프로젝트에만 있는 경우, [최신화](../playbooks/sync-from-source.md) 시 `.env`의 `SYNC_SOURCE_EXTRA_DRAWER_REL`로 경로를 지정할 수 있습니다.
{% endhint %}
