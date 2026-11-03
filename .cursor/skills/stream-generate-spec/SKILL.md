---
name: stream-generate-spec
description: Turn a long conversation, investigation, or agreed plan into a self-contained, implementation-ready Markdown development spec with preserved reasoning, decisions, caveats, ordered checkbox tasks, and verifiable acceptance criteria. Especially for GetStream's Unity Video SDK, Unity Chat SDK, and the native WebRTC fork. Use when asked to generate a spec, capture full context, hand off work, or turn a discussion into an actionable task checklist. Do not implement the feature.
---

# Stream Generate Spec

Create a **handoff artifact**, not a summary of the conversation. The reader may be a developer or an AI agent with **no access to this conversation**, no knowledge of the investigation, and no implicit understanding of why decisions were made.

The objective is to preserve **100% of the available implementation-relevant context**: problem, motivation, evidence, conclusions, agreed decisions, rejected approaches, implementation details, hidden dependencies, customer impact, caveats, and unresolved questions. Do not reproduce irrelevant conversational filler. Never claim to capture context that was not actually available.

## Output contract

1. Write **one real Markdown file**, using the complete structure in [`references/spec-template.md`](references/spec-template.md). Read that template before writing. Its SDK compatibility contract must appear **in the generated spec**, not only in this skill: the implementation agent may receive only the spec.
2. Default destination: `docs/specs/YYYY-MM-DD-<descriptive-kebab-case-name>.md` at the active repository root. Prefer an existing repository spec convention or an explicitly requested location. Keep planning files outside distributed Unity `Assets/`, `Packages/`, package samples, and native binaries. Never replace an existing spec silently; if continuing a spec, update it deliberately and preserve progress.
3. Output plain Markdown with **stable task IDs** (`T01`, `T02`, ...), `- [ ]` checkboxes, dependencies, concrete instructions, and testable completion criteria. Avoid vague tasks such as "fix bugs" or "make it cross-platform."
4. **Specification only.** Do not implement, refactor, run destructive commands, commit, or change runtime code unless separately requested. Read-only inspection and safe discovery commands are allowed to ground the spec.
5. If no writable workspace is available, return the entire spec as copyable Markdown and explicitly state that no file was written. Never claim a file exists without creating it.
6. Before committing a spec to a **public** repository, redact credentials, access tokens, customer data, security-sensitive information, unpublished business details, and private conversation content. Preserve technical context without exposing secrets. If redaction would destroy critical context, recommend a private handoff artifact and mark the public spec incomplete.

## Workflow

### 1. Reconstruct the discussion before designing tasks

Review the entire accessible conversation and any supplied notes, issue descriptions, logs, code, screenshots, or previous specs. Extract and distinguish:

- **Problem and motivation:** observed vs expected behavior, impact on end users and integrating customers, symptoms, reproduction, examples, desired outcome.
- **Investigation:** causes considered, evidence, experiments and outcomes, existing implementation and constraints, links and exact file/code references.
- **Decisions:** explicit agreements and their reasoning, chosen design, rejected alternatives and *why*, tradeoffs, intentional compromises.
- **Requirements:** functionality, nonfunctional behavior, API shape, backward compatibility, platform/build requirements, performance, security, packaging, documentation, tests.
- **Caveats:** edge cases, lifecycle/threading issues, failure modes, dependencies on other work, limitations, migration and breaking-change risks.
- **Unfinished business:** assumptions, competing interpretations, unanswered questions, blocked decisions, proposals that were *not* accepted.

Use the **latest explicit decision** when the discussion changes direction, but retain superseded choices when their rejection explains the design. Distinguish a user decision from an agent suggestion. Do not upgrade a tentative idea into a requirement.

### 2. Identify the actual repository and source-of-truth snapshot

The work usually concerns one or more of these independent codebases:

| Codebase | Source | Distribution consideration |
| --- | --- | --- |
| Stream Video for Unity | `https://github.com/GetStream/stream-video-unity` | Unity Package Manager package, typically under `Packages/StreamVideo` |
| Stream Chat for Unity | `https://github.com/GetStream/stream-chat-unity` | SDK distributed as a `.unitypackage` imported into the customer's `Assets/` |
| Stream's modified Unity WebRTC native package | Locate the actual fork from the workspace, dependency metadata, or discussion; **do not invent its repository URL** | Native, per-platform dependencies consumed by Video |

