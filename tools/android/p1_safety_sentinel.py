#!/usr/bin/env python3
"""Fail closed if authoritative P1 production tooling can self-promote or publish.

The sentinel scans production operator tools only. Python is inspected with ``ast``;
PowerShell and C# use comment/string-aware assignment and command recognizers.
It never changes repository state and never grants verification or publication.
"""

from __future__ import annotations

import argparse
import ast
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence

SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_REPO_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_CHAIN = SCRIPT_DIR / "p1_operator_release_chain.json"

PROTECTED_FIELDS = {
    "verified",
    "runtimeverified",
    "owneraccepted",
    "publicationeligible",
    "publicationperformed",
}

CRITICAL_CSHARP = (
    "unity_game/Assets/Afareet/Editor/P1ProductionCandidateStagingHandoff.cs",
)

FORBIDDEN_COMMAND_PATTERNS = (
    re.compile(r"(?i)(?:^|[;&|]\s*)git(?:\.exe)?\s+(?:push|tag)\b"),
    re.compile(r"(?i)(?:^|[;&|]\s*)gh(?:\.exe)?\s+release\s+(?:create|upload)\b"),
)


@dataclass(frozen=True)
class Violation:
    path: str
    line: int
    rule: str
    detail: str

    def render(self) -> str:
        return f"{self.path}:{self.line}: {self.rule}: {self.detail}"


def _field_name_from_target(node: ast.AST) -> str | None:
    if isinstance(node, ast.Name):
        return node.id.lower()
    if isinstance(node, ast.Attribute):
        return node.attr.lower()
    if isinstance(node, ast.Subscript):
        slice_node = node.slice
        if isinstance(slice_node, ast.Constant) and isinstance(slice_node.value, str):
            return slice_node.value.lower()
    return None


def _is_true(node: ast.AST | None) -> bool:
    return isinstance(node, ast.Constant) and node.value is True


def _qualname(node: ast.AST) -> str:
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        prefix = _qualname(node.value)
        return f"{prefix}.{node.attr}" if prefix else node.attr
    return ""


def _literal_strings(node: ast.AST) -> list[str]:
    return [
        child.value
        for child in ast.walk(node)
        if isinstance(child, ast.Constant) and isinstance(child.value, str)
    ]


def _looks_forbidden_command(text: str) -> str | None:
    compact = " ".join(text.replace("\n", " ").split())
    for pattern in FORBIDDEN_COMMAND_PATTERNS:
        match = pattern.search(compact)
        if match:
            return match.group(0).strip()
    return None


class PythonSafetyVisitor(ast.NodeVisitor):
    EXEC_CALLS = {
        "os.system",
        "os.popen",
        "subprocess.call",
        "subprocess.check_call",
        "subprocess.check_output",
        "subprocess.Popen",
        "subprocess.run",
    }

    def __init__(self, relative_path: str) -> None:
        self.relative_path = relative_path
        self.violations: list[Violation] = []

    def _record_true(self, field: str, node: ast.AST) -> None:
        if field.lower() in PROTECTED_FIELDS:
            self.violations.append(
                Violation(
                    self.relative_path,
                    getattr(node, "lineno", 1),
                    "P1_SELF_PROMOTION",
                    f"protected field {field!r} is assigned boolean true",
                )
            )

    def visit_Assign(self, node: ast.Assign) -> None:  # noqa: N802
        if _is_true(node.value):
            for target in node.targets:
                field = _field_name_from_target(target)
                if field:
                    self._record_true(field, node)
        self.generic_visit(node)

    def visit_AnnAssign(self, node: ast.AnnAssign) -> None:  # noqa: N802
        if _is_true(node.value):
            field = _field_name_from_target(node.target)
            if field:
                self._record_true(field, node)
        self.generic_visit(node)

    def visit_NamedExpr(self, node: ast.NamedExpr) -> None:  # noqa: N802
        if _is_true(node.value):
            field = _field_name_from_target(node.target)
            if field:
                self._record_true(field, node)
        self.generic_visit(node)

    def visit_Dict(self, node: ast.Dict) -> None:  # noqa: N802
        for key, value in zip(node.keys, node.values):
            if (
                isinstance(key, ast.Constant)
                and isinstance(key.value, str)
                and key.value.lower() in PROTECTED_FIELDS
                and _is_true(value)
            ):
                self._record_true(key.value, value)
        self.generic_visit(node)

    def visit_Call(self, node: ast.Call) -> None:  # noqa: N802
        for keyword in node.keywords:
            if keyword.arg and keyword.arg.lower() in PROTECTED_FIELDS and _is_true(keyword.value):
                self._record_true(keyword.arg, keyword.value)

        if _qualname(node.func) in self.EXEC_CALLS:
            strings: list[str] = []
            for arg in node.args:
                strings.extend(_literal_strings(arg))
            for keyword in node.keywords:
                strings.extend(_literal_strings(keyword.value))
            command = _looks_forbidden_command(" ".join(strings))
            if command:
                self.violations.append(
                    Violation(
                        self.relative_path,
                        getattr(node, "lineno", 1),
                        "P1_AUTOMATED_PUBLICATION",
                        f"execution call contains forbidden command: {command}",
                    )
                )
        self.generic_visit(node)


