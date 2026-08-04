---
description: Unity 개인 유틸·에디터 편의 라이브러리 MyUtil 문서
icon: house
---

# MyUtil

Unity 개발에서 반복되는 유틸리티, Inspector Attribute, 에디터 도구를 한곳에 모은 **개인용 라이브러리**입니다. 게임 도메인 로직이 아니라, 여러 프로젝트에 옮길 수 있는 범용 자산만 담습니다.

{% hint style="info" %}
코드 본체는 `Assets/CyKimExtension/` 입니다. Cursor Agent용 규칙·스킬은 `.cursor/`에 있으며, 공개 문서에서는 개념만 설명하고 프롬프트 원문은 옮기지 않습니다.
{% endhint %}

<table data-view="cards">
  <thead>
    <tr>
      <th></th>
      <th></th>
      <th data-hidden data-card-target data-type="content-ref"></th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><strong>시작하기</strong></td>
      <td>폴더 복사·패키지 설치·컴파일 확인</td>
      <td><a href="getting-started/installation.md">installation.md</a></td>
    </tr>
    <tr>
      <td><strong>런타임 유틸</strong></td>
      <td>플랫폼 판별, 포맷, 자식 탐색, UI 레이아웃</td>
      <td><a href="learn/runtime-utils.md">runtime-utils.md</a></td>
    </tr>
    <tr>
      <td><strong>에디터 도구</strong></td>
      <td>Favorite Prefab, Missing Script 제거, 단축키</td>
      <td><a href="learn/editor-tools.md">editor-tools.md</a></td>
    </tr>
    <tr>
      <td><strong>최신화 플레이북</strong></td>
      <td>다른 Unity 프로젝트에서 범용 자산 동기화</td>
      <td><a href="playbooks/sync-from-source.md">sync-from-source.md</a></td>
    </tr>
  </tbody>
</table>

## 이 문서의 청중

- Unity 프로젝트에 **CyKimExtension**을 넣고 싶은 개발자
- Inspector·프리팹·단축키 등 **에디터 편의**를 빠르게 쓰고 싶은 사람
- Cursor Agent와 함께 쓰는 **워크플로 개념**(원문 프롬프트가 아님)이 필요한 사람

## 범위

| 포함 | 제외 |
|------|------|
| 런타임 유틸, Attribute, 직렬화 구조체 | 특정 게임의 매니저·팝업·도메인 로직 |
| `Tools/` 에디터 창·단축키 | Unity CLI `-batchmode` 자동화 |
| Agent 도구 **작성 패턴** 안내 | `.cursor` 에이전트 지시문 원문 공개 |
