# [Specific development spec title]

> **Status:** Ready for implementation | Ready with assumptions | Blocked — decision required
> **Created/updated:** YYYY-MM-DD
> **Repository / scope:** [Video SDK, Chat SDK, WebRTC fork; name all affected repositories]
> **Source snapshot:** [branch, commit hash, package version; or "not available / unverified"]
> **Deliverable:** [concrete finished behavior]
> **Intended reader:** A developer or AI agent who has **not** read the original discussion.

> **Handoff rule:** This spec is the authoritative record of the agreed scope, background, decisions, and completion criteria. Read it completely before editing code. Do not assume undocumented context. Record material changes to the plan and the reasons in the decision log. Check off a task only when its acceptance and verification conditions pass.

## 1. Outcome and definition of done

**Goal:** [What must be true when this work is complete — not just a list of edits.]

**User/customer impact:** [Which SDK consumers are affected and how the result should behave inside their Unity projects.]

**Definition of done:** [Short, objectively verifiable release/integration criteria. Include successful customer-style package import and supported-target verification when relevant.]

## 2. Problem statement and full background

### Current behavior / problem

[Observed facts, symptoms, reproduction steps, error messages, important examples, initial report, and impact. Distinguish confirmed behavior from hypotheses.]

### Expected behavior

[Precise desired semantics, including API-facing behavior and observable outcomes.]

### How we arrived at this plan

[Investigation timeline in decision-relevant order: approaches tried, results, evidence, root-cause analysis, hypotheses ruled out. Preserve why the plan looks the way it does. Do not include irrelevant chat transcript.]

### Why this matters for downstream Unity customers

[Integration friction, collision/breakage/performance/upgrade impact. If not an SDK/customer change, state why this section does not apply.]

## 3. Scope and non-goals

**In scope**

- [Explicit included features, code areas, deliverables, and platform scope.]

**Out of scope**

- [Explicit exclusions and deliberate deferred work.]

**Must preserve**

- [Existing behavior, API contracts, compatibility, public method signatures, file format, serialized references, and documented support that must not regress.]
- **Unity Editor support floor and documented range:** [Oldest explicitly supported Unity version, newer supported version families, and source; do not silently raise the minimum. Explain customers may be pinned to old Unity releases.]

## 4. Decisions, rationale, and alternatives

| ID | Decision (confirmed/provisional) | Why / evidence | Alternatives rejected and why |
| --- | --- | --- | --- |
| D01 | [Decision] | [Rationale] | [What was considered and rejected] |

[Capture contradictory or superseded decisions where necessary. If the solution was explicitly chosen, never silently redesign it.]

## 5. Repository context and existing implementation

**Relevant components and paths**

| Repository / file / symbol | Current role and behavior | Planned change / reason |
| --- | --- | --- |
| [Specific path and symbol, or unknown] | [What currently happens] | [What changes] |

**Key interfaces, dependencies, and data flow:** [Describe the actual current design, call sequences, ownership, thread/lifecycle issues, dependency versions, packaging structure, and integration assumptions needed to work safely. Include small exact snippets if they materially reduce ambiguity.]

**Evidence and source snapshot:** [Local paths, commit hashes, changelog excerpts, README/platform-doc links, experiment outcomes. Summarize crucial findings inline. Mark which facts were not independently verified.]

## 6. Proposed implementation

**Design:** [Detailed but focused architecture: entry points, changed behavior, data/control flow, error handling, edge cases, interactions between Video/Chat/WebRTC where relevant.]

**Compatibility / migration:** [What existing consumers will experience after update, whether there are breaking changes, versioning implications, fallback behavior, and migration instructions if required.]

**Failure modes and caveats:** [Specific pitfalls identified during discussion and investigation, and their mitigations.]

## 7. SDK Consumer Compatibility Contract

This section is mandatory for changes to Stream's Unity Video SDK, Unity Chat SDK, or its custom WebRTC fork. **The SDK is a plugin installed into customers' existing Unity projects.** The implementation must minimize conflicts with customer assemblies, third-party plugins, project settings, and build tooling. Working in the SDK sample/editor alone is insufficient.

Evaluate each constraint below and attach any relevant work to tasks in section 8. If a constraint genuinely does not apply, say why — **do not silently remove it**.