def scan_python(path: Path, relative_path: str) -> list[Violation]:
    try:
        tree = ast.parse(path.read_text(encoding="utf-8-sig"), filename=relative_path)
    except (OSError, SyntaxError) as exc:
        return [Violation(relative_path, getattr(exc, "lineno", 1) or 1, "P1_SCAN_ERROR", str(exc))]
    visitor = PythonSafetyVisitor(relative_path)
    visitor.visit(tree)
    return visitor.violations


def _strip_line_comment(line: str, marker: str) -> str:
    """Strip a line comment marker only when it occurs outside quoted strings."""
    result: list[str] = []
    quote: str | None = None
    escaped = False
    index = 0
    while index < len(line):
        char = line[index]
        if escaped:
            result.append(char)
            escaped = False
            index += 1
            continue
        if char == "\\" and quote == '"':
            result.append(char)
            escaped = True
            index += 1
            continue
        if quote:
            result.append(char)
            if char == quote:
                quote = None
            index += 1
            continue
        if char in {'"', "'"}:
            quote = char
            result.append(char)
            index += 1
            continue
        if line.startswith(marker, index):
            break
        result.append(char)
        index += 1
    return "".join(result)


def _mask_strings(line: str) -> str:
    """Replace quoted content with spaces while preserving command positions."""
    chars = list(line)
    quote: str | None = None
    escaped = False
    for index, char in enumerate(chars):
        if escaped:
            chars[index] = " "
            escaped = False
            continue
        if quote:
            if char == "\\" and quote == '"':
                chars[index] = " "
                escaped = True
                continue
            if char == quote:
                quote = None
            chars[index] = " "
            continue
        if char in {'"', "'"}:
            quote = char
            chars[index] = " "
    return "".join(chars)


def _scan_assignment_lines(
    lines: Iterable[str],
    relative_path: str,
    *,
    comment_marker: str,
    true_token: str,
) -> list[Violation]:
    violations: list[Violation] = []
    fields = "|".join(re.escape(field) for field in sorted(PROTECTED_FIELDS, key=len, reverse=True))
    assignment = re.compile(rf"(?i)\b({fields})\b\s*[:=]\s*{true_token}\b")
    for line_number, raw in enumerate(lines, start=1):
        code = _strip_line_comment(raw, comment_marker)
        match = assignment.search(_mask_strings(code))
        if match:
            violations.append(
                Violation(
                    relative_path,
                    line_number,
                    "P1_SELF_PROMOTION",
                    f"protected field {match.group(1)!r} is assigned true",
                )
            )
    return violations


def scan_powershell(path: Path, relative_path: str) -> list[Violation]:
    try:
        lines = path.read_text(encoding="utf-8-sig").splitlines()
    except OSError as exc:
        return [Violation(relative_path, 1, "P1_SCAN_ERROR", str(exc))]

    violations = _scan_assignment_lines(
        lines,
        relative_path,
        comment_marker="#",
        true_token=r"\$true",
    )
    for line_number, raw in enumerate(lines, start=1):
        code = _strip_line_comment(raw, "#")
        if not code.strip():
            continue

        # Direct git/gh command detection must ignore quoted explanatory prose.
        masked = _mask_strings(code)
        direct = re.search(
            r"(?i)(?:&\s*)?(?:\$git(?:\.Source)?|git(?:\.exe)?)\b[^\r\n]*\b(push|tag)\b",
            masked,
        )
        gh_release = re.search(
            r"(?i)(?:&\s*)?(?:\$gh(?:\.Source)?|gh(?:\.exe)?)\b[^\r\n]*\brelease\b[^\r\n]*\b(create|upload)\b",
            masked,
        )

        # Start-Process may legitimately quote executable/arguments, so first prove the
        # Start-Process token itself is executable code, then inspect its raw arguments.
        start_process = None
        if re.search(r"(?i)\bStart-Process\b", masked):
            start_process = re.search(
                r"(?i)\bStart-Process\b[^\r\n]*(?:git|gh)[^\r\n]*(?:push|tag|release\s+(?:create|upload))\b",
                code,
            )

        match = direct or gh_release or start_process
        if match:
            violations.append(
                Violation(
                    relative_path,
                    line_number,
                    "P1_AUTOMATED_PUBLICATION",
                    f"PowerShell command can mutate remote release state: {match.group(0).strip()}",
                )
            )
    return violations