When files are available, inspect only what is relevant: repository `README.md`, package metadata, changelog, `.asmdef` files, plugin importer settings, `.meta` files, relevant runtime/editor source, test layout, CI/platform matrix, sample code, and references to the WebRTC fork. Record exact paths and the branch/commit/version inspected when available.

**Verify the complete documented Unity Editor version range for each affected SDK before specifying its implementation.** Start with the checked-out repository README, then follow its linked platform/support docs and compare package metadata, dependency floors and release/CI configuration. Record the **oldest supported Editor version, each explicitly documented version family, and the newest explicitly supported family**; do not assume an unspecified future Unity release is supported. Chat, Video and the native fork must not inherit each other's limits by accident. Resolve or clearly flag contradictions between sources and identify the source/commit/date used. If you cannot access authoritative sources, mark the range **unverified** and add a prerequisite discovery task; do not invent a version range.

Source discipline:

- Use **conversation history** for intent, priorities, and decisions; use **source code/configuration** for verified implementation facts.
- Quote or paraphrase critical discoveries **inside the spec**, with a source path, version, and optionally a link. An external link is a pointer, not a replacement for essential context.
- Distinguish `Confirmed`, `Inferred`, `Assumed`, and `Unknown` where the difference matters.
- Do not hallucinate symbols, file paths, version numbers, tests, platforms, or the cause of a bug. If exact details are missing, write a discovery task with a concrete success condition.

### 2a. Unity version-range compatibility is a mandatory release constraint

**Why:** Stream's SDKs run in customer projects whose Unity Editor version may be frozen for years after release. Passing tests on the SDK developer's newest Editor does **not** establish compatibility for those customers. A new SDK release must continue to work on the **oldest documented supported version** and across the documented range, unless dropping support was explicitly approved as a separate breaking-change decision.

**Current reference baselines (re-check, never hardcode as timeless truth):**

| Affected component | Baseline when this skill was reviewed (2026-11-02) | Verify against |
| --- | --- | --- |
| Chat Unity SDK | Unity **2019.4** as the practical oldest LTS regression baseline. The `develop` README actually advertises **2019.x**, so confirm whether releases promise pre-2019.4 editors before asserting an exact contractual minimum. | `https://github.com/GetStream/stream-chat-unity/blob/develop/README.md` and release/package configuration |
| Video Unity SDK | Unity **2021.3** minimum; current Video platform docs list **2021.3, 2022.3, 2023.1, Unity 6**. | `https://github.com/GetStream/stream-video-unity/blob/main/README.md`, linked `https://getstream.io/video/docs/unity/platforms/overview/`, and `Packages/StreamVideo/package.json` (`unity`) |
| Video's custom Unity WebRTC/native plugin | **Same Unity Editor and platform support as the Video SDK**, end to end. Do not silently narrow Video's compatibility via a native fork update. | Actual fork source/build outputs, importer settings, dependency version, and Video consumer builds; locate the correct fork (do not confuse the modified Unity package with a separate libwebrtc source fork) |

When generating a spec for any of these repositories, **put the resolved version range and its provenance in the spec itself**, and explicitly plan for:

