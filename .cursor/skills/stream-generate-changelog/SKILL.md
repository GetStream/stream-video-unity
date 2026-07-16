---
name: stream-generate-changelog
description: Generates and writes a customer-facing Stream Video Unity SDK changelog entry from all committed changes since the latest release. Use for preparing the root CHANGELOG.md for a new SDK release.
disable-model-invocation: true
---

# Stream Generate Changelog

Generate the next customer-facing changelog entry from every relevant committed change since the latest release. Edit only the repository-root `CHANGELOG.md`.

The repository root must be the opened Cursor workspace. If only `Packages/StreamVideo` is open, stop and ask the user to open the `stream-video-unity` root so this skill and all release context are available.

## Contract

- This skill may modify only `CHANGELOG.md` at the repository root.
- Do not modify package versions, manifests, metadata, release configuration, source, tests, or generated files.
- Do not commit, tag, push, create a release, or alter Git history.
- Base the entry on committed changes only. Never silently include uncommitted source changes.
- Preserve unrelated user changes.
- Keep scope limited to the Stream Video Unity SDK release represented by `Packages/StreamVideo`.

## Establish the Release Boundary

1. Read repository instructions, release documentation, `Packages/StreamVideo/package.json`, and the current `CHANGELOG.md`.
2. Inspect Git status before doing anything else.
   - If `CHANGELOG.md` has uncommitted changes, stop and ask whether to preserve and build on them or leave the file unchanged.
   - Exclude other uncommitted and untracked changes from release analysis. If they prevent reliable inspection or validation, stop and explain why.
3. Inspect release tags, their target commits, the latest changelog heading, package version, and commit ancestry.
4. Identify the latest released SDK version and its corresponding tag that is reachable from `HEAD`.
   - Prefer the tag matching the newest released version documented at the top of `CHANGELOG.md`.
   - Treat equivalent `X.Y.Z` and `vX.Y.Z` tags on the same commit as one release boundary.
   - Do not choose a tag solely because it sorts highest; unrelated, malformed, legacy, or non-SDK tags may exist.
5. Confirm that the repository contains the complete history from the release tag through `HEAD`. If the clone is shallow, required objects are missing, or the release tag cannot be resolved, stop rather than produce a partial entry.
6. Determine the new release version from the package manifest or explicit user input.
   - The new version must be greater than the latest released SDK version.
   - If the manifest still contains the released version, multiple plausible versions exist, or version sources disagree, ask the user for the intended version.
7. Use the exclusive commit range `<release-tag>..HEAD`. Record the resolved versions, tag, tag commit, `HEAD`, and commit range as an internal evidence ledger.

Do not edit `CHANGELOG.md` until the release boundary and new version are unambiguous.

## Analyze Every Change

Inspect:

- Every commit and merge commit in the release range.
- The cumulative diff from the release tag to `HEAD`.
- Changed public APIs, behavior, defaults, serialization, package structure, assembly definitions, platform integrations, tests, and documentation.
- Relevant surrounding code and prior behavior when a customer-facing effect is not clear from the diff.
- Pull request context available locally or through authenticated repository tooling when commit messages and code do not establish intent.

Do not rely on commit subjects alone. Commits may be incomplete, implementation-focused, duplicated by later fixes, or reverted.

For each candidate item, establish:

1. The final behavior at `HEAD`, accounting for reverts and follow-up fixes.
2. The customer-visible effect.
3. The affected platform or usage scenario.
4. Whether customers must change code, settings, assets, prefabs, or build configuration when upgrading.
5. Concrete evidence in commits and code locations.

Classify statements in the internal evidence ledger as:

- **Confirmed**: directly supported by the final diff and surrounding code.
- **Inferred**: strongly implied but not directly proven.
- **Unverified risk**: plausible but lacking enough evidence.

Only confirmed facts may be stated as facts in the changelog. Ask for clarification when an inferred point is important to customers. Omit unverified risks from the entry unless they are confirmed release limitations supplied by the user.

## Customer-Contract Review

