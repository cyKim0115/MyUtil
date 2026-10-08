#!/usr/bin/env python3
"""Unity 에디터에 떠 있는 네이티브 모달(씬 리로드 등)을 찾고, 안전할 때 버튼을 누른다.

모달이 떠 있으면 에디터 메인 스레드가 막혀 UnityMCP·ai-game-developer 호출이 전부 타임아웃된다.
MCP 서버도 그 메인 스레드 안에서 돌기 때문에 MCP로는 풀 수 없고, 바깥 프로세스(이 스크립트)가
Win32 API로 대화상자 창을 찾아 버튼에 클릭 메시지를 보낸다. 포커스·마우스를 쓰지 않는다.

씬을 저장했는지는 에디터의 AgentSceneDirtyBeacon이 Temp/AgentSceneDirty.json에 적어 둔 값으로 판단한다.
모달이 뜨면 MCP로 물을 수 없어서 미리 적어 둔 파일을 읽는 것이다.

사용 (경로는 설치 위치에 맞춘다. 예: .claude/tools/, Assets/CyKimExtension/Editor/Agent/Tools~/):
    python unity-modal.py                    # 이 프로젝트 Unity의 모달·진행 창 목록 + 씬 dirty 상태
    python unity-modal.py --reload           # 씬 리로드 모달이면 Reload (열린 씬이 모두 저장 상태일 때만)
    python unity-modal.py --reload --force   # dirty여도 Reload (저장 안 된 씬 편집을 버린다. 사용자 확인 후)
    python unity-modal.py --click "Keep Changes" [--title 일부]   # 다른 모달 버튼 (사용자 확인 후)
    python unity-modal.py --wait 30 --reload # 모달이 뜰 때까지 최대 30초 기다린 뒤 처리
    python unity-modal.py --json             # 기계용 출력
    python unity-modal.py --all              # 디버그: Unity 프로세스의 보이는 최상위 창 전부
    python unity-modal.py --project <경로>   # 프로젝트 루트를 직접 지정

프로젝트 루트는 --project, 이 파일 위치, 현재 폴더 순으로 위로 올라가며 Assets와 ProjectSettings가 함께 있는 폴더를 찾는다.

종료 코드: 0 = 모달 없음 또는 처리 완료, 2 = 모달이 남아 판단이 필요함, 1 = 오류(Unity 창 못 찾음 등).

씬 리로드 모달에서 Ignore는 누르지 않는다. 메모리의 옛 씬이 남아 있다가 저장되는 순간 git으로 받은 변경을 덮어쓴다.
"""

from __future__ import annotations

import argparse
import ctypes
import json
import sys
import time
from ctypes import wintypes
from pathlib import Path

BEACON_RELATIVE_PATH = Path("Temp") / "AgentSceneDirty.json"
SCENE_RELOAD_MARKER = "modified externally"
RELOAD_BUTTON = "Reload"
EDITOR_WINDOW_CLASS = "UnityContainerWndClass"
PROGRESS_BAR_CLASS = "msctls_progress32"

BM_CLICK = 0x00F5
WM_COMMAND = 0x0111
BN_CLICKED = 0
SMTO_ABORTIFHUNG = 0x0002

user32 = ctypes.WinDLL("user32", use_last_error=True)
WNDENUMPROC = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

user32.EnumWindows.argtypes = [WNDENUMPROC, wintypes.LPARAM]
user32.EnumChildWindows.argtypes = [wintypes.HWND, WNDENUMPROC, wintypes.LPARAM]
user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user32.GetClassNameW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user32.IsWindowVisible.argtypes = [wintypes.HWND]
user32.IsWindow.argtypes = [wintypes.HWND]
user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
user32.GetDlgCtrlID.argtypes = [wintypes.HWND]
user32.SendMessageTimeoutW.argtypes = [
    wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM,
    wintypes.UINT, wintypes.UINT, ctypes.POINTER(ctypes.c_size_t),
]
user32.SendMessageTimeoutW.restype = wintypes.LPARAM
user32.PostMessageW.argtypes = [wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]


def is_unity_project(path: Path) -> bool:
    return (path / "Assets").is_dir() and (path / "ProjectSettings").is_dir()


