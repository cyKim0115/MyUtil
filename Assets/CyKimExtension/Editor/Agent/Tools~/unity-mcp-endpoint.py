#!/usr/bin/env python3
"""Unity MCP 엔드포인트를 찾아 준다. 포트를 룰/스킬/스크립트에 박지 않기 위한 단일 진입점.

UnityMCP(stdio)의 소켓 포트는 에디터 재기동·도메인 리로드마다 바뀌므로
`~/.unity-mcp/unity-mcp-status-<projecthash>.json`에서 읽는다.
같은 기기에 Unity 인스턴스가 여러 개면 파일도 여러 개다 — `project_path`로 고른다.

ai-game-developer는 프로젝트마다 주소가 다르다: `http://localhost:<포트>/p/<핀>`.
- 포트: `UserSettings/AI-Game-Developer-Config.json`의 `host` (기기별, gitignore)
- 핀: 프로젝트 루트 경로(소문자·슬래시)의 SHA-256 앞 8자리 (플러그인 ProjectIdentity V2와 같은 식)
에이전트 설정은 플러그인 창의 Configure가 **프로젝트 안 파일**에 쓴다 — 이게 정본이다.
- Claude Code: `<루트>/.mcp.json`  (+ `.claude/settings.local.json`의 `enabledMcpjsonServers`로 승인)
- Codex:       `<루트>/.codex/config.toml`
Claude Desktop·Antigravity·Cline은 전역 파일 한 칸을 모든 프로젝트가 나눠 써서, 에디터를 여러 개 띄우면
한쪽의 Configure가 다른 쪽을 "Reconfiguration Required"로 만든다. 그래서 그 셋은 쓰지 않는다.
창의 에이전트 선택(PlayerPrefs `Unity_MCP_SelectedAiAgent`)은 회사·제품명이 같은 클론끼리 공유된다.

사용 (Windows는 python, macOS·Linux는 python3):
    python unity-mcp-endpoint.py                   # 사람이 읽는 요약
    python unity-mcp-endpoint.py --json            # 기계용
    python unity-mcp-endpoint.py --check           # 위 구성이 맞는지 (어긋나면 exit 1)
    python unity-mcp-endpoint.py --fix             # Claude 쪽·에이전트 선택을 바로잡는다
    python unity-mcp-endpoint.py --project <경로>  # 프로젝트 루트를 직접 지정

프로젝트 루트는 --project, 이 파일 위치, 현재 폴더 순으로 위로 올라가며 Assets와 ProjectSettings가 함께 있는 폴더를 찾는다.
ai-game-developer를 쓰지 않는 프로젝트면 UnityMCP 포트만 의미가 있다(--check·--fix는 ai-game-developer 설정이 있어야 한다).

토큰은 절대 출력하지 않는다.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from urllib.parse import urlsplit

try:
    import tomllib  # Python 3.11+. 없으면 Codex 설정 확인만 건너뛴다.
except ImportError:
    tomllib = None

STATUS_DIR = Path.home() / ".unity-mcp"
SERVER_NAME = "ai-game-developer"
CLAUDE_JSON = Path.home() / ".claude.json"
APPDATA = Path(os.environ.get("APPDATA", Path.home() / "AppData" / "Roaming"))

# 전역 파일 한 칸을 프로젝트끼리 나눠 쓰는 에이전트. 여러 에디터를 띄우면 서로 덮어쓴다.
GLOBAL_AGENT_FILES = {
    "claude-desktop": [APPDATA / "Claude" / "claude_desktop_config.json"],
    "antigravity": [
        Path.home() / ".gemini" / "config" / "mcp_config.json",
        Path.home() / ".gemini" / "antigravity" / "mcp_config.json",
    ],
    "cline": [APPDATA / "Code" / "User" / "globalStorage" / "saoudrizwan.claude-dev" / "settings" / "cline_mcp_settings.json"],
}
PROJECT_LOCAL_AGENT = "claude-code"
SELECTED_AGENT_PREF = "Unity_MCP_SelectedAiAgent"


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


def same_path(a: str | Path, b: str | Path) -> bool:
    return os.path.normcase(os.path.normpath(str(a))) == os.path.normcase(os.path.normpath(str(b)))


def find_unity_mcp_port(root: Path) -> tuple[int | None, dict]:
    """이 프로젝트의 UnityMCP 소켓 포트. 인스턴스가 여러 개여도 project_path로 고른다."""
    if not STATUS_DIR.is_dir():
        return None, {"error": f"상태 디렉터리 없음: {STATUS_DIR}"}

    assets = (root / "Assets").resolve()
    candidates = []
    for path in sorted(STATUS_DIR.glob("unity-mcp-status-*.json")):
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        recorded = data.get("project_path", "")
        if not recorded:
            continue
        # 경로 표기가 슬래시/대소문자로 갈리므로 정규화해 비교한다.
        if Path(recorded).resolve() == assets:
            candidates.append((path, data))

    if not candidates:
        others = [p.name for p in STATUS_DIR.glob("unity-mcp-status-*.json")]
        return None, {
            "error": "이 프로젝트의 상태 파일 없음 (Unity Editor가 꺼져 있을 수 있다)",
            "found": others,
        }

    # 하트비트가 가장 최근인 것을 쓴다.
    path, data = max(candidates, key=lambda pair: pair[1].get("last_heartbeat", ""))
    return data.get("unity_port"), {
        "status_file": str(path),
        "reloading": data.get("reloading"),
        "reason": data.get("reason"),
        "last_heartbeat": data.get("last_heartbeat"),
        "unity_version": data.get("unity_version"),
    }


def find_gamedev_host(root: Path) -> tuple[str | None, dict]:
    """ai-game-developer가 실제로 듣고 있는 host. 토큰은 읽지도 돌려주지도 않는다."""
    config = root / "UserSettings" / "AI-Game-Developer-Config.json"
    if not config.is_file():
        return None, {"error": f"설정 없음: {config} (Unity의 AI Game Developer 창에서 생성된다)"}

    try:
        data = json.loads(config.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return None, {"error": f"설정을 읽을 수 없음: {exc}"}

    return data.get("host"), {
        "transportMethod": data.get("transportMethod"),
        "authOption": data.get("authOption"),
        "connectionMode": data.get("connectionMode"),
        "keepServerRunning": data.get("keepServerRunning"),
    }


def project_pin(root: Path) -> str:
    """플러그인 ProjectIdentity V2의 핀: 소문자·슬래시 정규화한 루트 경로의 SHA-256 앞 8자리."""
    normalized = str(root).replace("\\", "/").lower().rstrip("/")
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:8]


def expected_url(host: str, root: Path) -> str:
    """플러그인 Configure가 쓰는 주소. 로컬 host면 authority만 남기고 /p/<핀>을 붙인다."""
    parts = urlsplit(host)
    return f"{parts.scheme}://{parts.netloc}/p/{project_pin(root)}"


def same_endpoint(a: str | None, b: str | None) -> bool:
    """localhost와 127.0.0.1은 같은 곳이다. 표기 차이로 거짓 불일치를 내지 않는다."""
    if not a or not b:
        return False

    def norm(url: str) -> str:
        return url.strip().rstrip("/").replace("//localhost:", "//127.0.0.1:").lower()

    return norm(a) == norm(b)


def mcp_json_entry(root: Path) -> dict | None:
    path = root / ".mcp.json"
    try:
        return json.loads(path.read_text(encoding="utf-8")).get("mcpServers", {}).get(SERVER_NAME)
    except (OSError, json.JSONDecodeError):
        return None


def codex_entry(root: Path) -> dict | None:
    if tomllib is None:
        return None
    path = root / ".codex" / "config.toml"
    try:
        return tomllib.loads(path.read_text(encoding="utf-8")).get("mcp_servers", {}).get(SERVER_NAME)
    except (OSError, tomllib.TOMLDecodeError):
        return None


def local_scope_duplicates(root: Path) -> list[str]:
    """~/.claude.json의 local 스코프 등록. .mcp.json보다 우선해 플러그인이 모르는 사본이 된다."""
    try:
        data = json.loads(CLAUDE_JSON.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []
    return [key for key, proj in data.get("projects", {}).items()
            if same_path(key, root) and SERVER_NAME in proj.get("mcpServers", {})]


def mcpjson_approved(root: Path) -> bool:
    path = root / ".claude" / "settings.local.json"
    try:
        return SERVER_NAME in json.loads(path.read_text(encoding="utf-8")).get("enabledMcpjsonServers", [])
    except (OSError, json.JSONDecodeError):
        return False


def global_agent_entries() -> list[str]:
    """전역 파일 에이전트에 남은 ai-game-developer 등록. 있으면 에디터끼리 서로 덮어쓴다."""
    found = []
    for agent, paths in GLOBAL_AGENT_FILES.items():
        for path in paths:
            try:
                text = path.read_text(encoding="utf-8")
            except OSError:
                continue
            if f'"{SERVER_NAME}"' in text:
                found.append(f"{agent}: {path}")
    return found


def player_prefs_key(root: Path) -> str | None:
    """Editor PlayerPrefs 레지스트리 키. 회사·제품명이 같은 클론끼리 공유된다."""
    asset = root / "ProjectSettings" / "ProjectSettings.asset"
    try:
        text = asset.read_text(encoding="utf-8")
    except OSError:
        return None
    company = re.search(r"^\s*companyName:\s*(.+)$", text, re.MULTILINE)
    product = re.search(r"^\s*productName:\s*(.+)$", text, re.MULTILINE)
    if not company or not product:
        return None
    return rf"Software\Unity\UnityEditor\{company.group(1).strip()}\{product.group(1).strip()}"


def selected_agent(root: Path) -> tuple[str | None, str | None, str | None]:
    """(레지스트리 키, 값 이름, 선택된 에이전트 ID). Windows가 아니면 모두 None."""
    key_path = player_prefs_key(root)
    if sys.platform != "win32" or not key_path:
        return key_path, None, None
    import winreg

    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, key_path) as key:
            index = 0
            while True:
                name, value, _ = winreg.EnumValue(key, index)
                index += 1
                if name.startswith(f"String:{SELECTED_AGENT_PREF}_h"):
                    if isinstance(value, bytes):
                        value = value.rstrip(b"\0").decode("utf-8", errors="replace")
                    return key_path, name, value
    except OSError:
        pass
    return key_path, None, None


def set_selected_agent(key_path: str, value_name: str, agent_id: str) -> None:
    import winreg

    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE) as key:
        winreg.SetValueEx(key, value_name, 0, winreg.REG_BINARY, agent_id.encode("utf-8") + b"\0")


def resolve_claude(root: Path) -> str | None:
    """PATH에서 찾은 claude 실행 파일의 절대 경로.

    `claude`를 이름만으로 실행하면 Windows는 현재 폴더(여기서는 저장소 루트)부터 찾는다.
    저장소에 심은 claude.exe가 대신 실행되지 않게 상대 경로·저장소 안의 PATH 항목은 건너뛴다.
    """
    root = root.resolve()
    exts = [""]
    if sys.platform == "win32":
        exts = [e for e in os.environ.get("PATHEXT", ".EXE;.CMD;.BAT").split(";") if e]
    for entry in os.environ.get("PATH", "").split(os.pathsep):
        entry = entry.strip().strip('"')
        if not entry or not os.path.isabs(entry):
            continue
        directory = Path(entry).resolve()
        if directory == root or root in directory.parents:
            continue
        for ext in exts:
            candidate = directory / f"claude{ext}"
            if candidate.is_file():
                return str(candidate)
    return None


def registered_url(root: Path) -> str | None:
    """claude mcp get 결과에서 Claude Code가 실제로 쓰는 URL."""
    exe = resolve_claude(root)
    if exe is None:
        return None
    # .cmd 셸이 node 등을 찾을 때도 현재 폴더를 건너뛰게 한다.
    env = {**os.environ, "NoDefaultCurrentDirectoryInExePath": "1"}
    # Windows 기본 인코딩(cp949)으로 디코딩하면 출력의 체크표시·가운뎃점에서 깨진다.
    try:
        out = subprocess.run(
            [exe, "mcp", "get", SERVER_NAME],
            capture_output=True, timeout=30, check=False, cwd=root, env=env,
        ).stdout
    except (OSError, subprocess.SubprocessError):
        return None

    for line in (out or b"").decode("utf-8", errors="replace").splitlines():
        if line.strip().startswith("URL:"):
            return line.split("URL:", 1)[1].strip()
    return None


def write_json_atomic(path: Path, data: dict) -> None:
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=f".{path.name}.", suffix=".tmp")
    with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(data, ensure_ascii=False, indent=2) + "\n")
    os.replace(tmp, path)


def fix_mcp_json(root: Path, want: str) -> None:
    """플러그인 Claude Code Configure와 같은 내용(type·url만)으로 맞춘다."""
    path = root / ".mcp.json"
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        data = {}
    data.setdefault("mcpServers", {})[SERVER_NAME] = {"type": "http", "url": want}
    write_json_atomic(path, data)


def fix_local_duplicates(root: Path) -> int:
    data = json.loads(CLAUDE_JSON.read_text(encoding="utf-8"))
    removed = 0
    for key, proj in data.get("projects", {}).items():
        if same_path(key, root) and proj.get("mcpServers", {}).pop(SERVER_NAME, None) is not None:
            removed += 1
    if removed:
        write_json_atomic(CLAUDE_JSON, data)
    return removed


def fix_mcpjson_approval(root: Path) -> None:
    path = root / ".claude" / "settings.local.json"
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        data = {}
    servers = data.setdefault("enabledMcpjsonServers", [])
    if SERVER_NAME not in servers:
        servers.append(SERVER_NAME)
    write_json_atomic(path, data)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--json", action="store_true", help="기계용 JSON 출력")
    parser.add_argument("--check", action="store_true", help="구성이 맞는지만 확인")
    parser.add_argument("--fix", action="store_true", help="Claude 쪽 등록과 에이전트 선택을 바로잡는다")
    parser.add_argument("--project", metavar="PATH", help="Unity 프로젝트 루트 (생략하면 자동으로 찾음)")
    args = parser.parse_args()
    # 파이프로 받으면 Windows 기본 인코딩(cp949)으로 나가 한글이 깨진다.
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")

    root = project_root(args.project)
    if root is None:
        print("Unity 프로젝트 루트를 찾지 못했습니다 (Assets·ProjectSettings가 있는 폴더). --project로 지정하세요.")
        return 1
    unity_port, unity_meta = find_unity_mcp_port(root)
    gamedev_host, gamedev_meta = find_gamedev_host(root)
    want = expected_url(gamedev_host, root) if gamedev_host else None
    mcp_json = mcp_json_entry(root)
    codex = codex_entry(root)
    duplicates = local_scope_duplicates(root)
    approved = mcpjson_approved(root)
    globals_left = global_agent_entries()
    prefs_key, prefs_name, selected = selected_agent(root)
    current = registered_url(root) if (args.check or args.fix or args.json) else None

    report = {
        "project": str(root),
        "unity_mcp": {"port": unity_port, **unity_meta},
        "ai_game_developer": {
            "host": gamedev_host,
            "expected_url": want,
            "claude_code_mcp_json": (mcp_json or {}).get("url"),
            "claude_code_approved": approved,
            "claude_code_local_duplicates": duplicates,
            "claude_code_resolved_url": current,
            "codex_url": (codex or {}).get("url"),
            "global_agent_entries": globals_left,
            "selected_agent": selected,
            "selected_agent_prefs_key": prefs_key,
            **gamedev_meta,
        },
    }

    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print(f"project           : {root}")
        if unity_port:
            state = "reloading" if unity_meta.get("reloading") else unity_meta.get("reason", "?")
            print(f"UnityMCP port     : {unity_port}  ({state})")
        else:
            print(f"UnityMCP port     : - ({unity_meta.get('error')})")
        if want:
            print(f"ai-game-developer : {want}  "
                  f"({gamedev_meta.get('transportMethod')}, auth={gamedev_meta.get('authOption')})")
        else:
            print(f"ai-game-developer : - ({gamedev_meta.get('error')})")
        print(f"  .mcp.json       : {(mcp_json or {}).get('url', '-')}  (승인={approved})")
        print(f"  .codex          : {(codex or {}).get('url', '-')}")
        if current:
            print(f"  Claude Code 실제: {current}")
        print(f"  창 에이전트 선택: {selected or '-'}  (공유 키 HKCU\\{prefs_key})")

    if not (args.check or args.fix):
        return 0

    if not want:
        print("ai-game-developer 설정을 찾지 못해 확인할 수 없다.", file=sys.stderr)
        return 2

    problems: list[str] = []
    if not mcp_json or mcp_json.get("type") != "http" or not same_endpoint(mcp_json.get("url"), want):
        problems.append(f".mcp.json 불일치: {(mcp_json or {}).get('url')} / 기대 {want}")
    if codex is not None and (codex.get("enabled") is not True or not same_endpoint(codex.get("url"), want)):
        problems.append(f".codex/config.toml 불일치: {codex.get('url')} / 기대 {want} "
                        "→ Unity 창에서 Codex 선택 후 Configure")
    if duplicates:
        problems.append(f"~/.claude.json local 등록이 .mcp.json을 가린다: {len(duplicates)}개")
    if not approved:
        problems.append(".claude/settings.local.json enabledMcpjsonServers에 없음 (.mcp.json 승인 대기)")
    if selected in GLOBAL_AGENT_FILES:
        problems.append(f"창 에이전트 선택이 전역 파일 에이전트({selected}) — 클론끼리 Reconfigure 핑퐁")
    for entry in globals_left:
        problems.append(f"전역 파일에 {SERVER_NAME} 남음 (수동 삭제): {entry}")

    if not problems:
        print("OK: 플러그인 기대 주소와 에이전트 설정이 모두 일치한다.")
        return 0

    for problem in problems:
        print(f"MISMATCH: {problem}")
    if not args.fix:
        print("--fix 를 주면 Claude 쪽 등록과 에이전트 선택을 바로잡는다.")
        return 1

    if not mcp_json or not same_endpoint(mcp_json.get("url"), want):
        fix_mcp_json(root, want)
        print(f"고침: .mcp.json → {want}")
    if duplicates:
        print(f"고침: local 중복 {fix_local_duplicates(root)}개 제거")
    if not approved:
        fix_mcpjson_approval(root)
        print("고침: enabledMcpjsonServers에 추가")
    if selected in GLOBAL_AGENT_FILES and prefs_name:
        set_selected_agent(prefs_key, prefs_name, PROJECT_LOCAL_AGENT)
        print(f"고침: 창 에이전트 선택 {selected} → {PROJECT_LOCAL_AGENT} (열린 창은 다시 열어야 반영)")
    print("Claude Code는 다음 세션부터 새 등록으로 붙는다. Codex·전역 파일 항목은 위 안내대로 처리한다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
