---
name: editor-tool-doc-writing
description: Write Korean Markdown documentation for Unity editor tools in MyUtil. Use when documenting an editor window or Tools menu workflow under `Doc/`.
disable-model-invocation: true
---

# Editor Tool Doc Writing

Use this skill when creating or updating documentation for a Unity editor tool in this repository.

## File placement

- Save docs under `Doc/{도구이름}/`
- Use `{도구이름}Window.md` or `{도구이름}.md`
- Keep screenshots in the same folder, typically `{도구이름} 사진.png`

## Document structure

1. `# {도구이름} 사용 가이드`
2. 개요
3. 접근 방법
4. UI 미리보기
5. UI 구성
6. 주요 기능
7. 사용 시나리오
8. 내부 동작 원리
9. 데이터 구조
10. 주의사항
11. 관련 파일
12. 버전 히스토리 — 비개발자에게 배포하는 가이드에는 넣지 않고 날짜 하나만 둔다

비개발자 독자용 가이드는 8~9절과 클래스·경로 나열을 빼거나 쉬운 말로 줄인다.

## Writing rules

- Write in Korean.
- Separate major sections with `---`.
- Bold UI element names and button labels.
- Write for handoff: assume the reader did not build the tool.

## Checklist

1. Confirm the menu path such as `Tools -> Prefab -> ...`. Agent 전용 도구(`Tools/Agent/...`, validate `false`)는 메뉴 대신 `Type.Method()` 호출을 적는다.
2. Capture the UI layout and each important control.
3. Include 2-3 realistic usage scenarios.
4. Mention related scripts under `Assets/CyKimExtension/Editor/`.
5. 같은 도구를 다룬 다른 사본(배포용 가이드, 공유 문서 등)이 있으면 함께 갱신할지 사용자에게 확인한다.