def project_root(override: str | None = None) -> Path | None:
    """--project, 이 파일 위치, 현재 폴더 순으로 위로 올라가며 Unity 프로젝트 루트를 찾는다.

    .claude/tools/ 에 두든 Assets/.../Tools~/ 에 두든 같은 파일이 동작하게 한다.
    """
    if override:
        path = Path(override).resolve()
        return path if is_unity_project(path) else None
    for start in (Path(__file__).resolve().parent, Path.cwd().resolve()):
        for path in (start, *start.parents):
            if is_unity_project(path):
                return path
    return None


def window_text(hwnd: int) -> str:
    length = user32.GetWindowTextLengthW(hwnd)
    buf = ctypes.create_unicode_buffer(length + 1)
    user32.GetWindowTextW(hwnd, buf, length + 1)
    return buf.value


def class_name(hwnd: int) -> str:
    buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(hwnd, buf, 256)
    return buf.value


def window_pid(hwnd: int) -> int:
    pid = wintypes.DWORD()
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    return pid.value


def top_level_windows() -> list[int]:
    result: list[int] = []

    def on_window(hwnd, _):
        if user32.IsWindowVisible(hwnd):
            result.append(hwnd)
        return True

    user32.EnumWindows(WNDENUMPROC(on_window), 0)
    return result


def child_windows(hwnd: int) -> list[int]:
    result: list[int] = []

    def on_child(child, _):
        result.append(child)
        return True

    user32.EnumChildWindows(hwnd, WNDENUMPROC(on_child), 0)
    return result


def read_beacon(root: Path) -> dict | None:
    """에디터가 적어 둔 dirty 상태. 쓰는 중에 읽으면 깨질 수 있어 한 번 더 읽는다."""
    path = root / BEACON_RELATIVE_PATH
    for _ in range(2):
        if not path.is_file():
            return None
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            time.sleep(0.2)
    return None


def find_editor_pids(root: Path, windows: list[int], beacon: dict | None) -> list[int]:
    """비컨의 pid가 살아 있으면 그것을, 아니면 메인 창 제목 「<프로젝트 폴더명> - 」으로 찾는다.

    제목 앞부분은 Preferences의 useProjectPathInTitle이 켜져 있으면 폴더 경로가 된다.
    「MyGame-Copy」 같은 다른 폴더의 에디터와 섞이지 않도록 「 - 」까지 맞춘다.
    """
    pids_with_windows = {window_pid(h) for h in windows}
    if beacon and beacon.get("Pid") in pids_with_windows:
        return [beacon["Pid"]]

    prefixes = (f"{root.name} - ", f"{root} - ".lower())
    found: list[int] = []
    for hwnd in windows:
        title = window_text(hwnd)
        if title.startswith(prefixes[0]) or title.lower().startswith(prefixes[1]):
            pid = window_pid(hwnd)
            if pid not in found:
                found.append(pid)
    return found


def describe_window(hwnd: int) -> dict:
    children = child_windows(hwnd)
    buttons = []
    texts = []
    has_progress_bar = False
    for child in children:
        cls = class_name(child).lower()
        text = window_text(child)
        if cls == PROGRESS_BAR_CLASS:
            has_progress_bar = True
        elif cls == "button" and text:
            buttons.append({"hwnd": child, "text": text.replace("&", "")})
        elif cls == "static" and text.strip():
            texts.append(text.strip())
    return {
        "hwnd": hwnd,
        "title": window_text(hwnd),
        "class": class_name(hwnd),
        "buttons": buttons,
        "texts": texts,
        "has_progress_bar": has_progress_bar,
    }


def collect(root: Path) -> dict:
    windows = top_level_windows()
    beacon = read_beacon(root)
    pids = find_editor_pids(root, windows, beacon)
    dialogs, progress = [], []
    for hwnd in windows:
        if window_pid(hwnd) not in pids:
            continue
        info = describe_window(hwnd)
        if info["has_progress_bar"]:
            # 「Hold on...」「Reloading Domain」 같은 진행 창도 Cancel·Skip Transcoding 버튼이 있다. 누르지 않고 기다린다.
            progress.append(info)
        elif info["buttons"]:
            dialogs.append(info)
        elif info["title"] and info["class"] != EDITOR_WINDOW_CLASS:
            # 메인 창·떠 있는 Game/Inspector 창은 UnityContainerWndClass다. 그 밖의 제목 있는 창은 진행 표시로 본다.
            progress.append(info)
    beacon_note = None
    if not beacon:
        beacon_note = f"{BEACON_RELATIVE_PATH.as_posix()} 없음 — 에디터가 AgentSceneDirtyBeacon을 컴파일하지 않았거나 도메인 리로드 중"
    elif beacon.get("Pid") not in pids:
        beacon_note = "기록의 pid가 이 에디터가 아님"
        beacon = None
    elif not beacon_script_exists(beacon):
        # 브랜치 전환 등으로 스크립트가 빠지면 에디터는 더 적지 않는데 옛 기록은 남는다.
        beacon_note = "AgentSceneDirtyBeacon 스크립트가 디스크에 없음 — 기록이 낡았을 수 있음"
        beacon = None
    return {
        "pids": pids,
        "dialogs": dialogs,
        "progress": progress,
        "beacon": beacon,
        "beacon_note": beacon_note,
    }


