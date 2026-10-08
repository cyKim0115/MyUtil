---
name: korean-git-commit
description: Draft Korean git commit messages for MyUtil in `{영역} - {구체적 변경 내용}` format, and commit/push when the user ends a request with +커푸. Use when the user asks to commit changes, wants help writing a commit message in this repository, or writes +커푸 / 커밋 / 푸시.
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

## Safety protocol

1. `git status` / `git diff --stat` / `git log -15 --oneline`을 **병렬**로 확인. 전체 `git diff`는 기본으로 읽지 않는다 — 아래 Cheap inspect
2. 시크릿(`.env`, `credentials.json` 등)은 스테이징하지 않음
3. `.cursor/rules/local/` 같은 로컬 오버라이드에 하드 제외 경로가 있으면 기본 스테이징 금지 (`전체 커밋`·`+커푸`만으로는 풀리지 않음)
4. 이번 작업 관련 파일만 경로를 지정해 stage 후 커밋
5. 커밋 후 `git status`로 확인. 제외 경로를 남겼으면 **제외 경로를 보고**
6. 사용자가 요청하지 않았고 `+커푸`도 없으면 push하지 않음
7. `--force` push, `--no-verify` 금지 (사용자가 명시하지 않는 한)
8. git config 변경 금지

## Cheap inspect (토큰)

비용은 `git` 실행이 아니라 **diff 본문을 컨텍스트에 올리는 것**에서 난다.

| 읽기 | 언제 |
|---|---|
| `git status` + `git diff --stat` + `git log -15 --oneline` | **기본.** 메시지 작성에 대부분 충분 |
| `git diff -- path` (소스·문서만, 경로 한정) | path/stat만으로 메시지가 모호할 때 |
| 직렬화·생성 에셋 본문 (프리팹, lockfile, 생성 YAML 등) | 묶음 판단이 애매할 때만. `--stat`으로 충분하면 열지 않음 |

관심사가 섞여 있거나 「전체 커밋」「단계별/비슷한 것끼리 커밋」이면 관심사별로 나눠 커밋한다(`grouped-git-commit` 스킬이 있으면 그 절차). 「전체」는 빠짐없이 올리라는 뜻이지 한 커밋으로 몰아넣으라는 뜻이 아니다.

## `+커푸` (커밋 + 푸시)

사용자 지시 **끝**에 `+커푸`가 있으면 작업을 마친 뒤:

1. 이번 작업 변경만 확인·스테이징
2. 위 형식으로 커밋
3. `git push` (필요 시 `-u origin HEAD`). **force push 금지**
4. 커밋 해시·푸시 결과를 짧게 안내

## Windows (PowerShell) 메시지

```powershell
git commit -m @"
영역 - 구체적 변경 내용

본문이 있으면 여기에
"@
```

## Examples

Good:
- `유틸 - PlatformUtil 빌드 타입 판별 추가`
- `에디터 - Favorite Prefab Missing Script 제거 도구 추가`
- `룰 - Cursor 에디터 워크플로 규칙 추가`

Bad:
- `PlatformUtil 빌드 타입 판별을 추가하고 Favorite Prefab을 정리한다.`
- `Add build type check`
- `WIP`

## Drafting checklist

1. Read `.cursor/rules/korean-git-commit.mdc` (and any `.cursor/rules/local/` overrides) before writing the subject.
2. Review staged and unstaged changes together with `--stat`. Open a path-scoped source/doc diff only when the message is ambiguous.
3. If a local hard-excluded path changed, confirm the user explicitly asked to include it; otherwise leave it unstaged.
4. Pick one area label for the main change.
5. Describe why the change exists, not just the edited symbols.
6. Reject the message if it ends with `한다`, `합니다`, or `됩니다`.
7. If `+커푸`, push after a successful commit.
