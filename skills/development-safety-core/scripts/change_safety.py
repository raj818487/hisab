#!/usr/bin/env python3
"""Deterministic Graphify and regression gates installed into target projects."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
from datetime import datetime, timezone

import impact_analysis


IGNORED_DIRS = {
    ".git", ".ai-work", ".claude", ".cursor", ".github", ".venv", "venv",
    "node_modules", "bin", "obj", "dist", "build", "coverage", "graphify-out",
    "__pycache__",
}
SOURCE_SUFFIXES = {
    ".py", ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs", ".cs", ".java",
    ".go", ".rs", ".rb", ".php", ".kt", ".kts", ".swift", ".vue", ".svelte",
    ".sql", ".prisma", ".json", ".yaml", ".yml", ".toml", ".xml", ".csproj",
    ".fsproj", ".vbproj", ".sln", ".props", ".targets",
}
PUBLIC_PATTERNS = [
    re.compile(r"^\s*(?:export\s+)?(?:async\s+)?(?:function|class|interface|type|enum)\s+([A-Za-z_$][\w$]*)"),
    re.compile(r"^\s*(?:public\s+)(?:static\s+)?(?:async\s+)?[\w<>,?\[\].]+\s+([A-Za-z_$][\w$]*)\s*\("),
    re.compile(r"^\s*(?:def|class)\s+([A-Za-z_][\w]*)\s*(?:\(|:)"),
    re.compile(r"^\s*(?:func|type)\s+([A-Za-z_][\w]*)"),
]


def run(cmd: list[str], root: Path, check: bool = False) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(cmd, cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and result.returncode:
        detail = (result.stdout + result.stderr).strip()
        raise RuntimeError(detail or f"Command failed: {shlex.join(cmd)}")
    return result


def project_root() -> Path:
    override = os.environ.get("DEV_ASSISTANT_PROJECT_ROOT")
    if override:
        return Path(override).resolve()
    current = Path.cwd().resolve()
    for candidate in (current, *current.parents):
        if (candidate / ".git").exists() or (candidate / "dev-assistant.json").exists():
            return candidate
    return current


def load_config(root: Path) -> dict:
    path = root / "dev-assistant.json"
    if not path.exists():
        return {}
    with path.open(encoding="utf-8-sig") as handle:
        return json.load(handle)


def graph_path(root: Path, config: dict) -> Path:
    configured = config.get("graphPath")
    return Path(configured).resolve() if configured else root / "graphify-out" / "graph.json"


def newest_source_mtime(root: Path) -> tuple[float, str]:
    newest = (0.0, "")
    for path in root.rglob("*"):
        if not path.is_file() or any(part in IGNORED_DIRS for part in path.relative_to(root).parts):
            continue
        if path.suffix.lower() not in SOURCE_SUFFIXES:
            continue
        stamp = path.stat().st_mtime
        if stamp > newest[0]:
            newest = (stamp, path.relative_to(root).as_posix())
    return newest


def refresh_graph(root: Path) -> None:
    if not shutil.which("graphify"):
        raise RuntimeError("graphify is not installed or not available on PATH")
    primary = run(["graphify", "extract", ".", "--code-only", "--no-gitignore"], root)
    if primary.returncode:
        fallback = run(["graphify", "extract", ".", "--code-only"], root)
        if fallback.returncode:
            raise RuntimeError((primary.stdout + primary.stderr + fallback.stdout + fallback.stderr).strip())


def verify_graph(root: Path, refresh: bool) -> dict:
    config = load_config(root)
    graph = graph_path(root, config)
    newest_stamp, newest_file = newest_source_mtime(root)
    stale = not graph.exists() or graph.stat().st_mtime + 1 < newest_stamp
    if stale and refresh:
        refresh_graph(root)
        stale = not graph.exists() or graph.stat().st_mtime + 1 < newest_stamp
    if not graph.exists():
        raise RuntimeError(f"Graphify graph not found: {graph}")
    if stale:
        raise RuntimeError(f"Graphify graph is stale; newest source is {newest_file}")
    with graph.open(encoding="utf-8") as handle:
        data = json.load(handle)
    links = impact_analysis.links(data)
    settings = impact_analysis.settings(root)
    if settings["requireGraphLinks"] and not links:
        raise RuntimeError("Graphify graph has no dependency links; impact analysis is unverified")
    return {
        "status": "pass",
        "graph": str(graph),
        "nodes": len(data.get("nodes", [])),
        "edges": len(links),
        "newestSource": newest_file,
    }


def git_paths(root: Path, args: list[str]) -> set[str]:
    result = run(["git", *args], root, check=True)
    return {line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()}


def sha256(path: Path) -> str | None:
    if not path.is_file():
        return None
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def public_symbols(path: Path) -> list[str]:
    if not path.is_file():
        return []
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return []
    symbols = []
    for line in lines:
        for pattern in PUBLIC_PATTERNS:
            match = pattern.match(line)
            if match:
                symbols.append(match.group(1))
                break
    return sorted(set(symbols))


def graph_query(root: Path, graph: Path, target: str) -> str:
    if not shutil.which("graphify"):
        return "graphify unavailable"
    result = run(["graphify", "query", f"{target} callers imports depends tests", "--graph", str(graph)], root)
    return (result.stdout + result.stderr).strip()[:12000]


def baseline_path(root: Path) -> Path:
    return root / ".ai-work" / "change-safety-baseline.json"


def capture(root: Path, files: list[str]) -> dict:
    graph_info = verify_graph(root, refresh=False)
    normalized = sorted({Path(item).as_posix().lstrip("./") for item in files})
    if not normalized:
        raise RuntimeError("At least one target file is required")
    impact = impact_analysis.verify_complete(root, normalized)
    graph = Path(graph_info["graph"])
    entries = {}
    for relative in normalized:
        target = root / relative
        entries[relative] = {
            "exists": target.exists(),
            "sha256": sha256(target),
            "publicSymbols": public_symbols(target),
            "graphContext": graph_query(root, graph, relative),
        }
    baseline = {
        "version": 1,
        "capturedAt": datetime.now(timezone.utc).isoformat(),
        "targets": normalized,
        "initialDirtyFiles": sorted(current_changed_files(root)),
        "graph": graph_info,
        "files": entries,
        "impact": impact,
    }
    output = baseline_path(root)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(baseline, indent=2), encoding="utf-8")
    return {"status": "pass", "baseline": str(output), "targets": normalized}


def is_ignored_path(relative: str) -> bool:
    return any(part in IGNORED_DIRS for part in Path(relative).parts)


def current_changed_files(root: Path) -> set[str]:
    tracked = git_paths(root, ["diff", "--name-only", "HEAD"])
    untracked = git_paths(root, ["ls-files", "--others", "--exclude-standard"])
    return {path for path in tracked | untracked if not is_ignored_path(path)}


def configured_checks(config: dict) -> list[tuple[str, list[str]]]:
    raw = config.get("requiredChecks", {})
    checks = []
    if isinstance(raw, dict):
        for name, command in raw.items():
            if isinstance(command, str) and command.strip():
                checks.append((name, shlex.split(command, posix=os.name != "nt")))
            elif isinstance(command, list) and command:
                checks.append((name, [str(part) for part in command]))
    return checks


def run_checks(root: Path) -> dict:
    config = load_config(root)
    checks = configured_checks(config)
    results = []
    for name, command in checks:
        result = run(command, root)
        results.append({
            "name": name,
            "command": shlex.join(command),
            "status": "pass" if result.returncode == 0 else "fail",
            "exitCode": result.returncode,
            "output": (result.stdout + result.stderr).strip()[-4000:],
        })
    failed = [item for item in results if item["status"] == "fail"]
    return {"status": "fail" if failed else "pass", "checks": results, "configured": bool(checks)}


def verify(root: Path, refresh: bool, include_checks: bool) -> dict:
    path = baseline_path(root)
    if not path.exists():
        raise RuntimeError(f"Baseline not found: {path}; run capture before editing")
    baseline = json.loads(path.read_text(encoding="utf-8"))
    graph = verify_graph(root, refresh=refresh)
    impact = impact_analysis.verify_complete(root, baseline["targets"])
    targets = set(baseline["targets"])
    initial_dirty = set(baseline.get("initialDirtyFiles", []))
    changed = current_changed_files(root)
    unexpected = sorted(changed - targets - initial_dirty)
    removed_symbols = {}
    missing_files = []
    for relative, before in baseline["files"].items():
        target = root / relative
        if before.get("exists") and not target.exists():
            missing_files.append(relative)
            continue
        removed = sorted(set(before.get("publicSymbols", [])) - set(public_symbols(target)))
        if removed:
            removed_symbols[relative] = removed
    checks = run_checks(root) if include_checks else {"status": "pass", "configured": False, "checks": []}
    failures = []
    if unexpected:
        failures.append("unexpected files changed")
    if missing_files:
        failures.append("target files deleted")
    if removed_symbols:
        failures.append("public symbols removed")
    if checks["status"] != "pass":
        failures.append("configured checks failed")
    return {
        "status": "fail" if failures else "pass",
        "failures": failures,
        "graph": graph,
        "changedFiles": sorted(changed),
        "unexpectedFiles": unexpected,
        "missingFiles": missing_files,
        "removedPublicSymbols": removed_symbols,
        "checks": checks,
        "impact": impact,
    }


def hook(root: Path, mode: str) -> dict:
    graph = verify_graph(root, refresh=True)
    checks = run_checks(root)
    if checks["status"] != "pass":
        return {"status": "fail", "mode": mode, "graph": graph, "checks": checks}
    return {"status": "pass", "mode": mode, "graph": graph, "checks": checks}


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description="Graphify-backed development safety gates")
    sub = result.add_subparsers(dest="command", required=True)
    graph = sub.add_parser("verify-graph")
    graph.add_argument("--refresh", action="store_true")
    capture_cmd = sub.add_parser("capture")
    capture_cmd.add_argument("--files", nargs="+", required=True)
    verify_cmd = sub.add_parser("verify")
    verify_cmd.add_argument("--refresh", action="store_true")
    verify_cmd.add_argument("--run-checks", action="store_true")
    sub.add_parser("run-checks")
    hook_cmd = sub.add_parser("hook")
    hook_cmd.add_argument("--mode", choices=("pre-commit", "pre-push", "ci"), required=True)
    return result


def main() -> int:
    args = parser().parse_args()
    root = project_root()
    try:
        if args.command == "verify-graph":
            result = verify_graph(root, args.refresh)
        elif args.command == "capture":
            result = capture(root, args.files)
        elif args.command == "verify":
            result = verify(root, args.refresh, args.run_checks)
        elif args.command == "run-checks":
            result = run_checks(root)
        else:
            result = hook(root, args.mode)
    except Exception as exc:
        result = {"status": "fail", "error": str(exc)}
    print(json.dumps(result, indent=2))
    return 0 if result.get("status") == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main())