def beacon_script_exists(beacon: dict) -> bool:
    script = beacon.get("ScriptPath")
    return bool(script) and Path(script).is_file()


def is_scene_reload(dialog: dict) -> bool:
    haystack = " ".join([dialog["title"], *dialog["texts"]]).lower()
    has_reload = any(b["text"] == RELOAD_BUTTON for b in dialog["buttons"])
    return SCENE_RELOAD_MARKER in haystack and has_reload


def dirty_items(beacon: dict) -> list[str]:
    items = [s.get("Path") or "(Untitled)" for s in beacon.get("Scenes", []) if s.get("IsDirty")]
    if beacon.get("PrefabStagePath") and beacon.get("PrefabStageDirty"):
        items.append(f"Prefab Mode: {beacon['PrefabStagePath']}")
    return items


def click_button(dialog_hwnd: int, button_hwnd: int) -> bool:
    """BM_CLICK을 보내고 창이 닫히는지 본다. 안 닫히면 대화상자에 WM_COMMAND(BN_CLICKED)를 직접 보낸다.

    BM_CLICK은 대화상자가 비활성일 때 무시될 수 있다고 문서에 적혀 있어서 두 번째 경로를 둔다.
    SendMessage 대신 SendMessageTimeout을 써서 에디터가 멈춰 있어도 이 스크립트는 멈추지 않는다.
    """
    result = ctypes.c_size_t()
    user32.SendMessageTimeoutW(button_hwnd, BM_CLICK, 0, 0, SMTO_ABORTIFHUNG, 3000, ctypes.byref(result))
    if wait_closed(dialog_hwnd, 3.0):
        return True
    control_id = user32.GetDlgCtrlID(button_hwnd)
    user32.PostMessageW(dialog_hwnd, WM_COMMAND, (BN_CLICKED << 16) | (control_id & 0xFFFF), button_hwnd)
    return wait_closed(dialog_hwnd, 3.0)


def wait_closed(hwnd: int, seconds: float) -> bool:
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if not user32.IsWindow(hwnd) or not user32.IsWindowVisible(hwnd):
            return True
        time.sleep(0.1)
    return False


def print_state(state: dict) -> None:
    print(f"Unity 에디터 pid: {', '.join(map(str, state['pids']))}")
    beacon = state["beacon"]
    if beacon is None:
        print(f"씬 dirty 상태: 모름 ({state['beacon_note']})")
    else:
        dirty = dirty_items(beacon)
        play = " · Play 모드" if beacon.get("IsPlaying") else ""
        print(f"씬 dirty 상태{play}: " + (", ".join(dirty) if dirty else "모두 저장됨"))
    if not state["dialogs"]:
        print("모달 없음")
    for d in state["dialogs"]:
        kind = " [씬 리로드]" if is_scene_reload(d) else ""
        print(f"모달{kind}: 「{d['title']}」 버튼={[b['text'] for b in d['buttons']]}")
        for t in d["texts"]:
            print(f"    {t}")
    for p in state["progress"]:
        print(f"진행 창(누르지 말고 기다림): 「{p['title']}」")


def to_json(state: dict) -> str:
    def strip(d: dict) -> dict:
        return {
            "title": d["title"],
            "class": d["class"],
            "buttons": [b["text"] for b in d["buttons"]],
            "texts": d["texts"],
            "scene_reload": is_scene_reload(d),
        }

    return json.dumps(
        {
            "pids": state["pids"],
            "beacon": state["beacon"],
            "beacon_note": state["beacon_note"],
            "dialogs": [strip(d) for d in state["dialogs"]],
            "progress": [p["title"] for p in state["progress"]],
        },
        ensure_ascii=False,
        indent=2,
    )


