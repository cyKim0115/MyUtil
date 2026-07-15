---
name: korean-git-commit
description: Draft Korean git commit messages for MyUtil in `{영역} - {구체적 변경 내용}` format. Use when the user asks to commit changes or wants help writing a commit message in this repository.
---

# Korean Git Commit

Use this skill whenever preparing a commit message for this repository.

Always-applied rule: `.cursor/rules/korean-git-commit.mdc`

## Format

```text
{영역} - {구체적 변경 내용}
```

## Rules

- Write the subject in Korean.
- Keep the first line focused on the user-visible or architectural reason for the change.
- Pick a clear area label such as `유틸`, `에디터`, `UI`, `문서`, `패키지`, `룰`, `스킬`.
- Prefer one concise subject line. Add a body only when extra context is helpful.
- Use a terse noun/verb-phrase tone. End with compact labels such as `구현`, `반영`, `정리`, `추가`, `수정`.
- Do **not** use sentence-style endings like `~한다`, `~합니다`, `~됩니다`.
- Do **not** copy older sentence-style commits from `git log`; follow this rule even when recent history is inconsistent.

## Examples

Good:
- `유틸 - PlatformUtil 빌드 타입 판별 추가`
- `에디터 - Favorite Prefab Missing Script 제거 도구 추가`
- `룰 - Cursor 에디터 워크플로 규칙 추가`

Bad:
- `PlatformUtil 빌드 타입 판별을 추가하고 Favorite Prefab을 정리한다.`

## Drafting checklist

1. Read `.cursor/rules/korean-git-commit.mdc` before writing the subject.
2. Review staged and unstaged changes together.
3. Pick one area label for the main change.
4. Describe why the change exists, not just the edited symbols.
5. Reject the message if it ends with `한다`, `합니다`, or `됩니다`.
