---
name: development-safety-core
description: Mandatory change-safety gate for every coding task, including features, fixes, refactors, API or database changes, UI work, configuration changes, and generated code. Use before the first edit and again before reporting completion. Requires a fresh Graphify graph, caller/dependency inspection, a recorded baseline, minimal edits, tests, diff review, and post-change contract verification.
---

# Development Safety Core

Treat this workflow as mandatory for every change to executable or runtime-affecting files.

## Four mandatory gates

1. **Understand:** do not edit yet. Establish the current flow, exact requirement or root cause, reusable code, affected contracts, and explicit out-of-scope behavior.
2. **Minimal plan:** justify every file to modify and estimate the expected diff. A bug changing more than 3 files or adding more than 100 lines needs a written necessity justification before editing; these are review triggers, not automatic rejection limits.
3. **Implement:** make only the agreed logical change. Preserve the existing architecture and prefer reuse or deletion over new code.
4. **Challenge:** review the complete diff as if another developer wrote it. Remove safe unnecessary complexity, then test the required behavior and its most important regression condition.

Do not introduce a class, interface, service, DTO, wrapper, helper, fallback, validation, or new file unless existing code cannot reasonably satisfy the requirement. Apply the rule of three before creating reusable abstractions. Report unrelated improvements separately; do not implement them in the current task.

Never change authorization, audit trails, reason-for-change, electronic signatures, validation, locking, or data-integrity behavior unless the requirement explicitly calls for it.

## Before editing

1. Read the latest contents of every target file.
2. Run:

   ```text
   python .claude/skills/development-safety-core/scripts/change_safety.py verify-graph --refresh
   ```

3. Start exact-symbol impact closure before exploring code:

   ```text
   python .claude/skills/development-safety-core/scripts/impact_analysis.py start --seeds <ExactSymbol> [<ExactSymbol> ...]
   ```

4. Process every returned item in the current batch. Classify it with a reason as `must_change`, `must_inspect`, `verify_only`, `ignore`, or `unknown`:

   ```text
   python .claude/skills/development-safety-core/scripts/impact_analysis.py classify --ids <id> [<id> ...] --as must_inspect --reason "<evidence>"
   python .claude/skills/development-safety-core/scripts/impact_analysis.py next
   ```

   `batchSize` limits only one response. It is never a stopping limit. Continue batch 2, batch 3, and every later batch until `continuationRequired` is false. Resolve every `unknown` through targeted code inspection; an unknown direct impact blocks completion.

5. Read complete files only for `must_change`; read targeted symbols/snippets for `must_inspect` and `verify_only`. Do not explore unrelated code.

6. Verify closure and declare the intended target files. Every file attached to a `must_change` item must be a target:

   ```text
   python .claude/skills/development-safety-core/scripts/impact_analysis.py verify --targets <file> [<file> ...]
   ```

7. Capture their baseline:

   ```text
   python .claude/skills/development-safety-core/scripts/change_safety.py capture --files <file> [<file> ...]
   ```

8. Print a pre-change matrix containing the task, target files, callers, dependencies, helpers to reuse, contracts to preserve, files that must not change, graph freshness, and risks.
   Include why each target file must change, the expected diff size, required behavior, and explicit out-of-scope items.
9. Do not edit until the graph and baseline steps pass. If either cannot run, report `BLOCKED` or `UNVERIFIED`.

## While editing

- Make the smallest change that satisfies the request.
- Preserve public APIs, behavior, authorization, validation, and data contracts unless the approved requirement explicitly changes them.
- Reuse existing code and follow the closest working project pattern.
- Do not rewrite or reformat unrelated code.
- If a public contract must change, update every caller found in the graph and add migration or compatibility handling where applicable.

## Before completion

1. Refresh the post-change graph:

   ```text
   python .claude/skills/development-safety-core/scripts/change_safety.py verify-graph --refresh
   ```

2. Rerun `impact_analysis.py start` with the same exact seeds, process every post-change batch, and run `impact_analysis.py verify` again. A stale pre-change manifest blocks completion.
3. Run the integrated gate against that same graph:

   ```text
   python .claude/skills/development-safety-core/scripts/change_safety.py verify --run-checks
   ```

4. Inspect `git diff` and confirm no unrelated behavior or files were changed.
5. Perform a simplification pass. Look only for duplicate logic, one-use abstractions that do not improve readability, redundant validation/fallbacks, dead code, excessive comments, and unnecessary files. Apply safe reductions without changing behavior.
6. Run the project's regression, test, and code-review skills.
7. Report the requirement/root cause, files changed and why, behavior before/after, tests run, remaining risks, and exact command results. Never describe an unrun check as passed.
8. Say `done` only when the final safety result is `PASS`. Otherwise report `BLOCKED`, `UNVERIFIED`, or `FAIL` with the reason.

## Completion invariant

Never claim completion unless target files were read, Graphify was current before and after editing, callers and dependencies were inspected, a baseline was captured, only intended files changed, public contracts were preserved or all callers were updated, the final diff was reviewed, and configured checks passed.

## Project checks

Configure repository-specific commands in `dev-assistant.json`; they run locally and in CI:

```json
{
  "requiredChecks": {
    "test": ["npm", "test", "--", "--runInBand"],
    "build": ["npm", "run", "build"]
  }
}
```

Use argument arrays when possible so commands behave consistently across operating systems.

## Mandatory impact invariant

Graph-first analysis is mandatory for every connected project. Do not substitute broad repository exploration for a missing or unhealthy graph. If the graph lacks dependency links or an exact seed, report `UNVERIFIED` and repair/refine the graph or resolve the missing relationship with explicit evidence. Never silently omit an impact because of token, file, depth, or batch limits.