1. **Oldest-first design:** Check every new/changed Unity API, editor/build API, C# language feature, compiler/runtime API, .NET profile, package dependency, graphics/audio/native facility, and serialization feature against the **minimum supported Editor**. Do not use a newer feature unconditionally when older Editors lack it. If necessary, specify compatible fallback behavior or tightly scoped `#if UNITY_...` guards, including how both guarded paths are tested. A `#if` is not an excuse to drop the feature on old versions without a product decision.
2. **Dependency compatibility:** Cross-check `package.json`/UPM minimum Unity version, Unity package availability, package and transitive dependency versions, `.asmdef` version defines/constraints, `.meta`/importer formats, and any tooling that generates packaged output. A higher package floor can break customers **before code runs**.
3. **Cross-version regression coverage:** Require a planned **oldest supported** Editor test, **newest explicitly documented** Editor test, and a representative intermediate supported Editor/LTS when material to the change. Choose exact editor patch versions in the implementation environment and record them in the spec; don't claim a whole major/version family was tested from one patch. For version-sensitive changes include relevant EditMode/PlayMode, C# compile, consumer package import, Editor, player, Mono/IL2CPP/AOT, and supported platform checks. Use a **risk-based matrix** rather than demanding every cross-product combination; explain any untested cells and who owns verification.
4. **Native fork parity:** For Video or WebRTC changes, require the fork's **managed C# bindings, native binaries, native build toolchain/output, plugin import settings, ABI/architecture and packaged dependencies** to function in **every Unity Editor family promised by Video** on the affected supported platforms. Prove via the Video SDK's shipped UPM package in consumer Unity projects, not just a native library compiling independently. Verify Editor loading *and* player builds/runtime as relevant.
5. **No silent floor increases:** Do not solve a build failure by raising `package.json`'s `unity` field, upgrading required APIs or dependencies, changing compiler settings, or removing old-version guards if that excludes a documented customer cohort. Any proposed support drop must be clearly flagged as **breaking**, with rationale, affected releases/customer segments, migration/release communication, and explicit approval; do not treat it as an ordinary implementation choice.
6. **Evidence and honesty:** Label each version/target cell `Verified`, `Planned`, `Unavailable`, `Failed`, or `Excluded (not supported)`. If old Unity Editors, native build environments or devices aren't available, create a trackable validation task and preserve the risk; never mark a release gate passed by inference from a newer Editor.

The generated spec must contain a **Unity Editor Compatibility Matrix**, the oldest-version API audit, version-specific caveats, a testing/verification plan and an explicit release gate. Keep the version information in the final spec even if the next agent never reads this skill.

### 3. Specify the solution and the work

Read `references/spec-template.md` and fill **all applicable sections**. Replace template text with actual facts; use `Not applicable — <reason>` for genuinely irrelevant sections. Describe:

- Architecture and intended behavior, including before/after data or control flow when relevant.
- What changes and what **must not** change; public APIs, serialization, files, assembly boundaries, package contents, and customer-facing behavior.
- Decision records (with rationale and rejected alternatives). For important tasks, also state **why this task exists**, not just what to edit.
- Concrete tasks ordered by dependency. Each task needs an ID, checkbox, objective, background/rationale, target components or paths (if known), implementation guidance, dependencies, edge cases, "done when", and verification.
- Separate implementation from validation, packaging/integration, and documentation where they are independently trackable deliverables. Include regression tests for changed behavior.
- An explicit definition of done and a **platform/consumer verification matrix** appropriate to this repo and change.

Do **not** overprescribe exact internals when multiple implementations would satisfy the agreed constraints. Conversely, if the discussion settled on a particular API, threading model, ownership rule, or approach, preserve it rather than leaving it as an open design choice.

### 4. Carry the Unity SDK consumer contract into every relevant spec

**Non-negotiable framing:** These repositories build Unity SDKs/plugins consumed inside **customers' existing Unity projects**. A change is not good merely because it works in the SDK's own project. It must coexist with unrelated customer code, other assets/plugins, other Stream SDKs, Unity versions, and supported build targets.

The template contains the complete **SDK Consumer Compatibility Contract**. Include it in the final document, apply every relevant item to actual tasks/acceptance criteria, and mark exclusions with a reason. At minimum evaluate:

