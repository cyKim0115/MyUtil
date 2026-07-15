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
- Keep screenshots in the same folder

## Document structure

1. `# {도구이름} 사용 가이드`
2. 개요
3. 접근 방법
4. UI 미리보기
5. UI 구성
6. 주요 기능
7. 사용 시나리오
8. 내부 동작 원리
9. 주의사항
10. 관련 파일
11. 버전 히스토리

## Writing rules

- Write in Korean.
- Separate major sections with `---`.
- Bold UI element names and button labels.
- Write for handoff: assume the reader did not build the tool.

## Checklist

1. Confirm the menu path such as `Tools -> Prefab -> ...`.
2. Capture the UI layout and each important control.
3. Include 2-3 realistic usage scenarios.
4. Mention related scripts under `Assets/CyKimExtension/Editor/`.
