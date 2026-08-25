# AnimationClip Path Remap — MyUtil

## 위치

이 도구는 **서브모듈**로 포함됩니다.

| 항목 | 값 |
|---|---|
| 경로 | `Assets/CyKimExtension/Editor/Animation` |
| 원격 | https://github.com/cyKim0115/AnimationClipPathRemap |
| 메뉴 | `Tools/Animation/Clip Path Remap` |

클론 후:

```bash
git submodule update --init --recursive
```

사용 가이드·API는 서브모듈 `Doc/AnimationClipPathRemapWindow.md` 및 `README.md`를 본다.

## 원본 이식

소스 프로젝트의 `Doc/AnimationClipPathRemap/MyUtil-Handoff.md`에서 MyUtil로 옮긴 뒤, 독립 저장소로 분리했다.

## 남은 작업 (소비 프로젝트)

- [ ] 소스 프로젝트 쪽 중복 스크립트 제거 또는 동일 서브모듈로 단일 소스 정리
- [ ] MyUtil Editor 첫 import 후 `.meta`를 서브모듈에 커밋 (GUID 고정)
