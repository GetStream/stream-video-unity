# Review checklists

Detailed checks for step 6 of the skill. Apply the sections the feature touches; skip the rest with a one-line reason. These are prompts for investigation, not boxes to fill. Report only findings that matter for the verdict.

## Compatibility audit

Compare the proposed surface with the current code and the latest release tag. Search real call sites, docs code samples, and the sample project, then consider plausible customer patterns.

- **Source:** removed or renamed symbols, namespace moves, overload ambiguity (`null`, lambdas, optional arguments, generic inference, extension methods), changed return types, parameter names (named arguments), default values, nullability, enum values and meaning.
- **Interfaces and inheritance:** apply the policy in `repo-baseline.md`. New members on SDK-implemented interfaces break implementers and mocks only. Do not rely on default interface implementations across all supported Unity versions and IL2CPP without verifying them.
- **Unity serialization:** field, type, namespace, or assembly renames, `.meta` GUID changes, prefab, scene, and ScriptableObject references, `[SerializeReference]`, inspector values. Persisted enum values must stay stable.
- **Behavior:** defaults, timing, event order and thread, exception types, reconnect behavior, permission prompts, logging volume, network traffic. Check an unchanged customer app after upgrading.
- **Wire and backend:** request and response fields, unknown enum values, protobuf and JSON tolerance, server rollout order, mixed SDK and server versions. Generated DTOs come from a generator, so do not plan hand edits.

For every break: show formerly valid customer code, who is affected, how to avoid it, and the migration path. Pick a release impact consistent with the repo's pre-1.0 history in `CHANGELOG.md`. An unavoidable break needs an explicit maintainer decision, not a routine approval.

## Platform checklist

Build the platform matrix from the documented support in `repo-baseline.md`. Split rows when constraints differ, for example Editor host vs player, or Intel vs Apple Silicon.

- **Android (ARM64):** IL2CPP; compile SDK 33+ already enforced; Gradle, AGP, and JDK differ across 2021.3 patches; manifest merging; runtime permissions; audio focus, routing, and Bluetooth; background and foreground-service limits; JNI and `.aar` packaging; R8/ProGuard; 16 KB page-size alignment for native libraries where Google Play requires it; Vulkan vs OpenGL ES; device and GPU fragmentation. Do not raise target or minimum SDK or build-tool versions without approval.
- **iOS:** IL2CPP/AOT; device vs simulator; deployment target; `webrtc.framework` slices; `RegisterPlugin.mm`; Info.plist usage descriptions; AVAudioSession category, mode, routes, and interruptions (the SDK already manages the session during calls); background modes; privacy manifest; App Store rules; Metal. CI does not build iOS, so device or manual evidence is required.
- **Windows (x64, Windows 10):** `webrtc.dll` and its runtime dependencies; audio and camera device enumeration; graphics API and driver variation; Editor vs player. UWP is unsupported and not equivalent to desktop Windows.
- **macOS (Intel and Apple Silicon):** `libwebrtc.dylib` architectures (check that the binary covers both); TCC camera and microphone prompts; entitlements; hardened runtime, signing, and notarization; Metal.
- **Linux (documented Ubuntu versions):** `libwebrtc.so` glibc and library ABI; X11 vs Wayland; audio stack; headless use. Do not generalize to all distributions. CI does not build or run Linux players.
- **WebGL / Web:** documented as unsupported. Native plugins and .NET sockets are not available; a browser backend would be a different media stack (JS interop, secure context, user gestures, autoplay, permission policy). Assess only whether the feature's API would survive such a backend.
- **visionOS, standalone Android XR (e.g. Quest), PC VR:** not supported. Android support does not imply Quest; iOS support does not imply visionOS. Consider capture and passthrough restrictions, stereo rendering, compositor and focus lifecycle, spatial audio, and frame budgets only if the request targets them.
- **Consoles (PlayStation, Xbox, Nintendo):** not supported. Requirements are partly under NDA; list unknowns and the evidence needed rather than claiming feasibility. Do not request confidential material in public reports.
- **Headless or dedicated server:** mark capture-related features N/A with the reason.