1. **Namespaces / API surface:** Keep new managed code under the SDK's established `StreamVideo.*` / `StreamChat.*` namespace conventions (verify exact project structure); avoid global-namespace types, overly generic public names, broad extension methods, unintentional public APIs, generic editor menu/define names, and unrelated project-wide side effects. Do not rename existing public namespaces just to conform to this guidance.
2. **Assemblies / dependency collisions:** Distinct, stable `.asmdef` names and references; runtime code cannot depend on editor-only assemblies. Avoid duplicate DLLs, types, packages, native symbols, or dependency versions. Specifically consider Unity's official `com.unity.webrtc` versus Stream's modified WebRTC package and multiple `Newtonsoft.Json` copies when applicable.
3. **Packaging and isolation:** Respect the repo's actual distribution model (`.unitypackage`, UPM, or native plugin); ensure required files, `.meta` GUIDs, `link.xml`, platform libraries, import settings, sample and editor files, and package dependencies are correctly scoped. Do not change global customer settings, scripting defines, project assets, or manifests without explicit justification and documentation.
4. **Supported Unity versions and platforms:** Preserve the **entire currently documented Editor version range**, especially the oldest customer-supported Unity Editor, as well as the affected package’s documented platforms. A compatibility claim requires an explicit version matrix and evidence, not only success on the newest Editor. Audit older Unity APIs/C#/BCL and dependency floors; test/plan Editor **and** player builds where applicable, IL2CPP/AOT/stripping, native architectures/import filters, permissions and platform conditionals. The Video native fork must not narrow Video’s range. Never promise Video on a platform merely because Chat supports it.
5. **Customer lifecycle and performance:** Unity main-thread rules, proper resource disposal, event unsubscribe, reconnect/reload/domain-reload behavior, additive scenes, reimport/recompile, and memory/GC/performance overhead where applicable.
6. **Public API and upgrade safety:** Compatibility for existing installations, serialized data, Unity GUIDs, interfaces, events, and package versions. If a breaking change is necessary, require an explicit migration/release note and a deliberate decision.
7. **Integration proof:** Validate the delivered/package-imported form in a **clean consumer Unity project**, not solely the source repository; test co-installation with the other Stream SDK and common conflicting dependencies when relevant. Distinguish tested vs merely expected results.

When a task involves the WebRTC fork, additionally capture native ABI/architecture coverage, plugin importer constraints, bundle/framework/library naming collisions, transitive native dependencies, licensing/attribution, patch preservation, and how the Video package includes/releases the fork. Never assume the official WebRTC package and the fork may be loaded side by side.

Do not blindly demand tests on unsupported targets. The matrix should be **accurate and scoped**, explaining untested target coverage or CI limitations.

### 5. Quality gate before writing the file

Evaluate the draft as if handing it to an agent with **only that Markdown file**:

- Can the agent explain *what failed / what is needed*, *why*, *what was decided*, and *why alternatives were rejected* without reading the old conversation?
- Does each task say **what to do, where, why, relevant caveats, dependencies, and how to prove it is done**?
- Are all important decisions, explicit constraints, examples, prior failed attempts, and platform/customer risks preserved? Are irrelevancies omitted?
- Are unverified assumptions and **blocking** questions explicit, rather than hidden guesses? Does the status correctly say `Ready`, `Ready with assumptions`, or `Blocked`?
- Does the spec **name and source the supported Unity version range**, protect the minimum Editor version, identify old-version API/dependency/native risks, and include oldest/newest/middle testing and the WebRTC-to-Video support-parity gate where relevant? Are unrun tests explicitly unverified?
- Is the final spec free of fake paths, invented results, private secrets, dangling placeholders, and references like "as discussed above"?
- Can progress be tracked by changing `- [ ]` to `- [x]` without renumbering tasks, and can a later agent resume from the remaining unchecked tasks?

If critical choices prevent safe implementation, do **not** fabricate them. Prefer producing the useful partial spec with clearly marked blocker(s) over refusing to write anything. Ask only questions that actually block an accurate handoff, and keep them in the file.

### 6. Deliver

Write the Markdown file; verify it exists, is valid UTF-8, uses Markdown checkboxes, and contains the SDK contract **and version-range compatibility matrix** where applicable. Reply with its path and a **brief** indication of any blocking decisions or unverified facts. Do not start implementation on behalf of the next agent.

## Template

**Required:** [`references/spec-template.md`](references/spec-template.md). Treat the template as a structure to fill, not boilerplate to copy unchanged. The generated spec must stand alone; it must not say "refer to the skill" for constraints or rationale.