For relevant changes, check customer impact across:

- Public and protected APIs, namespaces, assembly names, and assembly definitions.
- Optional parameters, defaults, events, callbacks, ordering, threading, and Unity main-thread behavior.
- Async operations, cancellation, synchronization, lifecycle, cleanup, resource ownership, native resources, and allocations.
- Unity serialization, prefabs, assets, metadata, managed stripping, reflection, and IL2CPP/AOT.
- JSON models, wire formats, backend compatibility, and persisted data.
- Android and iOS native integrations.
- Windows, macOS, Linux, Unity Editor/player, Mono, and IL2CPP/AOT where supported.
- Filesystem and path behavior.
- Upgrade behavior in existing customer projects.

Call out a breaking or migration-requiring change clearly and include the required customer action. Do not hide it under a generic improvement. If the code appears breaking but intent or migration guidance is unclear, stop and ask before writing the entry.

## Select and Consolidate Entries

Include changes that customers can observe or act on:

- New capabilities and public APIs.
- Reliability, correctness, compatibility, performance, or resource-use improvements.
- Bug fixes with a credible user-visible symptom.
- Platform support or build changes.
- Deprecations, breaking changes, and required migration steps.
- Important configuration or upgrade guidance.

Normally omit:

- Refactors with no externally observable effect.
- Test-only, CI-only, formatting, dependency housekeeping, or internal tooling changes.
- Generated-code churn with no customer-visible contract change.
- Intermediate implementation details superseded before `HEAD`.
- Duplicate descriptions of the same final behavior.
- Claims such as "more robust," "optimized," or "fixed issues" without a specific customer outcome.

Mention an internal dependency update only when it changes compatibility, supported platforms, build requirements, behavior, security, performance, or another customer-relevant outcome.

Combine related commits into one clear item. Separate distinct customer outcomes even if they came from one commit. Describe the final release, not the development history.

## Write in Repository Style

1. Follow the structure, heading levels, punctuation, terminology, and level of detail used by the most recent well-formed entries in `CHANGELOG.md`.
2. Add the new version at the top; do not rewrite prior releases.
3. Choose sections based on the actual changes. Prefer established headings such as `### Improvements` and `### Bug Fixes`; use a focused platform or feature heading only when it materially helps customers.
4. Order items by customer importance, then group closely related items.
5. Lead with the customer outcome. Use internal names only when they are public APIs customers must recognize.
6. Use exact public symbols in backticks.
7. Explain the practical scenario for subtle fixes without exposing unnecessary implementation jargon.
8. Keep each bullet concise, factual, and understandable without access to commits or internal architecture.
9. Do not include commit hashes, pull request numbers, ticket IDs, internal component names, or contributor-oriented notes unless the existing release style and customer value clearly require them.
10. Do not claim absolute safety, complete platform coverage, or performance gains that were not measured.

## Validate Before Finishing

After editing:

1. Re-read the complete new entry as a customer upgrading an existing Unity project.
2. Map every bullet back to concrete evidence in the release range.
3. Verify every relevant commit was considered, while confirming that omitted commits have no credible customer-facing effect.
4. Check that the entry describes the cumulative `HEAD` behavior and contains no reverted, duplicated, speculative, or internal-only claims.
5. Confirm the version heading is correct, appears once, and is above the prior release.
6. Confirm only `CHANGELOG.md` was modified by this skill and pre-existing unrelated work remains untouched.
7. Run an appropriate whitespace or diff integrity check for `CHANGELOG.md`.
8. Tests are normally unnecessary for a changelog-only edit. Do not run broad SDK tests unless needed to verify a disputed factual claim. Never imply that unrun platforms or tests were validated.

If validation exposes an ambiguity that affects wording, release scope, compatibility, or migration guidance, stop and ask instead of guessing.

## Final Response

On success, return only the exact finished changelog entry, beginning with the new version heading. Do not add a summary, validation report, preamble, or trailing commentary.

If blocked, ask the smallest focused question needed to continue and do not present a partial entry as final.
