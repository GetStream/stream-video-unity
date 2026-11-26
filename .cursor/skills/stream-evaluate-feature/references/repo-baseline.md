# Stream Video Unity: repository baseline

Facts an evaluation usually needs, with where each one comes from. **Last verified: 2026-11-25 at commit `30fb640`, package version `0.11.0`.** Treat every row as a starting point. Re-verify anything your verdict depends on, and note any drift in the report.

## Support contract

The sources disagree in places. Record conflicts in the report instead of picking the convenient one.

| Topic | Value at last check | Source to re-verify |
|---|---|---|
| Minimum Unity editor | 2021.3 | `Packages/StreamVideo/package.json` (`unity`) |
| Documented editor families | Unity 6, 2023.1, 2022.3, 2021.3 | https://getstream.io/video/docs/unity/platforms/overview/ (linked from root `README.md`) |
| Editors tested in CI | 2021.3.36f1, 2021.3.45f2, 2022.3.62f2, **2023.2**.20f1, 6000.0.63f1, 6000.1.17f1, 6000.2.12f1 | `.github/workflows/main.ci.cd.workflow.yml` matrix |
| Project editor | 6000.0.72f1 | `ProjectSettings/ProjectVersion.txt` |
| Documented platforms | Android ARM64 only (no ARMv7), iOS, Windows 10 x64, macOS Intel and Apple Silicon, Linux (Ubuntu 16.04 / 18.04 / 20.04) | Platform overview page above |
| Documented as unsupported | WebGL, Windows UWP | Platform overview page above |
| Platforms named in package description | Android, iOS, Windows, macOS (Linux missing) | `Packages/StreamVideo/package.json` (`description`) |
| Shipped native WebRTC binaries | `Android/libwebrtc.aar`, `iOS/webrtc.framework`, `macOS/libwebrtc.dylib`, `x86_64/webrtc.dll`, `x86_64/libwebrtc.so` | `Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime/Plugins/` |
| Versioning | Pre-1.0 (`0.x.y`). New interface members ship in minor releases (e.g. `IStreamVideoClient.CallLeaving` in 0.10.0) | `CHANGELOG.md`, git tags (`0.11.0`, older tags also as `v0.x.y`) |

Known conflicts to carry forward until they are resolved:

- Docs list **2023.1**, but CI tests **2023.2**. Neither proves the other.
- Linux is documented and its binary ships, but the package description omits it, and CI never builds or runs it.
- The fork's own `package.json` declares `unity: 2020.3`. The SDK minimum (2021.3) is what customers get; do not read the fork's value as SDK support.

## What CI actually proves

From `.github/workflows/main.ci.cd.workflow.yml`:

- Runs on Ubuntu Docker images (`unityci/editor`). Each row runs EditMode **and** PlayMode tests (`testMode: all`), then builds the sample app for **Android IL2CPP**, alternating `NET_4_x` and `STANDARD_2_x` API compatibility.
- **Not covered:** iOS (disabled, see the comment referencing PBE-5316), Windows, macOS and Linux player builds, Mono, and any real-device run.
- The workflow comment says the vendored Google.Protobuf needs **C# 9**, and that IL2CPP is always used because Unity WebRTC requires it. Treat the latter as the repo's CI policy, not proof that Mono fails everywhere; desktop and Editor still run on Mono.
- 2021.3 has two rows because its Android toolchain changed mid-LTS: 2021.3.36f1 uses Gradle 6.1.1 / AGP 4.0.1 / JDK 8, 2021.3.41f1+ uses Gradle 7.5.1 / AGP 7.4.2 / JDK 11.
- Release automation: `release_step1_create-release-pr.yml`, `release_step2_create-release-draft.yml`, and the `ReleaseTool/` .NET project.

A green CI run is evidence for "compiles, and tests pass in the Linux Editor, and an Android IL2CPP build succeeds on those editors". It is not evidence for anything else.

## Existing build-time constraints and host-project side effects

These already exist. A new feature must not make them worse without a decision.

- `Packages/StreamVideo/Editor/Android/AndroidCompileSdkValidator.cs` fails Android builds below compile SDK **33**, because background filters pull in ML Kit / androidx.core 1.9.0.
- `Packages/StreamVideo/Editor/Android/AndroidXGradlePropertiesPostprocessor.cs` sets `android.useAndroidX` in the generated `gradle.properties` when it is missing. Unity 2021.3 does not enable it by default. Existing values are left untouched.
- `link.xml` files ship in `Runtime/Core/` and `Runtime/Libs/`. UPM packages do not apply `link.xml` automatically, so `PackageXmlInstallerBase` (an `IUnityLinkerProcessor`) feeds them to the linker at build time. New reflection or serialization needs must use this path.
- On iOS the SDK configures the audio session for voice calls and restores it on leave (`Runtime/Libs/iOSAudioManagers`, CHANGELOG 0.10.0). This is a precedent for global-state changes that must be restored.
- The client creates a hidden `DontDestroyOnLoad` runner (`Runtime/Libs/VideoClientInstanceRunner/StreamMonoBehaviourWrapper.cs`) that calls `Update()` every frame and runs the WebRTC update coroutine. Outside Play Mode, the customer must call `Update()` and `Destroy()` manually.

