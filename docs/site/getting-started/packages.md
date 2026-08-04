---
description: MyUtil이 의존하는 외부 Unity 패키지 git URL
icon: box-open
---

# 외부 패키지

`Window > Package Manager > + > Add package from git URL…` 로 추가합니다.

{% tabs %}
{% tab title="LitMotion" %}
애니메이션(프로그레스 바, ScrollRect 등)에 사용합니다.

```text
https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion
```
{% endtab %}

{% tab title="UniTask" %}
비동기 대기·취소 토큰에 사용합니다.

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```
{% endtab %}

{% tab title="Cursor IDE Support" %}
Cursor와 Unity 연동용 IDE 패키지입니다.

```text
https://github.com/boxqkrtm/com.unity.ide.cursor.git
```
{% endtab %}

{% tab title="Unity MCP" %}
에디터를 Agent/MCP로 조작할 때 필요합니다.

```text
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
```
{% endtab %}

{% tab title="NuGetForUnity + MemoryPack" %}
1. NuGetForUnity 설치:

```text
https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
```

2. `NuGet > Manage NuGet Packages`에서 **MemoryPack** 검색 후 설치
{% endtab %}
{% endtabs %}

{% hint style="info" %}
모든 패키지가 모든 기능에 필수는 아닙니다. 예를 들어 ProgressBar/ScrollRect 애니메이션만 쓰면 LitMotion + UniTask가 핵심입니다.
{% endhint %}