def _strip_csharp_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.S)
    return "\n".join(_strip_line_comment(line, "//") for line in text.splitlines())


def scan_csharp(path: Path, relative_path: str) -> list[Violation]:
    try:
        text = path.read_text(encoding="utf-8-sig")
    except OSError as exc:
        return [Violation(relative_path, 1, "P1_SCAN_ERROR", str(exc))]

    code = _strip_csharp_comments(text)
    lines = code.splitlines()
    violations = _scan_assignment_lines(lines, relative_path, comment_marker="//", true_token="true")

    process_pattern = re.compile(
        r"(?is)\bProcess\s*\.\s*Start\s*\([^;]{0,1000}?(?:git|gh)[^;]{0,1000}?(?:push|tag|release\s+(?:create|upload))\b"
    )
    for match in process_pattern.finditer(code):
        line_number = code.count("\n", 0, match.start()) + 1
        violations.append(
            Violation(
                relative_path,
                line_number,
                "P1_AUTOMATED_PUBLICATION",
                "C# Process.Start can mutate Git/release state",
            )
        )
    return violations


def load_scan_paths(repo_root: Path, chain_path: Path) -> list[str]:
    payload = json.loads(chain_path.read_text(encoding="utf-8"))
    if payload.get("state") != "P1_AUTHORITATIVE_OPERATOR_CHAIN" or payload.get("authoritativeForP1") is not True:
        raise ValueError("operator chain is not the authoritative P1 schema/state")
    stages = payload.get("orderedStages")
    if not isinstance(stages, list) or not stages:
        raise ValueError("operator chain has no ordered stages")

    paths: list[str] = []
    for stage in stages:
        if not isinstance(stage, dict):
            raise ValueError("operator chain stage must be an object")
        tool = stage.get("tool")
        if tool is None:
            continue
        if not isinstance(tool, str) or not tool.strip():
            raise ValueError("operator chain tool path must be a non-empty string or null")
        if Path(tool).suffix.lower() in {".py", ".ps1", ".cs"}:
            paths.append(tool)
    paths.extend(CRITICAL_CSHARP)

    unique: list[str] = []
    seen: set[str] = set()
    resolved_root = repo_root.resolve()
    for relative in paths:
        normalized = relative.replace("\\", "/")
        if normalized in seen:
            continue
        candidate = (resolved_root / normalized).resolve()
        try:
            candidate.relative_to(resolved_root)
        except ValueError as exc:
            raise ValueError(f"scan path escapes repo root: {normalized}") from exc
        if not candidate.is_file():
            raise ValueError(f"authoritative P1 production scan path is missing: {normalized}")
        seen.add(normalized)
        unique.append(normalized)
    return unique


def scan_file(path: Path, relative_path: str) -> list[Violation]:
    suffix = path.suffix.lower()
    if suffix == ".py":
        return scan_python(path, relative_path)
    if suffix == ".ps1":
        return scan_powershell(path, relative_path)
    if suffix == ".cs":
        return scan_csharp(path, relative_path)
    return [Violation(relative_path, 1, "P1_SCAN_ERROR", f"unsupported production scan suffix: {suffix}")]


def scan_repo(repo_root: Path, chain_path: Path) -> tuple[list[str], list[Violation]]:
    repo_root = repo_root.expanduser().resolve()
    chain_path = chain_path.expanduser().resolve()
    paths = load_scan_paths(repo_root, chain_path)
    violations: list[Violation] = []
    for relative in paths:
        violations.extend(scan_file(repo_root / relative, relative))
    return paths, violations


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=str(DEFAULT_REPO_ROOT), help="Exact repository worktree root")
    parser.add_argument("--chain", default=str(DEFAULT_CHAIN), help="Authoritative P1 operator chain JSON")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        paths, violations = scan_repo(Path(args.repo_root), Path(args.chain))
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_P1_SAFETY_SENTINEL_ERROR: {exc}", file=sys.stderr)
        return 2

    if violations:
        for violation in violations:
            print(f"AFAREET_P1_SAFETY_SENTINEL_VIOLATION {violation.render()}", file=sys.stderr)
        print(
            f"AFAREET_P1_SAFETY_SENTINEL_BLOCKED files={len(paths)} violations={len(violations)}",
            file=sys.stderr,
        )
        return 2

    print(f"AFAREET_P1_SAFETY_SENTINEL_OK files={len(paths)} violations=0")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
