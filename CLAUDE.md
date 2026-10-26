# MyUtil

개인 재사용 Unity 유틸·에디터 도구 라이브러리입니다. 코드는 `Assets/CyKimExtension/`에 있고, 게임 도메인 코드는 넣지 않습니다.

에이전트 룰·스킬의 정본은 `.cursor/`입니다. Claude Code는 `.cursor`를 스스로 읽지 않으니, 항상 적용할 룰을 아래에서 불러옵니다.

@.cursor/rules/myutil-overview.mdc
@.cursor/rules/unity-assets.mdc
@.cursor/rules/unity-editor-agent-workflow.mdc
@.cursor/rules/unity-agent-editor-tools.mdc
@.cursor/rules/korean-git-commit.mdc

`Assets/CyKimExtension/**/*.cs`를 고치기 전에는 `.cursor/rules/unity-csharp-conventions.mdc`를 읽습니다.

## 스킬 (해당 요청이 오면 Read)

| 요청 | 스킬 |
|---|---|
| `최신화`·동기화·sync, 관여 레포 확장 스캔 | `.cursor/skills/project-workflows/sync-from-source/SKILL.md` + 같은 폴더 `sync-manifest.md` |
| 커밋 메시지, `+커푸` | `.cursor/skills/project-workflows/korean-git-commit/SKILL.md` |
| 녹화·PNG 시퀀스·목표 해상도 정지컷 | `.cursor/skills/project-workflows/unity-recorder/SKILL.md` |
| Agent 전용 Editor 도구 추가 | `.cursor/skills/project-workflows/agent-editor-tools/SKILL.md` |
| 에디터 도구 문서(`Doc/`) | `.cursor/skills/project-workflows/editor-tool-doc-writing/SKILL.md` |
| 멀티 이미터 파티클 루트 | `.cursor/skills/project-workflows/particle-effect-controller/SKILL.md` |

웹훅 보고는 전역 스킬 `webhook-report`로 셸에서 직접 보냅니다. Unity MCP를 거치지 않습니다.

## 이 저장소에서 확인할 때

- `.env`(gitignore)에 최신화 소스 경로가 있습니다. 값이나 다른 프로젝트 이름을 커밋·스킬·문서에 적지 않습니다.
- `Assets/CyKimExtension/Editor/Animation`은 서브모듈(AnimationClipPathRemap)입니다. 고칠 일이 있으면 상류 저장소에서 고치고 포인터만 올립니다.
- 이 프로젝트에는 Unity MCP 패키지가 없어, 에디터를 열어도 에이전트가 MCP로 붙지 못합니다. 컴파일은 사용자가 에디터 콘솔에서 확인하거나 따로 마련한 방법으로 확인하고, 확인하지 못했으면 그렇다고 보고합니다.
