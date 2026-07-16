---
name: stream-compare-reference-sdks
description: Compares the current Stream Video Unity SDK feature or problem with the mature JavaScript, Swift, and Android SDK implementations. Use only when explicitly invoked to find proven approaches, missing context, parity gaps, and Unity-specific constraints.
disable-model-invocation: true
---

# Stream Compare Reference SDKs

Investigate whether another Stream Video SDK already solves the feature or problem currently being developed. This is a read-only analysis: do not modify any repository.

Analyze one reference SDK at a time in this order:

1. JavaScript
2. Swift
3. Android

Report findings after each SDK and ask whether to continue to the next. Do not inspect all three up front.

## Locate the Repositories

Resolve the current Unity repository root, then look for these sibling directories under its parent:

- `stream-video-js`
- `stream-video-swift`
- `stream-video-android`

Construct paths from the parent directory at runtime; do not assume an operating system, username, drive letter, separator, or fixed absolute path.

Before analyzing a reference:

1. Verify that its directory exists and is a Git repository.
2. Record its current branch and `HEAD` commit.
3. Check whether relevant files have uncommitted changes. If they do, ask whether to analyze `HEAD` or the working tree.

If the expected sibling directory is missing, search only the current repository's parent directory for the expected repository name. If it still cannot be found, ask the user for that repository's path. Do not perform a broad machine-wide search and do not create or modify machine-specific configuration.

## Establish the Unity Problem

Before inspecting JavaScript:

1. Read the user's stated problem and any linked issue, specification, or files.
2. Inspect the relevant Unity feature branch changes, uncommitted changes, surrounding implementation, tests, and protocol models.
3. Trace the behavior far enough to identify:
   - The user-visible or system-level outcome.
   - API and SFU events involved.
   - State transitions, ordering, ownership, lifecycle, threading, and failure handling.
   - The exact uncertainty or design decision the comparison should resolve.
4. Distinguish the intended behavior from the current proposed implementation. The implementation may embody a wrong assumption.

If the feature or comparison question remains ambiguous, ask one focused question before searching a reference SDK.

## Analyze Each Reference SDK

Search by behavior and protocol concepts, not only Unity symbol names. Terminology and architecture differ across SDKs.

Use a narrow, evidence-driven search:

1. Find the closest public API, SFU event, model, state machine, coordinator, or lifecycle component.
2. Trace the complete relevant path from input/event to externally observable result.
3. Read focused tests, comments, and documentation that establish intent or edge-case behavior.
4. Check nearby handling for ordering, buffering, deduplication, cancellation, reconnects, cleanup, and partial state where relevant.
5. Inspect history only when current code cannot explain why the behavior exists.
6. Stop expanding the search once there is enough evidence to answer the comparison question.

For each reference, determine:

- Whether it solves the same problem or only a superficially similar one.
- Its actual behavior and the mechanism that produces it.
- Which assumptions it makes about the shared API/SFU.
- Which behavior is protocol-level and should likely remain consistent across SDKs.
- Which behavior is language-, platform-, framework-, or product-specific.
- Whether its tests confirm the important behavior.
- Whether multiple implementations or legacy paths exist.
- Whether adopting the approach would preserve Unity SDK compatibility and public behavior.

Treat reference SDKs as strong evidence, not authority. They are more mature and broadly exercised, so prefer alignment when constraints are equivalent. Still verify the reasoning and do not copy:

- Accidental behavior or an apparent defect.
- Legacy compatibility code Unity does not need.
- Architecture required only by that platform.
- Behavior that conflicts with current API/SFU contracts or Unity lifecycle constraints.

If the reference reveals missing information or shows that the Unity approach frames the problem incorrectly, say so directly and revisit the recommended Unity design.

## Unity-Specific Assessment

Explicitly consider whether Unity requires a different solution because of:

- Unity main-thread and object lifecycle rules.
- Mono and IL2CPP/AOT constraints.
- WebRTC resource ownership and callback timing.
- Editor versus player behavior.
- Android, iOS, Windows, macOS, and Linux differences.
- Existing Unity public APIs, serialization, upgrade compatibility, and customer expectations.

Do not label a difference “Unity-specific” without identifying the concrete constraint.

## Report After Each SDK

Keep the report focused on actionable evidence:

### `[SDK] findings`

- **Reference scope:** branch, commit, and relevant code/test locations.
- **How it works:** concise end-to-end behavior.
- **What this reveals:** missing context, confirmed assumptions, or flaws in the current framing.
- **Unity comparison:** important matches and differences.
- **Recommendation:** choose `Adopt`, `Adapt`, or `Do not use`, with the reason.
- **Confidence and gaps:** confirmed facts versus unresolved assumptions.

Do not dump search results or summarize unrelated architecture. Cite exact repository-relative files and symbols.

After the JavaScript report, ask whether to inspect Swift. After the Swift report, ask whether to inspect Android. Use a structured choice when available:

- Continue to the next SDK
- Stop with the current findings

When the user stops, or after Android, provide a short synthesis:

- Recommended Unity approach.
- Cross-SDK parity implications.
- Unity-specific deviations and their justification.
- Remaining unknowns or validation needed.

Do not claim consensus unless every SDK inspected supports it. Clearly state which SDKs were not inspected.