Feasibility terms: existing capability, managed-only, adapter or native work, alternative backend, blocked, N/A, unknown. Never write "supported" to imply a test was run.

## Unity version checklist

Rows: the oldest supported editor (2021.3, with the toolchain split noted), each documented family, families CI tests that docs do not list (currently 2023.2, 6000.1, 6000.2), the project editor, and the latest stable release if the request depends on new Unity features.

- C# language level and BCL profile on 2021.3 (vendored Protobuf already needs C# 9; do not assume newer features). Check both `NET_4_x` and `STANDARD_2_x` API compatibility, since CI alternates them.
- Unity API introduction and removal per version (for example `InstantiateAsync`, Awaitable, UI Toolkit features, new Input System, graphics APIs). A newer API must be behind version defines or a contained adapter, and never referenced from code that compiles on older editors.
- Package dependencies: never raise `package.json` `unity` or dependency versions to simplify implementation.
- Unity 6 is a family, not one release. Do not claim all `6000.x` versions from one patch.

## UPM package and host-project safety

For each relevant item: current mechanism, proposed change, failure mode, mitigation.

- **Install and upgrade:** Git URL install into a clean project, upgrade over an existing install, read-only package cache, coexistence with Stream Chat for Unity and with other WebRTC, voice, or video plugins (see the fork collision risk in the baseline).
- **Assemblies:** `.asmdef` references, `autoReferenced`, define constraints, version defines, platform filters, plugin importer settings and CPU filters, GUID stability. Editor code must never leak into players.
- **Scripting backend:** IL2CPP and AOT on mobile, Mono in the Editor and possibly desktop players. Watch reflection, generic instantiation, `dynamic`, marshaling, P/Invoke, reverse callbacks and delegate lifetime, and stripping at higher levels. Feed new preserve needs through the existing `link.xml` installer path.
- **Build pipeline:** Gradle and Xcode post-processing, ordering with other plugins, idempotency, never overwriting customer settings. Follow the existing pattern: read-only validation, or fill a value only when it is missing.
- **Customer environment:** Built-in, URP, and HDRP; graphics APIs; UI Toolkit vs uGUI; Addressables; additive scenes; custom audio mixers and spatializers; DI and PlayerLoop frameworks; other voice SDKs. Do not require a scene hierarchy, `MainCamera`, `AudioListener`, or singleton architecture.
- **Global state:** `Application.runInBackground`, audio configuration and DSP buffer size, frame rate, quality settings, logging handlers, PlayerLoop, synchronization context, static state. Avoid it; if unavoidable, restore it and coordinate as the iOS audio session handling does.
- **Dependency cost:** license and redistribution, codec and patent obligations, security posture, package and build size, initialization cost, update cadence, and reproducible native builds of the fork.

## Runtime, state, and lifecycle

- Initialization, connect, join, leave, end, logout, token refresh, client dispose, app quit, scene loads, and Play Mode exit with domain reload enabled and disabled.
- The client is ticked by a hidden `DontDestroyOnLoad` runner. New per-frame work runs on the main thread every frame, even for customers who never use the feature.
- Async operations under cancellation, timeout, dispose, or state transition; repeated and concurrent calls; re-entrancy from event handlers; stale callbacks from a previous call or session; out-of-order and duplicate events. No `async void` except at entry points.
- Threads: Unity main thread vs WebRTC, audio, render, and network callbacks. Marshal to the main thread before touching Unity APIs; never call customer callbacks while holding locks; bound queues.
- Ownership and teardown of native handles, delegates, textures and RenderTextures, AudioSources, tracks, coroutines, timers, and subscriptions. Disposal must be idempotent and safe after objects are destroyed.
- Device hot-plug and loss, permission denial and revocation, audio interruptions, Bluetooth and route changes, background and foreground, sleep, network loss and reconnect. Preserve explicit mute and privacy intent across reconnects.
- State: initial snapshot, local intent vs server-confirmed state, reconciliation, what resets per call vs per client.