- **Namespace and symbol hygiene:** Keep new C# types and APIs within established, SDK-specific namespaces (e.g., `StreamVideo.*` / `StreamChat.*`, confirmed against existing code). Avoid unintended global-namespace types, generic public type names, leaking internal helpers into the public API, ambiguous extension methods, overly broad editor menus, scripting define names, and new symbol/assembly collisions. Don't gratuitously rename existing public APIs.
- **Assembly isolation:** Check `.asmdef` names, references, define constraints and platform filters. Runtime must not reference Editor-only code. Preserve integrations with consumers using both default `Assembly-CSharp` and their own `.asmdef` assemblies. Any new dependencies must coexist with existing customer packages.
- **Managed dependency coexistence:** Prevent duplicate precompiled DLLs/types (notably `Newtonsoft.Json` when relevant), assembly-name clashes, and hidden conflicts between Chat and Video when both are installed. Avoid vendoring an extra dependency without a clear compatibility plan.
- **Native dependency coexistence and Unity-version parity:** Video embeds a modified Unity WebRTC package; assess conflicts with Unity's official `com.unity.webrtc`, native binary/framework names, ABI compatibility, plugin importer filters, duplicate types, and native symbol loading. For native-fork work, check supported architectures, transitive dependencies, patch retention, packaging and licenses. **The native fork must support the Video SDK's full documented Unity Editor version range on its supported platforms** (including the oldest Editor), not just compile in the fork's development environment. Prove the native + managed binding + packaged Video integration together. Do not claim both WebRTC packages work together unless verified.
- **Distribution and installation:** Respect each affected SDK's actual release path (Video UPM package, Chat `.unitypackage`, native fork embedded/distributed according to the inspected repo). Ensure required `.meta`/GUIDs, `.asmdef`, `.dll`/native binaries, `link.xml`, editor scripts, resources and package metadata survive packaging; keep samples/dev tools out of runtime or customer installs unless intentionally shipped. No unnecessary changes to customers' manifests, global settings, scripting defines, or project structure.
- **Supported Unity Editor versions:** Treat the **entire documented version range, not only the latest Editor**, as a compatibility contract. Identify the oldest supported Unity release, all documented supported families, and the newest explicitly documented release from source evidence. Keep new C# syntax, Unity API use, runtime/BCL APIs, package dependencies and native libraries compatible with the oldest. Where newer Unity versions require alternate implementations, preserve old-version behavior using documented guarded alternatives; test both paths. Do not silently increase the Unity floor or use a package dependency unavailable on the minimum Editor.
- **Supported-platform accuracy:** Obtain the platform matrix from the *current affected README*, platform docs, package metadata and CI. Chat and Video differ: never infer Video WebGL support from Chat support, or promise unsupported targets. Check Editor vs player build, Mono/IL2CPP/AOT, stripping/reflection, CPU architecture, native library importers, permissions, graphics/audio differences and platform `#if` guards where relevant. Report which targets were actually verified.
- **Unity lifecycle, stability, and footprint:** Preserve initialization order, main-thread access, event subscription cleanup, async cancellation/disposal, domain/scene reload behavior, Editor vs player behavior, memory/resource cleanup and reasonable runtime performance. Avoid hidden singleton/object/project-wide side effects.
- **Backwards compatibility:** Preserve public API and source compatibility where required, serialized fields/Unity GUID references, package installation/upgrade behavior, existing scripts/scenes and old release consumers. Flag unavoidable breaks explicitly with migration guidance and release notes.
- **Customer-project smoke test:** Verify the **shipped artifact** in a minimal, fresh consumer Unity project (not only the SDK's own repository). Where relevant test coexistence of Chat + Video, an existing Newtonsoft install, an existing official WebRTC dependency (expect a documented conflict unless resolved), consumer `.asmdef` references, and supported player builds. Distinguish tested outcomes from proposed checks.

### Mandatory Unity Editor version-range compatibility matrix

**Why this is a release gate:** Customers commonly freeze their Editor version when they ship their game. New features, dependency updates and native plugin rebuilds must not break users on the **oldest supported Unity version**. A successful build in the latest Editor does not prove compatibility with the supported range. **Never silently raise the minimum supported version.**

**Source resolution:** For each affected SDK inspect its checked-out README, the linked official version/platform docs, `package.json` (where present), package dependency versions, release/CI setup, and any SDK-specific compatibility policy. Capture the inspected commit/version and verification date. If these sources conflict, record the contradiction and a resolution task instead of silently choosing a convenient version.

**Reference baselines only — reverify against the current sources when this spec is written:** Chat's README lists Unity **2019.x** through Unity 6 (use **2019.4** as the oldest LTS regression baseline unless an earlier 2019.x minimum is confirmed); Video currently lists **2021.3, 2022.3, 2023.1 and Unity 6**, with UPM `package.json` requiring `2021.3`. Video's native WebRTC fork **must maintain the same version range as Video**. Supported versions are **not** an open-ended guarantee for unreleased or undocumented future Unity versions.

| SDK / component | Oldest supported Editor & source | Other explicitly supported Editor families | Latest explicitly supported Editor family & source | Native/dependency implications | Evidence status |
| --- | --- | --- | --- | --- | --- |
| [Chat SDK, if in scope] | [e.g. 2019.4 LTS test baseline; README advertises 2019.x, exact minimum must be checked] | [Source-backed list] | [Source-backed value] | [C#/.NET/UPM/IL2CPP constraints] | Confirmed / Conflicting / Unverified |
| [Video SDK, if in scope] | [e.g. 2021.3; verify README-linked docs and package.json] | [Source-backed list] | [Source-backed value] | [UPM and bundled WebRTC compatibility] | Confirmed / Conflicting / Unverified |
| [Native Unity WebRTC fork, if in scope] | [Must match the Video floor; provide evidence] | [Must cover Video-supported Editors] | [Must match the Video documented maximum] | [Managed bindings, binary ABI, importer, CI outputs, architecture] | Confirmed / Conflicting / Unverified |

**Oldest-version API and dependency audit:** [Identify any changed/new APIs, UnityEngine/UnityEditor APIs, preprocessor guards, C# language features, .NET/BCL/runtime features, compilation assumptions, UPM dependency minimums, serialized formats, asset importer formats, IL2CPP restrictions, or native integration that might fail on the oldest supported Editor. For each risk name its fallback/guard and task ID. State 'No version-specific risk identified after inspection of ...' only with evidence.]

**Required verification approach:** [Specify exact Editor patch versions for oldest, representative intermediate LTS when relevant, and newest documented release; what runs in each, including compile, EditMode/PlayMode/regression tests, clean consumer package import, Editor runtime, supported platform player/IL2CPP builds, native load/ABI if Video. For changes with no version-sensitive surface, justify reduced coverage. Do not demand every platform×Editor combination; prioritize version boundaries, native integration and affected targets.]

**Version-range release gate:** [All impacted supported Editor families must remain compatible; no undocumented floor increase; any old-version regression must be fixed before claiming Done. State which required checks need an actual older Unity Editor, CI runner, native build host/device. Unsupported versions are excluded; missing test environments are **unverified**, not evidence of passing. Any proposed support drop needs explicit product approval, breaking-change record and customer migration/release notes.]

**Applicability and decisions for this change:** [Which bullets apply most directly; concrete compatibility decisions and exceptions, verified support matrix, and which task IDs own each risk.]

## 8. Ordered implementation tasks

Tasks are the primary progress tracker. Every task must be a reviewable outcome; split independent deliverables, but don't manufacture meaningless steps. Use stable IDs. Do not delete completed tasks; use `- [x]` when verified. Record any scope change in the decision log.

- [ ] **T01 — [Specific action/outcome]**
  - **Why / background:** [What in the analysis, decision, or customer risk requires this task. Include task-specific context the next agent might otherwise miss.]
  - **Work:** [Concrete implementation steps and behavior. Describe relevant algorithms/contracts and edge cases.]
  - **Touchpoints:** [Repository, files, components, public interfaces, build assets; mark uncertain paths "locate first".]
  - **Dependencies:** [None, D01, or task IDs; state blockers explicitly.]
  - **Caveats:** [What not to break, rejected approaches, platform- and Unity-version-dependent constraints; explicitly name impacts on the oldest supported Editor when relevant.]
  - **Done when:** [Observable result and acceptance criteria.]
  - **Verify:** [Named test, build, manual integration scenario or repeatable check; note if it requires CI/device access.]

- [ ] **T02 — [Next dependency-ordered outcome]**
  - **Why / background:** [Reason.]
  - **Work:** [Concrete steps.]
  - **Touchpoints:** [Paths/symbols.]
  - **Dependencies:** [T01 or none.]
  - **Caveats:** [Potential regressions.]
  - **Done when:** [Acceptance.]
  - **Verify:** [Test/build/check.]

[Continue T03, T04, ... for implementation, regression tests, **oldest-supported Unity version validation**, newest documented Unity validation, representative intermediate version if justified, native/WebRTC-to-Video version-range parity, plugin packaging, sample/consumer validation, documentation, changelog/migration, and other agreed deliverables. For version-sensitive changes assign ownership to specific task IDs and specify exact versions, fallbacks, acceptance criteria, and release gates. Add **only** justified tasks; remove placeholder/example tasks.]

## 9. Verification plan and acceptance matrix

**Automated tests:** [What tests should be added/modified, unit/integration/Editor/PlayMode, precise assertions, regressions, repeatable commands when known.]

**Manual integration / package-install checks:** [Clean Unity project, release artifact import, user code integration, upgrade path, competing dependencies, and failure scenarios. Import the shipped SDK into the **oldest supported Unity Editor**, not just the SDK development project. When Video/native WebRTC changes, verify the native fork through Video's actual shipped package at Video's version floor.]

**Unity Editor version coverage (always fill for affected Unity SDKs):**

| Editor test point | Exact Unity Editor patch version | SDK artifact / native fork revision | Scope and acceptance | Evidence / status |
| --- | --- | --- | --- | --- |
| Oldest supported Editor (**mandatory release baseline**) | [exact patch; Chat normally 2019.4 LTS baseline / Video 2021.3, reverify] | [package version/commit] | [Import, compile, API functionality, platform build and native load if applicable] | Planned / Verified / Failed / Unavailable |
| Representative middle Editor/LTS (risk-based) | [exact patch and rationale] | [version] | [Regression and affected runtime/build targets] | Planned / Verified / Failed / Unavailable / Not needed with reason |
| Newest **explicitly documented** supported Editor | [exact patch; not an assumed future release] | [version] | [Import, compile, API functionality, relevant player build] | Planned / Verified / Failed / Unavailable |
| WebRTC ↔ Video parity check (if affected) | [oldest Video + other risk-bearing Editor patches] | [native build hash and packaged Video version] | [Native + managed bindings + importer + Video player behavior work across the documented range] | Planned / Verified / Failed / Unavailable / Not applicable |

A claim like “Unity 2021.3 supported” covers a version family, not proof that every patch/build target was tested. Note exact patches/targets, extrapolations, unknowns and known failures honestly.

| Target / consumer scenario | Relevant SDK and version | Expected result | How to verify | Evidence/status |
| --- | --- | --- | --- | --- |
| [Supported Unity Editor configuration] | [Package] | [Success criterion] | [Build/test] | Not run / pass / fail |
| [Supported player platform] | [Package] | [Success criterion] | [Device/CI test] | Not run / pass / fail |
| [Existing customer dependencies / both Stream SDKs] | [Packages] | [No duplicate/conflicting dependency, or known documented limitation] | [Fresh install test] | Not run / pass / fail |

**Out-of-matrix exclusions:** [Unsupported platforms, unavailable hardware, unverified versions, and why.]

**Release gate:** [The exact set of checks that must pass before marking the spec complete. Include oldest-supported Unity Editor compatibility, no SDK version-floor regression, required newest/middle checks, and native WebRTC↔Video version parity where relevant. No unverified test should be described as passed. Explicitly mark gating checks that cannot be run in the current environment.]

## 10. Risks, edge cases, and mitigations

| Risk / edge case | Trigger and impact | Mitigation / owner task |
| --- | --- | --- |
| [Specific failure mode] | [When/why] | [Txx, approach] |

## 11. Open questions, assumptions, and blockers

| ID | State | Question / assumption | Consequence / how to resolve |
| --- | --- | --- | --- |
| Q01 | Blocking / Non-blocking / Assumed | [Specific unknown] | [What the agent can do, or who must decide] |

If nothing is open: **None — all implementation-critical decisions are resolved.**

## 12. References and handoff notes

**Material from the original discussion:** [Record key decisions, links, messages, or evidence already incorporated above. Do not require a future agent to look up the original conversation.]

**Source references:** [Permalinks/commit hashes to source files, README, docs, issues, failing tests/logs, and local paths. URLs supplement — they do not replace — the facts in this spec.]

**Progress notes:** [When implementation starts, append dated short notes for blocks/deviations, with task IDs and proof of validation. Keep this spec current so the next agent can resume from unchecked tasks.]
