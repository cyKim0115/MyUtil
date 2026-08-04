---
description: Favorite Prefab 창으로 자주 쓰는 프리팹을 세트별로 관리
icon: star
---

# Favorite Prefab 활용

프리팹을 프로젝트 여기저기에서 자주 꺼낼 때, Project 창을 매번 찾지 않고 **세트별 즐겨찾기**로 엽니다.

{% stepper %}
{% step %}
### 창 열기

메뉴 `Tools/Prefab/Favorite Prefab` 또는 단축키 **`Ctrl/Cmd+Shift+F`**.
{% endstep %}

{% step %}
### 프리팹 등록

창의 드롭 영역에 프리팹을 끌어다 놓습니다. 목록에서 드래그로 순서를 바꿀 수 있습니다.
{% endstep %}

{% step %}
### 세트 전환

여러 세트(예: UI / VFX / 환경)를 두고 드롭다운·칩으로 전환합니다. 기본 세트 이름은 `Default`입니다.
{% endstep %}

{% step %}
### 배치·편집

목록 항목을 클릭해 씬에 배치하거나, 프리팹 편집 흐름과 함께 사용합니다. 선택 프리팹만 빠르게 열려면 `Alt+E` (`Open Selected Prefab`)도 함께 쓰면 됩니다.
{% endstep %}
{% endstepper %}

{% hint style="success" %}
세트 구성은 EditorPrefs에 저장되므로 머신별로 유지됩니다. 팀 공유가 필요하면 별도 에셋/문서로 목록을 관리하세요.
{% endhint %}

구현 위치: `Assets/CyKimExtension/Editor/FavoritePrefabWindow.cs`