## Repository map

| Area | Path | Notes |
|---|---|---|
| Shipped package | `Packages/StreamVideo/` | `io.getstream.video`, installed by customers via a UPM Git URL |
| Public client API | `Runtime/Core/IStreamVideoClient.cs`, `Runtime/Core/StreamVideoClient.cs` (`CreateDefaultClient`) | Entry point for evaluations |
| Stateful models | `Runtime/Core/StatefulModels/` (`IStreamCall`, `IStreamVideoCallParticipant`, ...) | Single source of truth for call and participant state |
| Device managers | `Runtime/Core/DeviceManagers/` (`IStreamAudioDeviceManager`, `IStreamVideoDeviceManager`) | Camera, microphone, and device selection |
| Low-level client and signaling | `Runtime/Core/LowLevelClient/` | Coordinator and SFU WebSockets, peer connections |
| Generated DTOs | `Runtime/Core/InternalDTO/` (OpenAPI models and `Sfu/` protobuf) | Marked `<auto-generated>`; the generator is not in this repo. Do not plan hand edits without confirming how they are regenerated |
| Other core areas | `Runtime/Core/{Configs,State,Stats,Trace,BackgroundFilters,Auth,Exceptions,QueryBuilders}` | |
| Libraries and platform glue | `Runtime/Libs/` | Websockets, HTTP, serialization, logs, network monitors, iOS audio, background filters, dependency factory |
| Vendored WebRTC fork | `Runtime/Libs/io.stream.unity.webrtc/` | `io.stream.unity.webrtc` `3.0.0-pre.8-stream.1`, assemblies `Stream.Unity.WebRTC.Runtime` and `Stream.Unity.WebRTC.Editor` |
| Editor tooling | `Editor/` (`StreamVideo.EditorTools`) | Build tools, define-symbol helpers, Android build hooks |
| Docs code samples | `DocsCodeSamples/` (`StreamVideo.DocsCodeSamples`, Editor-only) | Doc snippets compile against the real API, so a public API change must keep them compiling or update them |
| Sample project | `Samples~/VideoChat/` (shipped as a UPM sample); imported copy at `Assets/Samples/...` for development | Do not let package code depend on sample code |
| Tests | `Tests/{Editor,Runtime,Shared}` | See the testing section below |

### WebRTC fork collision risk

The fork keeps the upstream `Unity.WebRTC` namespace and the native library name `webrtc` (`Runtime/Scripts/WebRTC.cs`). A customer who also installs Unity's official `com.unity.webrtc`, or another plugin that bundles libwebrtc, can get duplicate types or native symbols. Any feature that touches the fork, its binaries, or its public types must consider this.

## Public API evolution policy

- The public surface is mostly interfaces that the SDK implements (`IStreamVideoClient`, `IStreamCall`, `IStreamVideoCallParticipant`, device-manager interfaces). Adding members to them is how features ship. Example: `CallLeaving` was added to `IStreamVideoClient` in 0.10.0.
- That is still a source break for customers who implement, wrap, or decorate these interfaces, or mock them with a framework that needs every member. Report it as **"additive interface member: breaks implementers and mocks only"** with the expected impact. It is not an automatic blocker.
- Removing or renaming members, changing signatures, defaults, event order or thread, exception types, or enum values is a real break and needs an explicit decision, a migration path, and a CHANGELOG entry.
- Avoid exposing `Unity.WebRTC` types on new public APIs unless that is deliberate.

## Testing conventions

- Follow the repo's `write-test` skill for structure and naming.
- Test assemblies compile only with `UNITY_INCLUDE_TESTS` (asmdef constraint), and every test file is wrapped in `#if STREAM_TESTS_ENABLED`. CI enables the define via `StreamVideo.EditorTools.StreamEditorTools.EnableStreamTestsEnabledCompilerFlag`.
- Runtime tests connect to Stream's **demo environment** using demo credentials (`Tests/Shared/TestClient.cs`). They need network access, can be rate-limited, and are not hermetic.
- Mocking uses NSubstitute (`net.tnrd.nsubstitute` in the root `Packages/manifest.json`).

## Related repo skills

- `stream-compare-reference-sdks`: explicit-invocation comparison with the JavaScript, Swift, and Android SDKs from sibling checkouts.
- `stream-generate-spec`: turns an accepted design into an implementation spec.
- `stream-review-branch`: reviews an implemented branch before merge.
- `write-test`: test conventions.

## Re-verification commands (read-only)

```bash
git rev-parse --abbrev-ref HEAD && git rev-parse --short HEAD && git status --short
git tag --sort=-creatordate | head -5
cat Packages/StreamVideo/package.json ProjectSettings/ProjectVersion.txt
grep -n "unity_version" .github/workflows/main.ci.cd.workflow.yml
ls Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime/Plugins/*
```

Adapt to PowerShell on Windows. Fetch the platform overview page for documented support.