def handle_reload(state: dict, force: bool) -> int:
    targets = [d for d in state["dialogs"] if is_scene_reload(d)]
    if not targets:
        print("씬 리로드 모달 없음")
        return 2 if state["dialogs"] else 0

    beacon = state["beacon"]
    if not force:
        if beacon is None or not beacon.get("Scenes"):
            print(f"멈춤: 씬 저장 여부를 알 수 없어 Reload를 누르지 않았습니다 ({state['beacon_note'] or '열린 씬 없음'}). 사용자에게 확인하거나 --force.")
            return 2
        if beacon.get("IsPlaying"):
            print("멈춤: Play 모드 중이라 Reload를 누르지 않았습니다. 사용자에게 확인하거나 --force.")
            return 2
        dirty = dirty_items(beacon)
        if dirty:
            print("멈춤: 저장 안 된 편집이 있어 Reload를 누르지 않았습니다: " + ", ".join(dirty))
            print("  Reload = 이 편집을 버림 / Ignore = 이후 저장 때 git으로 받은 씬 변경을 덮어씀. 사용자에게 물어보세요.")
            return 2

    ok = True
    for d in targets:
        button = next(b for b in d["buttons"] if b["text"] == RELOAD_BUTTON)
        if click_button(d["hwnd"], button["hwnd"]):
            print(f"Reload 누름: 「{d['title']}」")
        else:
            print(f"실패: 「{d['title']}」 창이 닫히지 않았습니다. 사용자가 직접 눌러야 합니다.")
            ok = False
    return 0 if ok else 2


def handle_click(state: dict, text: str, title_filter: str | None) -> int:
    candidates = [
        d for d in state["dialogs"]
        if any(b["text"] == text for b in d["buttons"])
        and (title_filter is None or title_filter.lower() in d["title"].lower())
    ]
    if not candidates:
        print(f"「{text}」 버튼이 있는 모달 없음")
        return 2 if state["dialogs"] else 0
    if len(candidates) > 1:
        print(f"「{text}」 버튼이 있는 모달이 {len(candidates)}개입니다. --title로 좁혀 주세요.")
        return 2
    d = candidates[0]
    button = next(b for b in d["buttons"] if b["text"] == text)
    if click_button(d["hwnd"], button["hwnd"]):
        print(f"「{text}」 누름: 「{d['title']}」")
        return 0
    print(f"실패: 「{d['title']}」 창이 닫히지 않았습니다.")
    return 2


def list_all(pids: list[int]) -> None:
    for hwnd in top_level_windows():
        if window_pid(hwnd) in pids:
            print(f"{hwnd} | {class_name(hwnd)} | {window_text(hwnd)}")


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    action = parser.add_mutually_exclusive_group()
    action.add_argument("--reload", action="store_true", help="씬 리로드 모달이면 Reload")
    action.add_argument("--click", metavar="TEXT", help="이 글자의 버튼을 누름")
    action.add_argument("--all", action="store_true", help="디버그: 보이는 최상위 창 전부")
    parser.add_argument("--force", action="store_true", help="--reload에서 dirty·상태 모름이어도 누름")
    parser.add_argument("--title", help="--click 대상 모달 제목 일부")
    parser.add_argument("--wait", type=float, default=0, metavar="SEC", help="모달이 뜰 때까지 최대 SEC초 기다림")
    parser.add_argument("--json", action="store_true", help="기계용 출력 (목록 모드)")
    parser.add_argument("--project", metavar="PATH", help="Unity 프로젝트 루트 (생략하면 자동으로 찾음)")
    args = parser.parse_args()

    root = project_root(args.project)
    if root is None:
        print("Unity 프로젝트 루트를 찾지 못했습니다 (Assets·ProjectSettings가 있는 폴더). --project로 지정하세요.")
        return 1
    deadline = time.monotonic() + args.wait
    state = collect(root)
    while not state["dialogs"] and time.monotonic() < deadline:
        time.sleep(0.5)
        state = collect(root)

    if not state["pids"]:
        print(f"Unity 에디터 창을 찾지 못했습니다 (프로젝트 {root.name}).")
        return 1

    if args.all:
        list_all(state["pids"])
        return 0
    if args.reload:
        return handle_reload(state, args.force)
    if args.click:
        return handle_click(state, args.click, args.title)

    if args.json:
        print(to_json(state))
    else:
        print_state(state)
    return 2 if state["dialogs"] else 0


if __name__ == "__main__":
    sys.exit(main())