Add a small state table or sequence for features with non-trivial lifecycle. Include at least one failure and recovery path and the behavior when the feature is unused.

## Media, signaling, and Stream features

Decide which layer owns the feature: managed SDK, WebRTC fork, native OS layer, Stream backend or SFU, or the customer app.

- **Audio:** capture vs playback, mute vs stop publishing vs release device, AEC/AGC/NS (platform-specific today), device selection, sample rate and channel conversion, Unity audio thread safety, interaction with the customer's own audio and spatialization.
- **Video:** camera and custom texture input, orientation and rotation, mirroring, color space and formats, texture ownership, GPU readback cost, render targets (including UI Toolkit), codec and hardware encoder availability.
- **Transport:** negotiation and renegotiation, ICE restart, simulcast and layer selection, subscription policy (the SDK gates video subscriptions until a participant publishes), bandwidth adaptation, server-driven quality and pause.
- **Stream features:** call and participant state, permissions and moderation, ringing, recording, HLS and livestream, backstage, custom events, call types. Check other SDKs as remote participants, and mixed client versions.
- **Backend:** verify that the endpoint or SFU message exists and is authorized for this client, plus rate limits and feature flags. Another SDK supporting it does not prove the Unity integration is complete.

Build an interaction table only for meaningful collisions: existing behavior | proposed interaction | risk | desired outcome | test.

## Performance

- Cost when enabled **and when unused**: main, render, and audio thread time, GC allocations per frame, native and texture memory, copies, queue growth, extra threads, devices, or network traffic.
- Media metrics where relevant: join time, time to first frame or audio, latency, frame pacing, encode and decode cost, bandwidth, battery, thermal.
- Scale: participant count, subscribed vs visible streams, rapid join/leave churn, reconnect storms, long sessions.
- Measure release player builds on representative low-end mobile devices, not only the Editor. Define the baseline, workload, metrics, and acceptable delta before approving. Give estimates only with stated assumptions.

## Security and privacy

- Tokens: lifetime, refresh, storage, and never logged. No embedded secrets. Demo credentials are for tests and samples only.
- Capture: no camera or microphone access before explicit intent; nothing re-enabled implicitly on reconnect, resume, or a remote action; clear distinction between device access, publishing, and server-side recording.
- Data: minimize what is collected; redact tokens and PII in logs and traces; no new analytics or remote calls by default.
- Untrusted input: malformed or hostile SFU and coordinator messages, oversized custom payloads, resource exhaustion, native memory safety.
- Platform obligations: Info.plist strings, privacy manifests, Android permissions, store policies. Flag items needing product, legal, or platform-owner decisions.

## Verification plan

Use a risk-based matrix, not a full cross-product. Always cover every supported platform at some level and the oldest editor.

- **Contract:** unchanged customer code still compiles, docs code samples compile, old scenes and prefabs load, defaults and events are unchanged.
- **EditMode:** state transitions, validation, cancellation, dispose, ordering, race conditions. Fakes via NSubstitute, following `write-test`.
- **PlayMode / integration:** demo-environment runtime tests per `write-test`; note network dependence and rate limits.
- **CI:** state which checks the existing workflow covers (Linux Editor tests, Android IL2CPP build) and which need new runners or manual work (iOS, desktop players, devices).
- **Package:** clean consumer project installing the package via Git URL; upgrade from the previous release; oldest and newest editors; IL2CPP with stripping.
- **Devices:** real Android and iOS hardware for anything touching capture, audio, permissions, or native code; desktop players for each OS affected.
- **Interop and performance:** other Stream SDKs as remote peers; enabled vs unused regression checks.

For each critical risk give one concrete test: preconditions, action, expected result. State which checks must pass before merge, before release, and before claiming support on a new platform. Never claim a test passed unless it was run.
