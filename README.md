# MyUtil

Unity 개발을 위한 유틸리티 및 확장 기능 모음입니다.

## 📋 개요

이 프로젝트는 Unity 개발 시 자주 사용하는 유틸리티 클래스, 커스텀 속성(Attribute), 에디터 도구들을 모아놓은 라이브러리입니다.

## ✨ 주요 기능

### 🔧 유틸리티 클래스

#### ChildUtil
Transform의 자식 객체를 탐색하는 확장 메서드를 제공합니다.

```csharp
// 이름으로 자식 찾기 (재귀적 탐색)
Transform child = parent.FindChildByName("TargetName");

// 모든 자식 Transform을 리스트로 수집
List<Transform> allChildren = new List<Transform>();
parent.GetTransformIncludeAllChild(ref allChildren);
```

#### PlatformUtil
플랫폼 및 빌드 타입을 확인하는 유틸리티입니다.

```csharp
if (PlatformUtil.IsEditor()) { /* 에디터 환경 */ }
if (PlatformUtil.IsDev()) { /* 개발 빌드 */ }
if (PlatformUtil.IsReal()) { /* 마켓 빌드 */ }
```

#### UILayoutUtil
UI 레이아웃을 하위부터 순차적으로 리빌드하는 유틸리티입니다.

```csharp
// LayoutGroup과 ContentSizeFitter를 하위부터 순차적으로 리빌드
transform.RebuildLayoutsFromBottom();
```

### 🎨 커스텀 속성 (Attributes)

#### ReadOnlyProperty
Inspector에서 필드를 읽기 전용으로 만드는 속성입니다.

```csharp
[ReadOnlyProperty] // 에디터에서만 읽기 전용
public int editorOnlyReadOnly;

[ReadOnlyProperty(true)] // 런타임에서만 읽기 전용
public int runtimeOnlyReadOnly;
```

#### ShowIfAttribute
조건에 따라 필드를 표시하거나 비활성화하는 속성입니다.

```csharp
public bool showField;

[ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(showField))]
public int conditionalField;

[ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.Or, "condition1", "condition2")]
public string disabledField;
```

### 📦 데이터 구조

#### SerializableDictionary
Unity Inspector에서 직렬화 가능한 Dictionary입니다.

```csharp
[Serializable]
public class MyClass : MonoBehaviour
{
    public SerializableDictionary<string, int> myDictionary;
}
```

#### DoubleColor
상단과 하단 색상을 가진 구조체입니다.

```csharp
public DoubleColor gradientColor = new DoubleColor(Color.white, Color.black);
```

### 🛠️ 에디터 도구

- **DataPathUtil**: 데이터 경로 관련 유틸리티
- **DoubleColorDrawer**: DoubleColor를 위한 커스텀 Property Drawer
- **FavoritePrefabWindow**: 자주 사용하는 프리팹을 관리하는 윈도우
- **FixResolutionScale**: 해상도 스케일 수정 도구
- **PrefabEditModeShortcut**: 프리팹 편집 모드 단축키
- **SerializableDictionaryDrawer**: SerializableDictionary를 위한 커스텀 Property Drawer

## 📦 요구사항

- Unity 2022.3 이상
- Universal Render Pipeline (URP)
- Input System 패키지

## 🚀 설치 방법

1. 이 저장소를 클론하거나 다운로드합니다.
2. Unity 프로젝트의 `Assets` 폴더에 `CyKimExtension` 폴더를 복사합니다.
3. Unity 에디터에서 프로젝트를 열면 자동으로 컴파일됩니다.

## 📝 사용 예제

### 자식 객체 찾기
```csharp
using cyKimUnityExtensions.UnityEngine;

// 특정 이름의 자식을 찾기
Transform target = transform.FindChildByName("Player");

// 재귀 깊이 제한 (최대 3단계)
Transform limited = transform.FindChildByName("Item", 3);
```

### 조건부 필드 표시
```csharp
public class MyComponent : MonoBehaviour
{
    public bool useAdvancedSettings;
    
    [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(useAdvancedSettings))]
    public float advancedValue;
    
    [ReadOnlyProperty(true)]
    public int runtimeValue;
}
```

### UI 레이아웃 리빌드
```csharp
using cyKimUnityExtensions.UnityEngine.UI;

// UI 레이아웃을 하위부터 순차적으로 리빌드
canvasTransform.RebuildLayoutsFromBottom();
```

## 📄 라이선스

이 프로젝트는 개인 사용 목적으로 제작되었습니다.
