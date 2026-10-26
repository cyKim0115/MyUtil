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
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0
```

`#main` 대신 태그나 커밋 SHA로 고정합니다. 움직이는 참조는 임포트할 때마다 새 에디터 코드가 바로 실행되고, MCP 서버(`mcpforunityserver==<같은 버전>`)와 버전이 어긋날 수 있습니다.
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
모든 패키지가 모든 기능에 필수는 아닙니다. 예를 들어 ProgressBar/ScrollRect 애니메이션만 쓰면 LitMotion + UniTask가 핵심입니다. git 패키지는 URL 끝에 `#<태그>`를 붙여 고정하기를 권장합니다.
{% endhint %}

## 관련 패키지

**CyKim Scroller**는 가상화 uGUI ScrollRect(셀 재사용, 가변 셀 크기, 루프·스냅)입니다. MyUtil의 `ScrollRectUtil`과는 다른 기능이라 별도 UPM 패키지로 둡니다.

```text
https://github.com/cyKim0115/cyKimScroller.git?path=/Packages/com.cykim.scroller#v0.3.0
```
