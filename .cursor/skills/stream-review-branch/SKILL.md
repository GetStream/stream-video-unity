---
name: stream-review-branch
description: Performs a thorough, evidence-based review of all committed changes on the current feature branch, emphasizing correctness, customer value, regressions, maintainability, and cross-platform Stream Video Unity SDK compatibility.
disable-model-invocation: true
---

# Stream Review Branch

Review all committed changes on the current Git branch. Do not modify code unless explicitly asked.

Answer these questions:

1. Should this branch be merged?
2. Is it safe for existing customers to upgrade?
3. Does it provide meaningful customer or project value?
4. Is the implementation correct, appropriately simple, and maintainable?

## Establish the Scope

1. Read repository instructions and relevant documentation.
2. Determine the default base branch and merge-base.
3. Inspect:
   - The complete committed diff from the merge-base to `HEAD`.
   - Every commit on the current branch.
   - Relevant surrounding code, callers, implementations, tests, and documentation.
4. Review the cumulative branch result, not only the latest commit.
5. Check for uncommitted and untracked changes before reviewing.
   - Do not include them automatically.
   - If they could affect inspection or validation, stop and ask whether they should be included, temporarily set aside, or ignored.
6. If the intended base branch or feature purpose is ambiguous, ask before proceeding.
7. State the base branch, merge-base, reviewed commit range, and whether uncommitted changes were excluded.

## Understand the Change

Infer the intended feature or fix from commits, changed code, tests, documentation, and project context.

Determine:

- What problem the branch solves.
- Who benefits and how.
- Whether the implementation fulfills that purpose.
- Whether every changed file is necessary.
- Whether unrelated, redundant, generated, or accidental changes are present.
- Whether a smaller or clearer implementation would provide the same value.

Do not assume added code is valuable merely because it works.

## Correctness

Verify:

- The implementation behaves as intended.
- All relevant execution paths and call sites were considered.
- Edge cases, invalid inputs, partial failures, cancellation, retries, and cleanup are handled.
- State transitions, object lifetimes, and resource ownership remain correct.
- Async, threading, synchronization, and Unity main-thread requirements are respected.
- Exceptions and error results preserve existing contracts.
- There are no race conditions, leaks, deadlocks, stale state, or ordering problems.

Trace important behavior through surrounding code rather than reviewing the diff in isolation.

## Regression Risk

Look for existing behavior that could now:

- Produce different results.
- Throw new exceptions.
- Stop invoking callbacks or events.
- Invoke callbacks in a different order or on a different thread.
- Change timing, lifecycle, defaults, validation, or error handling.
- Break serialization, persistence, networking, configuration, or generated data.
- Increase allocations, CPU usage, latency, bandwidth, or resource retention.
- Fail when customers upgrade without changing their application code.

Treat subtle behavioral incompatibilities as seriously as compilation failures.

## Public API and Upgrade Compatibility

This repository is an SDK distributed as a Unity plugin. Existing customers upgrade it inside established projects.

Inspect externally observable contracts, including:

- Public and protected types and members.
- Constructors, overloads, interfaces, inheritance, and generic constraints.
- Namespaces, assembly names, package layout, and assembly definitions.
- Method signatures, return types, parameter names, optional parameters, and default values.
- Events, delegates, callbacks, properties, fields, and extension methods.
- Enums, including member names and numeric values.
- DTOs, serialized models, JSON field names, and wire formats.
- Unity-serialized fields, components, prefabs, assets, and metadata.
- Configuration keys, defaults, and documented behavior.

Check for:

- Source, binary, or behavioral compatibility breaks.
- Overload ambiguity.
- Serialization or migration breaks.
- Changes requiring customer code or asset modifications.

Assume breaking changes are unacceptable unless the branch clearly documents an intentional major-version release and migration plan. Treat undocumented or accidental breaking changes as blockers.

## Cross-Platform and Unity Compatibility

Consider:

- Android
- iOS
- Windows
- macOS
- Linux
- Unity Editor and player builds
- Mono and IL2CPP/AOT where supported

Check platform-specific APIs, conditional compilation, filesystem assumptions, path handling, native dependencies, threading differences, stripping/linker behavior, reflection, code generation, serialization, and unsupported runtime APIs.

Do not assume behavior validated on one platform is portable to all supported targets.

## Design and Maintainability

Evaluate whether the change:

- Fits the existing architecture and conventions.
- Uses existing abstractions where appropriate.
- Introduces unnecessary layers, indirection, configuration, or generalization.
- Duplicates existing functionality.
- Expands the public API unnecessarily.
- Makes future changes harder.
- Contains speculative code not required by the feature.
- Leaves dead code, obsolete paths, misleading names, or inconsistent behavior.

Prefer the simplest implementation that fully satisfies the requirement, without sacrificing correctness or required extensibility.

## Tests and Validation

Evaluate whether tests:

- Cover intended behavior rather than implementation details.
- Reproduce the original bug when reviewing a fix.
- Cover success, failure, edge cases, and compatibility-sensitive paths.
- Protect existing behavior from regression.
- Are deterministic and meaningful.
- Would fail if the implementation were removed or broken.
- Cover affected platforms or clearly identify platform coverage gaps.

Run the most relevant available tests and static checks when practical. Do not claim safety from tests that were not run. State exactly what was and was not verified.

## Finding Quality

Report only concrete, actionable findings supported by evidence.

For every finding provide:

- Severity: `Blocker`, `High`, `Medium`, or `Low`.
- File and exact line or code location.
- The problematic behavior.
- A realistic failure or customer-impact scenario.
- Why existing tests or safeguards do not prevent it.
- The smallest appropriate correction.

Do not report:

- Pure stylistic preferences.
- Hypothetical concerns without a credible failure path.
- Existing problems unrelated to this branch.
- Suggestions that add complexity without meaningful benefit.

Clearly distinguish confirmed defects from residual risks and unverified assumptions.

## Final Report

### Verdict

Choose exactly one:

- `DO NOT MERGE`
- `MERGE AFTER FIXES`
- `SAFE TO MERGE`

Include a confidence level and a direct answer about customer upgrade safety.

`SAFE TO MERGE` does not mean risk-free. Use it only when no merge-blocking issue was found and validation is proportionate to the change.

### Scope

- Base branch and merge-base.
- Commits reviewed.
- Uncommitted changes excluded or included.
- Tests and checks run.

### Purpose and Value

- Intended behavior.
- Customer or project value.
- Whether the branch achieves it.
- Unnecessary or unrelated changes.

### Findings

List findings from highest to lowest severity. If none were found, explicitly say so.

### Compatibility Assessment

Summarize:

- Public API compatibility.
- Behavioral and serialization compatibility.
- Unity and platform risks.
- Upgrade impact for existing customers.

### Validation Gaps and Residual Risks

List anything that could not be proven, such as untested platforms, unavailable Unity versions, native integrations, or missing integration tests.

### Required Actions

Provide only actions necessary before merge. Keep optional improvements separate and minimal.

End with direct answers:

- Should this be merged?
- Is it safe for existing customers to upgrade?
- Does it add enough value to justify its complexity?
