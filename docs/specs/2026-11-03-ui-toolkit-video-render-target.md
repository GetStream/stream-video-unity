# Render remote video into UI Toolkit: `StreamVideoTrack.SetRenderTarget(UnityEngine.UIElements.Image)`

> **Status:** Ready with assumptions (see Q01–Q03; Q02 gates only T03)
> **Created/updated:** 2026-11-03
> **Repository / scope:** Stream Video for Unity (`GetStream/stream-video-unity`), package `Packages/StreamVideo`. The WebRTC fork (`GetStream/com.unity.webrtc`) was inspected for root cause only and is **not changed**.
> **Source snapshot:** branch `feature/uni-211-create-new-sample-project`, commit `d12c2633bec0a09604394f10d27b0549f4fc5010`. Package `io.getstream.video` `0.11.0`. `origin/main` was at `33e73834578a4e300159462549beaf65ddc74946`. WebRTC fork inspected at branch `feature/uni-213-upgrade-webrtc-to-m145`, commit `be1b6764e2c93abd513489806e37bcf07370e205`.
> **Deliverable:** UI Toolkit customers render a participant's video with one call, `videoTrack.SetRenderTarget(image)`. They no longer need a hidden uGUI `RawImage` or per-frame polling. The UI Toolkit sample is simplified to use the new call.
> **Intended reader:** A developer or AI agent who has **not** read the original discussion.

> **Handoff rule:** This spec is the authoritative record of the agreed scope, background, decisions, and completion criteria. Read it completely before editing code. Do not assume undocumented context. Record material changes to the plan and the reasons in the decision log. Check off a task only when its acceptance and verification conditions pass.

## 1. Outcome and definition of done

**Goal:** `StreamVideoTrack` can render received video directly into a UI Toolkit `UnityEngine.UIElements.Image`, with the same lifecycle semantics as the existing uGUI `SetRenderTarget(RawImage)`. The SDK, not customer code, picks up the new texture instance after every incoming resolution change.

**User/customer impact:**
- Customers building UI with UI Toolkit (`VisualElement`, UXML/USS) no longer need to create inactive `GameObject` + `RawImage` holders per participant or read `.texture` every frame.
- Existing uGUI customers see no behaviour change.

**Definition of done:**
- `StreamVideoTrack.SetRenderTarget(UnityEngine.UIElements.Image)` is public, documented, and works on Unity 2021.3 (the package floor) and Unity 6. Remote video renders, survives resolution changes, and rebinds after reconnect when the customer re-calls `SetRenderTarget`.
- The existing `SetRenderTarget(RawImage)` behaviour is unchanged.
- The UI Toolkit sample no longer creates hidden `RawImage` objects. The local preview still re-crops correctly after in-place resizes.
- CI (Android IL2CPP matrix 2021.3 → 6000.2) is green.
- The new runtime test passes where a camera is available.
- A clean consumer project on the oldest and newest documented Editors compiles and renders video through the shipped package.
- A release-note entry is provided.

## 2. Problem statement and full background

### Current behavior / problem

Confirmed from source:

- The only public way to display a remote participant's video is `StreamVideoTrack.SetRenderTarget(RawImage)` (`Packages/StreamVideo/Runtime/Core/StatefulModels/Tracks/StreamVideoTrack.cs`). The underlying WebRTC track is `protected TTrack Track` in `BaseStreamTrack<TTrack>` (`BaseStreamTrack.cs`), so customers cannot read the texture directly.

  ```csharp
  //StreamTodo: can we remove Unity dependency?
  public void SetRenderTarget(RawImage targetImage)
  {
      if (targetImage == null) throw new ArgumentNullException(nameof(targetImage));
      _targetImage = targetImage;
  }

  internal override void Update()
  {
      base.Update();
      if (_targetImage == null) return;
      _targetImage.texture = Track.Texture;   // re-assigned every frame
  }
  ```

- The SDK calls `Update()` every frame through `StreamVideoCallParticipant.Update()`, which updates `_audioTrack`, `_videoTrack` and `_screenShareTrack`.
- UI Toolkit has no `RawImage`. The UI Toolkit sample works around this in `Packages/StreamVideo/Samples~/VideoChatUIToolkit/Scripts/Views/CallParticipantTile.cs` (`GetOrCreateVideoTarget`, roughly lines 124–140):
  - It creates an **inactive** `GameObject` with a `RectTransform` and `RawImage` per remote participant, parented under the app.
  - It passes that `RawImage` to `SetRenderTarget`.
  - Every frame it reads `_videoTarget.texture` and assigns it to a UI Toolkit `Image` inside `VideoSurface`.
  - It destroys the holder `GameObject` in `Dispose`.
- Nothing is converted between UI systems. The video is an ordinary `UnityEngine.Texture`, and UI Toolkit's `Image.image` accepts it directly. The `RawImage` is only a texture holder.
- **Why the texture cannot simply be assigned once (root cause of per-frame polling):** the WebRTC fork creates a new receive texture on every incoming resolution change and destroys the old one.
  - Bundled managed copy: `Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime/Scripts/VideoStreamTrack.cs`, `UnityVideoRenderer.OnVideoFrameResizeInternal`, calls `WebRTC.DestroyOnMainThread(Texture)` and then `Texture = new Texture2D(width, height, format, TextureCreationFlags.None)`.
  - Native trigger: `Plugin~/WebRTCPlugin/UnityVideoRenderer.cpp`, `SetFrameBuffer`, invokes the resize callback whenever the frame width or height changes.
  - Resolution changes are routine: simulcast layer switches, bandwidth adaptation, and the requested-resolution changes customers make with `UpdateRequestedVideoResolution`.
- **The local camera preview needs no workaround.** `IStreamVideoDeviceManager.GetLocalPreviewTexture()` already returns a `Texture`. Its doc says: "Assign it once (e.g. to a RawImage); the same instance is returned for the lifetime of the client… It is black until the camera delivers frames and is **resized in place** when the video size or orientation changes." The implementation is `LocalPreviewRenderer.EnsureSize` (`Packages/StreamVideo/Runtime/Core/BackgroundFilters/LocalPreviewRenderer.cs`): it releases the texture, changes width and height, and calls `Create()` on the same instance. The texture starts as a 16×16 placeholder.
- The sample's `VideoSurface` (`Samples~/VideoChatUIToolkit/Scripts/Views/VideoSurface.cs`) implements cover-fit and rotation by manually sizing an absolutely positioned `Image` every frame. **Every caller passes rotation `0`**: `CallParticipantTile.Update` for both local and remote tiles, and `LobbyScreen.OnUpdate`.

### Expected behavior

- `videoTrack.SetRenderTarget(uiToolkitImage)` makes the SDK keep `uiToolkitImage.image` pointing at the track's current received texture. It updates the image whenever the WebRTC fork swaps the texture instance, on the main thread, from the existing `Update()` loop.
- Customer code mirrors the uGUI path:

  ```csharp
  var video = root.Q<UnityEngine.UIElements.Image>("video");
  video.scaleMode = ScaleMode.ScaleAndCrop;   // cover-fit
  videoTrack.SetRenderTarget(video);
  ```

- **One render target per track: whichever overload was called last wins.** Calling either overload clears the other target.
- `SetRenderTarget(RawImage)` behaves exactly as today.

### How we arrived at this plan

1. **Starting point.** A draft tutorial described the UI Toolkit sample as answering "how do you get a live WebRTC texture from one UI system into the other" with "a small rendering bridge". The question was how to make this simpler for customers. The draft is unpublished and not in this repo.
2. **First agent analysis (superseded).** It recommended a public UI-agnostic `Texture` getter plus a `TextureChanged` event on `StreamVideoTrack`. It also suggested optionally changing the WebRTC fork to keep the receive texture instance stable with `Texture2D.Reinitialize(w, h)`, which needs Unity 2021.2+ (the floor is 2021.3). It argued *against* a UI Toolkit overload for three reasons: (a) `SetRenderTarget(null)` becomes ambiguous, (b) it couples the SDK to a second UI system, (c) it doesn't solve fit or crop.
3. **Second analysis (provided by the user; adopted direction).**
   - It recommends adding `SetRenderTarget(UnityEngine.UIElements.Image)`, roughly 15 SDK lines, so customers write one line instead of the bridge.
   - It rejects the Texture getter as a footgun, because the instance is destroyed on resize.
   - It rejects an SDK `VisualElement` because of the large API surface and the UXML declaration differences between Unity 2021.3 and 2023.2+.
   - It rejects a separate UI Toolkit assembly because consumers with their own asmdefs would need an extra reference.
   - It notes that `ScaleMode.ScaleAndCrop` replaces most of `VideoSurface`.
4. **Review of the second analysis against source (this spec's corrections).** It confirmed the core claims and reversed the first analysis's objections:
   - (a) The ambiguous `null` call throws `ArgumentNullException` today anyway.
   - (b) UI Toolkit is a built-in engine module, much as uGUI is already an SDK dependency.
   - (c) `ScaleAndCrop` handles cover-fit for remote video, because remote textures are swapped rather than resized in place.

   The review also found four gaps, all incorporated below:
   - **`ScaleAndCrop` alone is not equivalent for the local preview.** The preview `RenderTexture` is resized *in place*: from the 16×16 placeholder to the camera size, when the device rotates (width and height swap), and when publishing starts. A UI Toolkit `Image` regenerates geometry only when marked dirty, for example when `image` is set to a different instance. In-place size changes therefore leave a stale or stretched crop. The sample must keep a small size-change check that calls `MarkDirtyRepaint()`. This is *inferred* from UI Toolkit's dirty-repaint model and not yet tested.
   - **"No new package dependency" holds only relative to this branch.** The runtime's only other UI Toolkit usage, `Packages/StreamVideo/Runtime/Libs/KeyboardAvoidance/*` in the `StreamVideo.Libs` assembly, is **not** on `origin/main` or in the `0.11.0` tag. `package.json` declares neither `com.unity.ugui` (although `StreamVideo.Core` uses `RawImage`) nor `com.unity.modules.uielements`.
   - **The original sketch was incomplete.** The `RawImage` overload must also clear the UI Toolkit target. The signature must use the fully qualified `UnityEngine.UIElements.Image`, because `StreamVideoTrack.cs` already has `using UnityEngine.UI;` and `UnityEngine.UI.Image` exists. Clearing the element's image on track dispose is worth considering.
   - **The "frozen frames from UI Toolkit dynamic atlas" risk is low.** The sample already assigns this exact received `Texture2D` to `Image.image`, so the overload adds no new rendering path.
5. The user then requested this handoff spec. The overload decision, the review corrections, and the provisional items are recorded below.

### Why this matters for downstream Unity customers

- UI Toolkit is Unity's recommended runtime UI system for new projects. Every UI Toolkit customer currently has to discover and re-implement the hidden-`RawImage` workaround, including per-frame polling and holder `GameObject` lifecycle. Getting it wrong (assigning the texture once) produces frozen or blank video after the first simulcast switch.
- The fix is additive. Existing uGUI integrations, binaries and serialized data are unaffected.

## 3. Scope and non-goals

**In scope**

- New public overload `StreamVideoTrack.SetRenderTarget(UnityEngine.UIElements.Image)`, with mutual clearing of targets, per-frame update, XML docs, and (provisionally, D05) clearing the image on dispose.
- Tests for the new overload (runtime, camera-gated; Editor tests if feasible).
- UI Toolkit sample simplification in **both** tracked copies: `Packages/StreamVideo/Samples~/VideoChatUIToolkit/` and `Assets/Samples/Stream Video & Audio Chat SDK/0.11.0/Video Chat UI Toolkit Example/`. The two copies are currently byte-identical.
- A docs code sample in `Packages/StreamVideo/DocsCodeSamples/`, a docs-site update (external repo; locate it first), and a release-note text.
- Provisional (Q02): declare `com.unity.ugui` and `com.unity.modules.uielements` in `Packages/StreamVideo/package.json`.
- Validation across the documented Unity range and a clean consumer project.

**Out of scope**

- Any WebRTC fork change, managed or native. That includes making the receive texture stable with `Texture2D.Reinitialize` (deferred, D07).
- A public `Texture` getter or change event on `StreamVideoTrack` (deferred, D03).
- An SDK-provided UI Toolkit `VisualElement` or UXML control (rejected, D04).
- Automatic rebinding of the render target after reconnect, which creates a new track.
- Automatic requested-resolution management based on element size.
- A UI Toolkit helper for the local preview resize issue (document it; see Q07).
- Changing `IStreamVideoCallParticipant.VideoTrack`'s weak `IStreamTrack` type (existing `StreamTodo`).
- The sample's existing behaviour of binding both camera and screen-share `StreamVideoTrack`s to the same tile target. Do not change it here.

**Must preserve**

- `public void SetRenderTarget(RawImage targetImage)`: signature, `ArgumentNullException` on null, and per-frame `texture` assignment while the `RawImage` is alive (Unity-null check). No clearing on dispose.
- `StreamVideoTrack` namespace `StreamVideo.Core.StatefulModels.Tracks`, the class shape, and the existing members (including the internal `TargetTexture` stub).
- The `StreamVideo.Core` assembly name and references. Do not add an explicit asmdef reference unless required, and record it if you do.
- No serialized data or GUID changes to runtime assets. When sample files are deleted, delete their `.meta` files in both copies.
- **Unity Editor support floor and documented range:**
  - Minimum **Unity 2021.3** (`Packages/StreamVideo/package.json` `"unity": "2021.3"`; public docs).
  - Documented families: **2021.3, 2022.3, 2023.1, Unity 6** (<https://getstream.io/video/docs/unity/platforms/overview/>, fetched 2026-11-03).
  - Do not raise the floor. Customers often freeze their Editor version for years after shipping.

## 4. Decisions, rationale, and alternatives

| ID | Decision (confirmed/provisional) | Why / evidence | Alternatives rejected and why |
| --- | --- | --- | --- |
| D01 | **Add `public void SetRenderTarget(UnityEngine.UIElements.Image targetImage)` to `StreamVideoTrack`.** Status: adopted direction (from the user-provided analysis; endorsed by review; assumed accepted, see Q01). | Smallest change that removes the workaround. It reuses the existing `Update()` polling, which is the right place because the fork swaps texture instances on resize. Customer code becomes one line. | (a) Docs-only plus the sample workaround: every customer would re-implement polling. (b) First analysis's objections: `null` ambiguity is negligible because the call throws today, coupling is acceptable because UI Toolkit is a built-in module, and fit is solved by `ScaleAndCrop`. |
| D02 | **One render target per track; last call wins.** Each overload clears the other target field. Confirmed in the adopted analysis. | Simple, predictable, and mirrors the existing single-target semantics. | Multiple simultaneous targets: speculative and not requested. |
| D03 | **Do not add a public `Texture` getter or change event now.** Provisional deferral. | The instance is destroyed on resize, so a cached reference goes stale. Add it when a 3D-surface use case appears. | Getter plus `TextureChanged` event (first analysis): useful for materials and 3D, but a footgun without a stable texture. |
| D04 | **No SDK-provided UI Toolkit `VisualElement` or UXML control.** | Large public surface. Custom UXML elements are declared with `UxmlFactory`/`UxmlTraits` on 2021.3 and with `[UxmlElement]` on 2023.2+. No uGUI equivalent. | — |
| D05 | **On track dispose, set the UI Toolkit target's `image = null` if it still references this track's texture, then drop the reference.** Kept. SDK disposal runs on the main thread (see progress notes, 2026-11-04). | UI Toolkit elements are not `UnityEngine.Object`s and never become "null". Otherwise the element keeps a destroyed texture until the customer retargets. The reference-equality guard avoids clobbering another track (e.g. screen share) that targets the same element. The `RawImage` path is left unchanged. Public `Dispose()` is still main-thread-only, same as the rest of this type; the XML docs say so. | Leave the image untouched (matches the `RawImage` path; customers manage visibility themselves). |
| D06 | **Use the fully qualified `UnityEngine.UIElements.Image` in the SDK signature.** | `StreamVideoTrack.cs` has `using UnityEngine.UI;`, so a bare `Image` would mean or conflict with uGUI `UnityEngine.UI.Image`. | Adding `using UnityEngine.UIElements;` (ambiguous `Image`). |
| D07 | **Do not change the WebRTC fork.** Deferred: stable receive texture via `Texture2D.Reinitialize`. | Render-thread and native-pointer (`TexturePtr`) risk across D3D11, D3D12, Metal, Vulkan and GLES. Not needed for this deliverable. | — |
| D08 | **Declare `com.unity.ugui` and `com.unity.modules.uielements` as `package.json` dependencies.** **Provisional, needs product decision (Q02).** | `StreamVideo.Core` uses `RawImage` (uGUI), and on this branch `StreamVideo.Libs` and (after T01) `StreamVideo.Core` use UI Toolkit. Neither is declared, so a project without them probably fails to compile the SDK. | Leave undeclared (status quo; relies on Unity's default project template). |
| D09 | **No separate UI Toolkit assembly or extension method.** | Customers with their own asmdefs would have to add a reference for no gain. | — |
| D10 | **Sample: replace the manual cover-fit math with `Image.scaleMode = ScaleMode.ScaleAndCrop` and fill-parent styling. Keep a size-change check that calls `MarkDirtyRepaint()` for the local preview. Drop the unused rotation parameter.** | All callers pass rotation 0. Remote textures are swapped, so assigning a new instance re-crops. The local preview is resized in place, so it needs an explicit repaint. | Deleting `VideoSurface` logic entirely (would break preview crop after rotation or resize). |
| D11 | **Do not call `SetRenderTarget` for the local participant's tile.** Added during implementation. | The local tile displays `GetLocalPreviewTexture()`, which is a different instance from the published track texture. Both would write the same UI Toolkit `Image` and replace each other every frame. The previous hidden `RawImage` was not what the local tile displayed. Remote camera and screen-share tracks still share one target. | Binding the local published track to the preview image. |

## 5. Repository context and existing implementation

**Relevant components and paths**

| Repository / file / symbol | Current role and behavior | Planned change / reason |
| --- | --- | --- |
| `Packages/StreamVideo/Runtime/Core/StatefulModels/Tracks/StreamVideoTrack.cs` (`SetRenderTarget(RawImage)`, `Update()`, `OnDisposing()`, `_targetImage`) | Holds an optional `RawImage` and assigns `Track.Texture` to it every frame. | Add the UI Toolkit overload, mutual clearing, update branch, XML docs, and dispose clearing (D05). T01. |
| `.../Tracks/BaseStreamTrack.cs` | `protected TTrack Track`; `public void Dispose() => OnDisposing();`; `internal virtual void Update()`. | No change. |
| `Packages/StreamVideo/Runtime/Core/StatefulModels/StreamVideoCallParticipant.cs` (`Update()`, track creation around lines 280–320) | Calls `Update()` on the audio, video and screen-share tracks each frame. On a new remote track it disposes the old `StreamVideoTrack` and creates a new one, which fires `TrackAdded`. | No change. The new track needs `SetRenderTarget` re-called (documented). |
| `Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime/Scripts/VideoStreamTrack.cs` (`UnityVideoRenderer.OnVideoFrameResizeInternal`, `Texture`) | Destroys and recreates the receive `Texture2D` on every resolution change. | No change (D07). Relied upon: the instance swap triggers the UI Toolkit `Image` to re-crop. |
| `Packages/StreamVideo/Runtime/Core/BackgroundFilters/LocalPreviewRenderer.cs` (`GetOrCreate`, `EnsureSize`) | Stable `RenderTexture`, resized in place, 16×16 placeholder. | No change. The sample and docs must handle in-place resize (T04, T05). |
| `Packages/StreamVideo/Runtime/Core/DeviceManagers/IStreamVideoDeviceManager.cs` (`GetLocalPreviewTexture`) | Documents "assign once… resized in place". | Optional doc note for UI Toolkit (`MarkDirtyRepaint` on resize) in T05. |
| `Packages/StreamVideo/Runtime/Core/StreamVideo.Core.asmdef` | References `StreamVideo.Libs`, `Google.Protobuf`, `Stream.Unity.WebRTC`. No explicit `UnityEngine.UI`. | No change expected. Engine modules such as `UnityEngine.UIElementsModule` are referenced automatically (`noEngineReferences: false`). On the 6000.0.72f1 dev project, the generated `StreamVideo.Core.csproj` also references `Library/ScriptAssemblies/UnityEngine.UI.dll` implicitly. |
| `Packages/StreamVideo/package.json` | `"unity": "2021.3"`. Dependencies: `com.unity.nuget.newtonsoft-json` 3.2.0, `com.unity.modules.jsonserialize`, `com.unity.editorcoroutines`, `com.unity.modules.audio`. | T03 (provisional): add `com.unity.ugui` and `com.unity.modules.uielements`. |
| `Packages/StreamVideo/Runtime/Libs/KeyboardAvoidance/*` (`StreamVideo.Libs`) | New on this branch (not in `0.11.0` or `origin/main`). Already `using UnityEngine.UIElements;`. | No change. Evidence that this release already introduces a runtime UI Toolkit dependency. |
| `Packages/StreamVideo/Samples~/VideoChatUIToolkit/Scripts/Views/CallParticipantTile.cs` | Hidden `RawImage` holder per remote participant (`GetOrCreateVideoTarget`, `_videoTargetHost`, `_videoTarget`). Polls `.texture`. Local tile uses `GetLocalPreviewTexture()`. Also `UpdateRequestedResolution()`. | T04: use the new overload; delete the holder logic. |
| `.../Scripts/Views/VideoSurface.cs`, `.../Scripts/Views/ParticipantTileView.cs`, `.../Scripts/Screens/LobbyScreen.cs` | Manual cover-fit and rotation. `SetVideo(texture, rotationAngle, isVideoEnabled)` toggles the placeholder. | T04: `ScaleAndCrop`, expose the `Image` for `SetRenderTarget`, keep the preview size check, remove the rotation parameter. |
| `.../UI/Components/ParticipantTile.uxml`, `.../UI/Styles/ParticipantTile.uss` | `#video` container is `.participant-tile__video` (absolute, 0 insets, `overflow: hidden`). | Keep. The `Image` must fill this container (absolute 0 insets or `StretchToParentSize()`). |
| `.../Scripts/StreamVideo.ExampleProject.UIToolkit.asmdef` | References `StreamVideo.Libs`, `StreamVideo.Core`, `Unity.WebRTC`, `UnityEngine.UI`. | T04: remove `UnityEngine.UI` only if no sample file still uses it (grep first). |
| `Assets/Samples/Stream Video & Audio Chat SDK/0.11.0/Video Chat UI Toolkit Example/` | Tracked imported copy of the sample, byte-identical to `Samples~` today. The repo convention is to keep both in sync. | T04: mirror every change. |
| `Packages/StreamVideo/Samples~/VideoChat/Scripts/UI/ParticipantView.cs:198`, `Packages/StreamVideo/DocsCodeSamples/01-basics/{ParticipantView.cs:70, QuickStartCodeSamples.cs:121}` | uGUI usages of `SetRenderTarget(RawImage)`. | Must keep compiling unchanged (regression check). T05 adds a UI Toolkit docs sample. |
| `Packages/StreamVideo/DocsCodeSamples/StreamVideo.DocsCodeSamples.asmdef` | Editor-only, references `StreamVideo.Core` and `StreamVideo.Libs`. Compiled snippets for the docs site. | T05: add a UI Toolkit snippet. |
| `Packages/StreamVideo/Tests/Runtime/CallsTests.cs` | Camera-gated two-client tests that obtain a remote `StreamVideoTrack` (`When_client_joins_call_with_video_expect_receiving_video_track`). | T02: model new tests on it. |
| `.github/workflows/main.ci.cd.workflow.yml` | Android IL2CPP build and tests (`testMode: all`) on 2021.3.36f1, 2021.3.45f2, 2022.3.62f2, 2023.2.20f1, 6000.0.63f1, 6000.1.17f1, 6000.2.12f1. | Used for T06 and T07 evidence. |
| `CHANGELOG.md` | Only modified by "Release version X" commits. The changelog is passed to `ReleaseTool` via the `release_step1_create-release-pr.yml` input `changelogBase64`. | Do **not** edit it directly. Provide release-note text (T09). |

**Key interfaces, dependencies, and data flow**

- Today (UI Toolkit sample):
  1. The WebRTC receive callback (`WebRTC.Sync`, main thread) runs `OnVideoFrameResizeInternal` and creates a new `Texture2D`.
  2. `StreamVideoCallParticipant.Update()` calls `StreamVideoTrack.Update()`, which sets hidden `RawImage.texture = Track.Texture`.
  3. The sample reads `RawImage.texture` and sets `VideoSurface` `Image.image`.
  4. Frame pixels are uploaded to the same `Texture2D` each frame on the render thread (`CommandBuffer.IssuePluginCustomTextureUpdateV2` → native `ConvertVideoFrameToTextureAndWriteToBuffer`), so UI Toolkit shows live video without reassigning.
- After this change:
  1. Same fork behaviour.
  2. `StreamVideoTrack.Update()` sets `targetUIToolkitImage.image = Track.Texture` when the reference changed.
  3. UI Toolkit's `Image.image` setter marks layout and repaint dirty on an instance change, so `ScaleAndCrop` recalculates.
- `SetRenderTarget` and `Update` run on the main thread. `Update` is driven by the client's update loop.
- UI Toolkit `Image` API on the floor Editor (Confirmed, Unity 2021.3 Scripting API page `UIElements.Image`): `image`, `scaleMode`, `sourceRect`, `uv`, `tintColor`, `MarkDirtyRepaint()`. The docs state: "If you assign a Texture or Texture2D, the Image element will resize and show the assigned texture." That is the source of the layout-jump caveat.

**Evidence and source snapshot**

- Confirmed by reading source at the snapshot above: every code fact in this section and in section 2.
- Confirmed by `git`: `Packages/StreamVideo/Samples~/VideoChatUIToolkit/` and `Packages/StreamVideo/Runtime/Libs/KeyboardAvoidance/` do not exist at tag `0.11.0`, so the UI Toolkit sample and the runtime UI Toolkit usage are unreleased.
- Confirmed: the `Samples~` and `Assets/Samples/...` UI Toolkit copies are byte-identical (hash comparison, excluding `.meta`).
- Inferred, not tested:
  - In-place `RenderTexture` resize does not re-crop a UI Toolkit `Image` without `MarkDirtyRepaint()`.
  - Projects without `com.unity.ugui` fail to compile the SDK.
  - Default UI Toolkit dynamic-atlas settings never atlas the received texture.

## 6. Proposed implementation

**Design (SDK, T01).** Indicative code; keep the existing file's style.

```csharp
// StreamVideoTrack.cs — keep `using UnityEngine.UI;`
public void SetRenderTarget(RawImage targetImage)
{
    if (targetImage == null)
    {
        throw new ArgumentNullException(nameof(targetImage));
    }

    _targetImage = targetImage;
    _targetUIToolkitImage = null;
}

public void SetRenderTarget(UnityEngine.UIElements.Image targetImage)
{
    if (targetImage == null)
    {
        throw new ArgumentNullException(nameof(targetImage));
    }

    _targetUIToolkitImage = targetImage;
    _targetImage = null;
}

internal override void Update()
{
    base.Update();

    if (_targetImage != null)               // existing behaviour, unchanged
    {
        _targetImage.texture = Track.Texture;
        return;
    }

    if (_targetUIToolkitImage != null)
    {
        var texture = Track.Texture;
        if (!ReferenceEquals(_targetUIToolkitImage.image, texture))
        {
            _targetUIToolkitImage.image = texture;
        }
    }
}

protected override void OnDisposing()       // D05 (provisional)
{
    if (_targetUIToolkitImage != null && ReferenceEquals(_targetUIToolkitImage.image, Track.Texture))
    {
        _targetUIToolkitImage.image = null;
    }
    _targetUIToolkitImage = null;
    base.OnDisposing();
}

private UnityEngine.UIElements.Image _targetUIToolkitImage;
```

- Use `ReferenceEquals`, not Unity's overloaded `==`. A destroyed texture compares equal to `null` under Unity's operator, which could skip a needed assignment or clear.
- XML docs on **both** overloads must state:
  - Only one target per track; the last call wins.
  - Call from the main thread.
  - The SDK updates the target when the incoming resolution changes, and the texture instance changes.
  - After a reconnect the participant gets a new track (`TrackAdded`), so call `SetRenderTarget` again.
- UI Toolkit overload docs must also recommend `scaleMode = ScaleMode.ScaleAndCrop` and sizing the element with USS (absolute fill or `flex-grow`), and note that the `image` stays `null` until the first frame.
- Keep the existing `//StreamTodo: can we remove Unity dependency?` comment as it is, or move it next to the overloads. Don't expand it.

**Design (sample, T04).**
- Remote tile: `videoTrack.SetRenderTarget(view.VideoImage)` in `HandleTrack`.
  - Show the video (hide the placeholder) when `Participant.IsVideoEnabled && view.VideoImage.image != null`. Unity-null semantics are fine here, because a destroyed texture should show the placeholder.
  - Delete `_videoTargetHost`, `_videoTarget`, `GetOrCreateVideoTarget`, its `Object.Destroy`, and the `UnityEngine.UI` using.
- Local tile and lobby: assign `GetLocalPreviewTexture()` (keep the existing ">16 px means has frame" gating). Track the last width and height, and call `MarkDirtyRepaint()` on the `Image` when they change.
- `VideoSurface`: becomes a small wrapper that creates `new Image { scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore }`, fills the `#video` container, exposes the `Image`, and implements the preview size check. Alternatively, fold it into `ParticipantTileView` and delete the file plus its `.meta` in both copies. Remove the rotation parameter from `SetSource` and `SetVideo` and their callers.
- Keep `UpdateRequestedResolution()` and the reconnect handling (`TrackAdded` → `HandleTrack` re-calls `SetRenderTarget` with the same `Image`).

**Compatibility / migration**

- Binary compatible: adding an overload doesn't change the existing method's signature or token.
- Source: only a literal `SetRenderTarget(null)` or `SetRenderTarget(default)` becomes ambiguous (CS0121). Both throw `ArgumentNullException` today. Reflection `GetMethod("SetRenderTarget")` without parameter types would throw `AmbiguousMatchException`, which is negligible.
- uGUI customers: no behaviour change. No migration needed.
- The UI Toolkit sample and runtime UI Toolkit usage are unreleased, so their simplification affects no released customer copy.

**Failure modes and caveats**

- **Layout jumps.** A UI Toolkit `Image` sizes itself to its texture unless styles constrain it. Without USS sizing, the layout jumps when the resolution changes. Document it, and in the sample make the `Image` fill its container.
- **Local preview in-place resize.** This needs `MarkDirtyRepaint()` (D10). If the WebRTC fork ever keeps the remote texture stable and resizes it in place (D07 follow-up), the SDK overload must add the same size check and repaint.
- **Reference retention.** The track holds the `VisualElement` until it is disposed or retargeted. Writing to a detached element is harmless.
- **Dynamic atlas (low risk, inferred).** If a customer customizes `PanelSettings` dynamic-atlas filters so the received `Texture2D` gets atlased, frames would freeze. The default filters (including the 64 px max sub-texture size) exclude video-sized textures, and the sample already uses this exact assignment.
- **Shared targets.** Camera and screen-share tracks targeting the same element compete (last `Update` wins). This is existing sample behaviour; D05's reference guard avoids clearing another track's texture.
- **Disposal thread.** D05 touches a `VisualElement` in `OnDisposing`. Verify that disposal runs on the main thread: `StreamVideoCallParticipant` replaces tracks and disposes participants from SDK event handling. If it doesn't, skip the clearing and record it in the decision log.

## 7. SDK Consumer Compatibility Contract

This section is mandatory for changes to Stream's Unity Video SDK, Unity Chat SDK, or its custom WebRTC fork. **The SDK is a plugin installed into customers' existing Unity projects.** The implementation must minimize conflicts with customer assemblies, third-party plugins, project settings, and build tooling. Working in the SDK sample/editor alone is insufficient.

Evaluate each constraint below and attach any relevant work to tasks in section 8. If a constraint genuinely does not apply, say why — **do not silently remove it**.

- **Namespace and symbol hygiene:** Keep new C# types and APIs within established, SDK-specific namespaces (e.g., `StreamVideo.*` / `StreamChat.*`, confirmed against existing code). Avoid unintended global-namespace types, generic public type names, leaking internal helpers into the public API, ambiguous extension methods, overly broad editor menus, scripting define names, and new symbol/assembly collisions. Don't gratuitously rename existing public APIs. → *Applies:* the change is one overload on the existing `StreamVideo.Core.StatefulModels.Tracks.StreamVideoTrack`. No new types or extension methods (D09). Fully qualified `UnityEngine.UIElements.Image` (D06). T01.
- **Assembly isolation:** Check `.asmdef` names, references, define constraints and platform filters. Runtime must not reference Editor-only code. Preserve integrations with consumers using both default `Assembly-CSharp` and their own `.asmdef` assemblies. Any new dependencies must coexist with existing customer packages. → *Applies:* `UnityEngine.UIElementsModule` is a runtime engine module, auto-referenced. No asmdef change expected. A consumer asmdef that references `StreamVideo.Core` and uses the overload needs no extra reference beyond the engine module (verify in T08). Sample asmdef cleanup in T04.
- **Managed dependency coexistence:** Prevent duplicate precompiled DLLs/types (notably `Newtonsoft.Json` when relevant), assembly-name clashes, and hidden conflicts between Chat and Video when both are installed. Avoid vendoring an extra dependency without a clear compatibility plan. → *Not materially affected:* no new managed DLLs. Chat co-install is unaffected because no shared assemblies change.
- **Native dependency coexistence and Unity-version parity:** Video embeds a modified Unity WebRTC package; assess conflicts with Unity's official `com.unity.webrtc`, native binary/framework names, ABI compatibility, plugin importer filters, duplicate types, and native symbol loading. For native-fork work, check supported architectures, transitive dependencies, patch retention, packaging and licenses. **The native fork must support the Video SDK's full documented Unity Editor version range on its supported platforms** (including the oldest Editor), not just compile in the fork's development environment. Prove the native + managed binding + packaged Video integration together. Do not claim both WebRTC packages work together unless verified. → *Not applicable to implementation:* no fork, managed-binding or native changes (D07). The design **relies on** the fork swapping texture instances on resize; record any future fork change to this in the decision log and revisit T01.
- **Distribution and installation:** Respect each affected SDK's actual release path (Video UPM package, Chat `.unitypackage`, native fork embedded/distributed according to the inspected repo). Ensure required `.meta`/GUIDs, `.asmdef`, `.dll`/native binaries, `link.xml`, editor scripts, resources and package metadata survive packaging; keep samples/dev tools out of runtime or customer installs unless intentionally shipped. No unnecessary changes to customers' manifests, global settings, scripting defines, or project structure. → *Applies:* UPM package. Sample files live in `Samples~` (imported on demand); keep `.meta` files consistent and delete the `.meta` of deleted files. T03 changes `package.json` dependencies, which causes UPM to add or resolve `com.unity.ugui` / `com.unity.modules.uielements` in customer projects. That is a deliberate, documented change pending Q02.
- **Supported Unity Editor versions:** Treat the **entire documented version range, not only the latest Editor**, as a compatibility contract. Identify the oldest supported Unity release, all documented supported families, and the newest explicitly documented release from source evidence. Keep new C# syntax, Unity API use, runtime/BCL APIs, package dependencies and native libraries compatible with the oldest. Where newer Unity versions require alternate implementations, preserve old-version behavior using documented guarded alternatives; test both paths. Do not silently increase the Unity floor or use a package dependency unavailable on the minimum Editor. → *Applies:* see the matrix below. T06, T07.
- **Supported-platform accuracy:** Obtain the platform matrix from the *current affected README*, platform docs, package metadata and CI. Chat and Video differ: never infer Video WebGL support from Chat support, or promise unsupported targets. Check Editor vs player build, Mono/IL2CPP/AOT, stripping/reflection, CPU architecture, native library importers, permissions, graphics/audio differences and platform `#if` guards where relevant. Report which targets were actually verified. → *Applies:*
  - Video platforms (docs): Android ARM64, iOS, Windows 10 x64, macOS (Intel and Apple Silicon), Linux. WebGL and UWP are unsupported.
  - CI builds Android IL2CPP only. The change is platform-neutral managed code with no reflection, so the IL2CPP stripping risk is low (the UI Toolkit module is kept when used).
  - Verify Editor plus at least one device player (T06–T08).
- **Unity lifecycle, stability, and footprint:** Preserve initialization order, main-thread access, event subscription cleanup, async cancellation/disposal, domain/scene reload behavior, Editor vs player behavior, memory/resource cleanup and reasonable runtime performance. Avoid hidden singleton/object/project-wide side effects. → *Applies:*
  - Main-thread-only `SetRenderTarget` and `Update`.
  - D05 clearing runs on dispose (verify the thread).
  - The per-frame cost is one reference comparison per track.
  - Removes per-participant hidden `GameObject`s from the sample.
  - T01, T04.
- **Backwards compatibility:** Preserve public API and source compatibility where required, serialized fields/Unity GUID references, package installation/upgrade behavior, existing scripts/scenes and old release consumers. Flag unavoidable breaks explicitly with migration guidance and release notes. → *Applies:* additive. The only source ambiguity is `SetRenderTarget(null)` / `SetRenderTarget(default)`, which throws today; mention it in the release note (T09).
- **Customer-project smoke test:** Verify the **shipped artifact** in a minimal, fresh consumer Unity project (not only the SDK's own repository). Where relevant test coexistence of Chat + Video, an existing Newtonsoft install, an existing official WebRTC dependency (expect a documented conflict unless resolved), consumer `.asmdef` references, and supported player builds. Distinguish tested outcomes from proposed checks. → *Applies:* T08 (UI-Toolkit-only consumer project, consumer asmdef, uGUI regression, project without ugui if T03 lands).

### Mandatory Unity Editor version-range compatibility matrix

**Why this is a release gate:** Customers commonly freeze their Editor version when they ship their game. New features, dependency updates and native plugin rebuilds must not break users on the **oldest supported Unity version**. A successful build in the latest Editor does not prove compatibility with the supported range. **Never silently raise the minimum supported version.**

**Source resolution:**
- `README.md` links to <https://getstream.io/video/docs/unity/platforms/overview/>. Fetched 2026-11-03, it lists Unity 6, 2023.1, 2022.3 and 2021.3.
- `Packages/StreamVideo/package.json` (commit `d12c263`) sets `"unity": "2021.3"`.
- CI (`.github/workflows/main.ci.cd.workflow.yml`) states "Floor is 2021.3 (package.json). Vendored Google.Protobuf needs C# 9." and runs the patch versions listed below.
- **Contradictions (non-blocking, Q06):**
  - Docs list 2023.1, but CI tests 2023.2.20f1.
  - Docs say "Unity 6", while CI also tests 6000.1 and 6000.2. Whether 6.1/6.2 are promised is unclear.

| SDK / component | Oldest supported Editor & source | Other explicitly supported Editor families | Latest explicitly supported Editor family & source | Native/dependency implications | Evidence status |
| --- | --- | --- | --- | --- | --- |
| Video SDK (`io.getstream.video` 0.11.0 + this branch) | **2021.3** (`package.json`, docs, CI floor 2021.3.36f1) | 2022.3, 2023.1 (docs); CI additionally covers 2023.2 | **Unity 6** (docs). CI covers 6000.0.63f1, 6000.1.17f1, 6000.2.12f1. Dev project uses 6000.0.72f1. | UPM. `com.unity.ugui` is 1.0.0 on 2021.3–2023.1 and 2.0.0 (core package) on 2023.2+/Unity 6. `com.unity.modules.uielements` is built in on all. | Confirmed (docs vs CI family mismatch noted) |
| Native Unity WebRTC fork | Must match Video (2021.3) | Must cover Video families | Must match Video (Unity 6) | **Not changed by this spec.** Behaviour relied upon: the receive texture instance is recreated on resize. | Not applicable (no fork change) |

**Oldest-version API and dependency audit**

| Item | 2021.3 availability | Risk / fallback | Task |
| --- | --- | --- | --- |
| `UnityEngine.UIElements.Image.image`, `.scaleMode` (`ScaleMode.ScaleAndCrop`) | Confirmed (Unity 2021.3 Scripting API, `UIElements.Image`) | None expected; compile on 2021.3 | T01, T06 |
| `VisualElement.MarkDirtyRepaint()`, `StretchToParentSize()` (sample) | `MarkDirtyRepaint` confirmed on the 2021.3 `Image` page. `StretchToParentSize` is an established `VisualElementExtensions` API. Compile-verify on 2021.3, or use absolute 0-inset styles instead. | Low | T04, T06 |
| C# syntax | Use C# ≤ 9 constructs only (2021.3 supports C# 9). The sketch uses only C# 7-level syntax. | None | T01 |
| `UnityEngine.UIElementsModule` reference from `StreamVideo.Core` | Engine modules are auto-referenced when `noEngineReferences` is false. Whether a 2021.3 project can disable the built-in UI Toolkit module, and what happens if it does, is **Unknown** (Q04). | If disabling is possible, the SDK (already, via `StreamVideo.Libs` on this branch) fails to compile in such projects. T03 declaring the dependency mitigates. | T03, T08 |
| `package.json` dependencies (T03) | `com.unity.ugui` 1.0.0 exists on 2021.3. On Unity 6, UPM resolution of a declared `1.0.0` against the bundled 2.0.0 core package is **Unverified**. | Verify that resolution produces no error or downgrade on 6000.0. If it fails, pick a version spec that resolves on both, or skip the ugui declaration and record why. | T03, T07, T08 |
| IL2CPP / stripping | No reflection. Direct API use keeps the module. | Low; covered by the CI Android IL2CPP builds | T06, T07 |
| Native / ABI | No change | Not applicable | — |

**Required verification approach:**
- Oldest: 2021.3.36f1 and 2021.3.45f2 (CI rows), plus a manual 2021.3 Editor run of the UI Toolkit sample and consumer project. Use 2021.3.36f1, or record the exact patch used.
- Middle: 2022.3.62f2 (CI).
- Newest documented: Unity 6, 6000.0.63f1 (CI) plus a manual run on 6000.0.72f1 (dev project).
- CI rows 2023.2.20f1, 6000.1.17f1 and 6000.2.12f1 must stay green but are not documented families.
- Runtime render tests are camera-gated and will likely be skipped on CI (no camera), so remote-video rendering must be verified manually on at least the oldest and newest Editors.

**Version-range release gate:**
- All CI rows are green.
- The manual oldest (2021.3) and newest (Unity 6) Editor checks for T06/T08 pass.
- The 2021.3 consumer-project check needs a locally installed 2021.3 Editor; if none is available, the gate is **Unverified**, not passed.
- No floor increase. Any support drop requires explicit product approval and release notes; none is planned.

**Applicability and decisions for this change:**
- Managed-only, additive API. Version risk is concentrated in UI Toolkit API availability on 2021.3 (low; confirmed) and in T03's dependency resolution across 2021.3 and Unity 6 (unverified).
- Native parity is not affected.
- Owners: T01 (API), T03 (dependencies), T06/T07 (version validation), T08 (consumer proof).

## 8. Ordered implementation tasks

- [ ] **T01 — Add `SetRenderTarget(UnityEngine.UIElements.Image)` to `StreamVideoTrack`**
  - **Why / background:** This is the core deliverable (D01). The fork recreates the receive texture on every resolution change, so per-frame rebinding must live in the SDK. `StreamVideoTrack.Update()` already does it for `RawImage`.
  - **Work:**
    - Implement the overload, mutual clearing in both overloads (D02), the `Update()` branch with a `ReferenceEquals` check, and D05 dispose clearing (provisional; see Q03), as in section 6.
    - Add XML docs to both overloads covering the points listed in section 6.
    - Before relying on D05, confirm that `OnDisposing` is reached on the main thread (trace `StreamVideoCallParticipant` track replacement and participant disposal). If it isn't, drop D05 and log the decision.
  - **Touchpoints:** `Packages/StreamVideo/Runtime/Core/StatefulModels/Tracks/StreamVideoTrack.cs`.
  - **Dependencies:** D01, D02, D05, D06.
  - **Caveats:**
    - `RawImage` behaviour must be byte-for-byte equivalent in effect: assigned every frame while alive, no dispose clearing.
    - Use the fully qualified `UnityEngine.UIElements.Image`.
    - Don't add a public `Texture` getter (D03). Don't touch the WebRTC fork (D07).
    - C# ≤ 9.
  - **Done when:**
    - Both overloads compile on 2021.3 and Unity 6.
    - Passing `null` to either throws `ArgumentNullException`.
    - Calling one overload stops updates to the other target.
    - The UI Toolkit `Image` receives each new texture instance.
    - The uGUI sample and `DocsCodeSamples` compile unchanged.
  - **Verify:** T02 tests, T06/T07 CI, and a manual check with the uGUI sample (`Samples~/VideoChat`), which should show no regression.

- [ ] **T02 — Tests for the UI Toolkit render target**
  - **Why / background:** This is new public behaviour. The project's convention requires tests (see `.cursor/skills/write-test/SKILL.md`: `#if STREAM_TESTS_ENABLED`, `When_<condition>_expect_<outcome>`, runtime tests inherit `TestsBase` and use `ConnectAndExecute`).
  - **Work:**
    - Add camera-gated runtime tests (`[UnityTest, ConditionalIgnore(IgnoreConditionNoCameraKey, IgnoreConditionNoCameraReason)]`) modelled on `CallsTests.When_client_joins_call_with_video_expect_receiving_video_track`, either in `Packages/StreamVideo/Tests/Runtime/` (new `StreamVideoTrackTests.cs`) or in `CallsTests.cs`. Cover these cases:
      - (a) After `SetRenderTarget(uiToolkitImage)`, wait with `WaitForConditionAsync` until `image.image != null` and `width > 0`.
      - (b) Retargeting to a `RawImage` makes `rawImage.texture` non-null. It also stops UI Toolkit updates; assert this by requesting a different resolution if practical, otherwise assert the target fields indirectly through behaviour.
      - (c) Passing `null` to either overload throws `ArgumentNullException`.
      - (d) If D05 is kept, `track.Dispose()` clears `image.image`.
    - Also check whether a sending `new VideoStreamTrack(texture)` can be constructed in Editor tests (WebRTC context availability). If yes, add Editor tests in `Tests/Editor/` for (a)–(d) without a backend; its `Texture` is a stable `RenderTexture`. If no, record that in progress notes.
  - **Touchpoints:** `Packages/StreamVideo/Tests/Runtime/`, possibly `Packages/StreamVideo/Tests/Editor/`. Use existing asmdefs only; never create new ones.
  - **Dependencies:** T01.
  - **Caveats:** CI likely has no camera, so runtime tests will be skipped there. Don't use `Task.Delay` for polling.
  - **Done when:** The tests exist and pass locally with a camera (or the Editor variant passes in CI).
  - **Verify:** Unity Test Runner locally (record the Editor version and camera availability). CI `testMode: all`.

- [ ] **T03 — (Provisional, gated by Q02) Declare uGUI and UI Toolkit dependencies in `package.json`**
  - **Why / background:**
    - `StreamVideo.Core` uses `UnityEngine.UI.RawImage`.
    - On this branch, `StreamVideo.Libs` (keyboard avoidance) and, after T01, `StreamVideo.Core` use `UnityEngine.UIElements`.
    - Neither `com.unity.ugui` nor `com.unity.modules.uielements` is declared, so a project without them probably fails to compile the SDK (inferred).
  - **Work:** If Q02 is approved, add `"com.unity.ugui"` and `"com.unity.modules.uielements": "1.0.0"` to `dependencies`. Choose a ugui version spec that resolves on 2021.3 (1.0.0) **and** on Unity 2023.2+/6 (bundled 2.0.0 core package). Verify the resolution in both Editors and check `packages-lock.json` effects.
  - **Touchpoints:** `Packages/StreamVideo/package.json`.
  - **Dependencies:** Q02 decision. Independent of T01, but should land in the same release.
  - **Caveats:** Must not raise the Unity floor or force a ugui downgrade on Unity 6. This changes what UPM installs in customer projects, so mention it in the release note (T09).
  - **Done when:**
    - The package resolves without errors or warnings in a fresh 2021.3 project and a fresh Unity 6 project.
    - Both have ugui and UI Toolkit present after install, even if they were removed beforehand.
  - **Verify:** T08 consumer-project checks (with ugui removed from the manifest before installing the SDK).

- [ ] **T04 — Simplify the UI Toolkit sample to use the new overload**
  - **Why / background:**
    - The hidden `RawImage` holders exist only because the SDK lacked this overload.
    - `VideoSurface`'s manual cover-fit math is replaceable by `ScaleAndCrop` because all callers pass rotation 0 (D10).
    - The local preview is resized in place, so it still needs a size-change check with `MarkDirtyRepaint()`.
  - **Work:** As in section 6 (sample design). In both copies:
    - `CallParticipantTile.cs`: remove `GetOrCreateVideoTarget`, `_videoTargetHost`, `_videoTarget`, their dispose code and the `UnityEngine.UI` using. Call `videoTrack.SetRenderTarget(<tile Image>)`. Compute placeholder visibility from `IsVideoEnabled && image.image != null`.
    - `VideoSurface.cs` / `ParticipantTileView.cs`: use an `Image` with `ScaleAndCrop` that fills `#video`. Expose the `Image`. Keep the size-change → `MarkDirtyRepaint()` logic for externally assigned textures (preview). Remove the rotation parameter.
    - `LobbyScreen.cs`: update the `SetVideo` calls.
    - Asmdef: remove `UnityEngine.UI` only if no sample file still uses it.
  - **Touchpoints:** `Packages/StreamVideo/Samples~/VideoChatUIToolkit/Scripts/**` and the mirrored `Assets/Samples/Stream Video & Audio Chat SDK/0.11.0/Video Chat UI Toolkit Example/Scripts/**`, with their `.meta` files.
  - **Dependencies:** T01.
  - **Caveats:**
    - Keep `UpdateRequestedResolution()`, reconnect handling via `TrackAdded`, and the ">16 px means has frame" preview gating.
    - Don't change the camera/screen-share shared-target behaviour.
    - Keep both copies byte-identical.
  - **Done when:**
    - The sample has no `RawImage` or `GameObject` video holders.
    - Remote video renders and re-crops after resolution changes.
    - The local preview crop is correct after device rotation and after joining a call.
    - The two copies are identical.
  - **Verify:**
    - Manual run: Editor plus one mobile device, two participants.
    - Change the tile size or layout to trigger requested-resolution changes.
    - Rotate the device (lobby and in-call).
    - Toggle the camera, leave and rejoin, and force a reconnect (network toggle).
    - Hash-compare the two sample copies.

- [ ] **T05 — Docs: XML docs, docs code sample, external docs page**
  - **Why / background:** Customers find the API through the docs. The caveats (layout jumps, `ScaleAndCrop`, re-call after reconnect, main thread, local preview `MarkDirtyRepaint`) are not discoverable from the signature.
  - **Work:**
    - Confirm the XML docs from T01.
    - Add a UI Toolkit snippet to `Packages/StreamVideo/DocsCodeSamples/01-basics/` (next to `QuickStartCodeSamples.cs` / `ParticipantView.cs`) showing `TrackAdded` → `SetRenderTarget(image)` with `ScaleAndCrop`. The asmdef is Editor-only and must keep compiling.
    - Locate the docs-site source for <https://getstream.io/video/docs/unity/> (not in this repo; Q05) and add a UI Toolkit section next to the existing `RawImage` instructions.
    - Optionally extend the `GetLocalPreviewTexture()` XML doc with the UI Toolkit in-place-resize note.
  - **Touchpoints:** `Packages/StreamVideo/DocsCodeSamples/**`, `Packages/StreamVideo/Runtime/Core/DeviceManagers/IStreamVideoDeviceManager.cs` (doc comment only), and the external docs repo (locate first).
  - **Dependencies:** T01.
  - **Caveats:**
    - Docs must not promise WebGL or UWP.
    - The snippet must use only 2021.3-compatible APIs.
    - The unpublished tutorial draft that described the sample's "rendering bridge" should be updated by its owner to show the one-line API (Q05). Do not publish private draft text in this public repo.
  - **Done when:** The snippet compiles; the docs-site change is submitted (or Q05 records who owns it).
  - **Verify:** The project compiles with `DocsCodeSamples`. The docs PR link is recorded in progress notes.

- [ ] **T06 — Oldest supported Editor validation (Unity 2021.3)**
  - **Why / background:** 2021.3 is the package floor. Customers on it can't upgrade their Editor.
  - **Work:**
    - Confirm the CI rows 2021.3.36f1 and 2021.3.45f2 are green (compile, tests, Android IL2CPP build).
    - Manually open the project or a consumer project in a 2021.3 Editor (record the exact patch). Import the UI Toolkit sample and run a two-participant call: remote video renders, survives resolution changes, preview re-crops.
  - **Touchpoints:** CI, local 2021.3 Editor.
  - **Dependencies:** T01–T04 (and T03 if approved).
  - **Caveats:** If no 2021.3 Editor is available locally, mark this **Unavailable/Unverified**, not passed.
  - **Done when:** CI is green and the manual check passes with the patch recorded.
  - **Verify:** CI run link and manual notes in progress notes.

- [ ] **T07 — Middle and newest documented Editor validation (2022.3, Unity 6)**
  - **Why / background:** UI Toolkit runtime behaviour and the ugui packaging (2.0.0 core package on 2023.2+) differ across versions.
  - **Work:**
    - Confirm CI 2022.3.62f2, 2023.2.20f1, 6000.0.63f1, 6000.1.17f1 and 6000.2.12f1 are green.
    - Run the T04 manual scenario in 6000.0.72f1 (dev project). Include one device player build (Android ARM64 or iOS) running the UI Toolkit sample.
  - **Touchpoints:** CI, dev project, device.
  - **Dependencies:** T01–T04.
  - **Caveats:** Record exact patches. One patch doesn't prove a whole family.
  - **Done when:** CI is green and the manual and device checks pass.
  - **Verify:** CI link, device, and Editor notes.

- [ ] **T08 — Clean consumer project smoke test (shipped package form)**
  - **Why / background:** Success in the SDK repo doesn't prove customer integration. In particular, check consumer asmdefs and projects without ugui.
  - **Work:** In fresh projects on 2021.3 and Unity 6:
    - Install the package via git URL or local path at the branch commit.
    - Create a UI-Toolkit-only scene. Consumer code in its own `.asmdef` references `StreamVideo.Core` only, calls `SetRenderTarget(UnityEngine.UIElements.Image)`, and renders remote video.
    - Separately, run a uGUI `RawImage` consumer as a regression check.
    - If T03 landed, repeat on a project with `com.unity.ugui` removed from `manifest.json` before installing.
    - If T03 didn't land, record the observed behaviour of that scenario (expected: compile error) as a known limitation.
  - **Touchpoints:** Fresh consumer projects (outside this repo).
  - **Dependencies:** T01, T03 (if approved).
  - **Caveats:** Distinguish tested from expected outcomes.
  - **Done when:** All scenarios behave as expected and are recorded.
  - **Verify:** Notes with Editor patch, package commit, and outcomes.

- [ ] **T09 — Release note**
  - **Why / background:** `CHANGELOG.md` is generated at release from the `ReleaseTool` `changelogBase64` input, not edited on feature branches.
  - **Work:** Put release-note text in the PR description, for example:
    - "Added `StreamVideoTrack.SetRenderTarget(UnityEngine.UIElements.Image)` to render participant video directly in UI Toolkit."
    - The UI Toolkit sample update.
    - If T03 landed, the new dependencies.
    - The note that `SetRenderTarget(null)` is now ambiguous at compile time (it previously threw).
  - **Touchpoints:** PR description.
  - **Dependencies:** T01, T03, T04.
  - **Caveats:** Don't edit `CHANGELOG.md` directly unless the team asks.
  - **Done when:** The release-note text is in the PR.
  - **Verify:** PR review.

## 9. Verification plan and acceptance matrix

**Automated tests:**
- T02 runtime tests (camera-gated) for: texture delivery to the UI Toolkit `Image`, retargeting in both directions, `ArgumentNullException`, and D05 dispose clearing. Add Editor tests if a sending `VideoStreamTrack` can be constructed in Editor tests.
- Existing Editor and runtime suites must stay green in the CI matrix.

**Manual integration / package-install checks:**
- T04 scenario: remote video, resolution changes, preview rotation, camera toggle, reconnect, leave/rejoin.
- T08 clean consumer projects on 2021.3 and Unity 6, covering a consumer asmdef, UI Toolkit only, a uGUI regression, and (if T03 landed) ugui removed.

**Unity Editor version coverage:**

| Editor test point | Exact Unity Editor patch version | SDK artifact / native fork revision | Scope and acceptance | Evidence / status |
| --- | --- | --- | --- | --- |
| Oldest supported Editor (**mandatory release baseline**) | 2021.3.36f1 (CI) and 2021.3.45f2 (CI); manual check on a 2021.3 patch to be recorded | Branch commit at PR time; bundled fork unchanged | Compile, Editor and runtime tests, Android IL2CPP build (CI); manual sample and consumer render | Planned |
| Representative middle Editor/LTS | 2022.3.62f2 (CI) | Same | Compile, tests, Android IL2CPP build | Planned |
| Newest **explicitly documented** supported Editor | Unity 6: 6000.0.63f1 (CI), 6000.0.72f1 (manual, dev project) | Same | Compile, tests, build; manual sample, device run, consumer project | Planned |
| Non-documented CI rows | 2023.2.20f1, 6000.1.17f1, 6000.2.12f1 | Same | Stay green | Planned |
| WebRTC ↔ Video parity check | — | Fork unchanged | Not applicable: no fork change | Not applicable |

| Target / consumer scenario | Relevant SDK and version | Expected result | How to verify | Evidence/status |
| --- | --- | --- | --- | --- |
| UI Toolkit consumer, own asmdef, 2021.3 | Branch package | Compiles; remote video renders and survives resolution change | T08 | Not run |
| UI Toolkit consumer, own asmdef, Unity 6 | Branch package | Same | T08 | Not run |
| uGUI `RawImage` consumer (regression) | Branch package | Unchanged behaviour | T08, uGUI sample | Not run |
| Project without `com.unity.ugui` | Branch package ± T03 | With T03: resolves and compiles. Without T03: documented limitation. | T08 | Not run |
| Android ARM64 IL2CPP player | Branch package | UI Toolkit sample renders remote video | T07 device | Not run |
| iOS player | Branch package | Same (if a device is available) | T07 device | Not run |
| Windows/macOS Editor | Branch package | Sample renders; preview re-crops | T04/T06/T07 | Not run |

**Out-of-matrix exclusions:**
- WebGL and UWP: unsupported by Video.
- iOS in CI: not built (PBE-5316 per workflow comment).
- Linux player: not specifically targeted (low risk; managed-only change).
- Camera-gated runtime tests are likely skipped in CI.

**Release gate:**
- All CI rows are green.
- T02 tests pass locally with a camera, or Editor-variant tests pass in CI.
- The manual 2021.3 and Unity 6 checks (T06, T07) and the consumer smoke tests (T08) pass with patches recorded.
- No Unity floor change.
- T03 is either landed and verified on both 2021.3 and Unity 6, or explicitly deferred via Q02.
- Checks needing a local 2021.3 Editor or a device are marked Unverified until actually run.

## 10. Risks, edge cases, and mitigations

| Risk / edge case | Trigger and impact | Mitigation / owner task |
| --- | --- | --- |
| Stale crop on in-place resize | Local preview `RenderTexture` resized in place (16×16 → camera, rotation, publish start). `ScaleAndCrop` geometry isn't regenerated, so the preview looks stretched. | Size check plus `MarkDirtyRepaint()` in the sample (T04). Document it (T05). |
| Future fork makes remote texture stable | If D07 is ever implemented, remote textures resize in place and the overload's reference check never fires. | Add a size check plus `MarkDirtyRepaint()` to `StreamVideoTrack` at that time (decision-log note). |
| Layout jumps | `Image` sizes to its texture without USS constraints. | Docs (T05). The sample fills its container (T04). |
| Missing ugui / UI Toolkit in customer project | Customer removed the packages or modules, so the SDK fails to compile. | T03 (Q02), T08. Q04 (module disable). |
| UPM resolution conflict for ugui on Unity 6 | A declared 1.0.0 vs the bundled 2.0.0 core package. | T03 verification on 6000.0. |
| `SetRenderTarget(null)` ambiguity | Customer code passes a literal `null` or `default`, causing a CS0121 compile error. | Release note (T09). The call threw before. |
| Element retained / detached | Track holds the `VisualElement` until dispose or retarget. | Documented. D05 clears on dispose. |
| D05 touches UI off the main thread | If track disposal ever occurs off the main thread. | Verify in T01; drop D05 if not guaranteed. |
| Shared target (camera + screen share) | Both tracks write to the same `Image`. | Existing behaviour, out of scope. The D05 reference guard prevents clearing the other track's texture. |
| Dynamic atlas freezing (low) | Customer-modified `PanelSettings` atlas filters. | Docs note (T05). The sample already uses this path. |

## 11. Open questions, assumptions, and blockers

| ID | State | Question / assumption | Consequence / how to resolve |
| --- | --- | --- | --- |
| Q01 | Assumed | The user accepts the overload direction (D01) and the review corrections. Assumed from the request to write this spec directly after the review. | If rejected, stop and revisit section 4 (the alternatives are recorded). |
| Q02 | Open. T03 not implemented. | Should `package.json` declare `com.unity.ugui` and `com.unity.modules.uielements`? This changes what UPM installs in customer projects. | Not approved. Known limitation: a project that removes `com.unity.ugui` or the UI Toolkit module can fail to compile `StreamVideo.Core` (`RawImage`, and now `UnityEngine.UIElements.Image`) and `StreamVideo.Libs` (keyboard avoidance). Release note states this. |
| Q03 | Non-blocking | Keep D05 (clear the UI Toolkit `image` on track dispose)? | Default: implement with the reference guard. Drop it if the owner prefers parity with the `RawImage` path or if disposal isn't main-thread. |
| Q04 | Unknown | Can the built-in UI Toolkit module (`com.unity.modules.uielements`) be disabled in 2021.3 or Unity 6 projects, and does that remove `UnityEngine.UIElementsModule` from compilation? | Test in T08. If yes, T03 becomes more important. Note that `StreamVideo.Libs` already depends on it on this branch. |
| Q05 | Located, not submitted. | Where is the docs-site source, and who owns the unpublished tutorial draft that describes the sample's "rendering bridge"? | Docs site source is `GetStream/docs` (`data/docs/video/unity/01-basics/03-quickstart.md` has the `RawImage` instructions). The local clone is behind `origin/main` and was not edited. The docs repo owners submit the UI Toolkit section. The unpublished tutorial draft was not in either repo and was not modified. |
| Q06 | Non-blocking | Docs list 2023.1 and "Unity 6", while CI tests 2023.2 and 6000.1/6000.2. Which families are promised? | Docs owner clarifies. Doesn't change this implementation. |
| Q07 | Non-blocking (follow-up) | Later work, not accepted into this scope: auto-rebinding after reconnect; automatic requested resolution from element size; a UI Toolkit helper for the local preview; a stable remote texture (D07) plus a public `Texture` getter (D03). | Track separately. |

## 12. References and handoff notes

**Material from the original discussion:** All decisions, evidence, rejected alternatives and corrections are incorporated in sections 2, 4, 6 and 10. The unpublished tutorial draft is intentionally not quoted beyond the phrase "rendering bridge".

**Source references (commit `d12c263`, branch `feature/uni-211-create-new-sample-project`):**
- `Packages/StreamVideo/Runtime/Core/StatefulModels/Tracks/StreamVideoTrack.cs`, `BaseStreamTrack.cs`
- `Packages/StreamVideo/Runtime/Core/StatefulModels/StreamVideoCallParticipant.cs` (`Update`, track replacement)
- `Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime/Scripts/VideoStreamTrack.cs` (`UnityVideoRenderer.OnVideoFrameResizeInternal`)
- `GetStream/com.unity.webrtc` @ `be1b676`: `Plugin~/WebRTCPlugin/UnityVideoRenderer.cpp` (`SetFrameBuffer`, `ConvertVideoFrameToTextureAndWriteToBuffer`), `Plugin~/WebRTCPlugin/UnityRenderEvent.cpp` (texture update V2 callback)
- `Packages/StreamVideo/Runtime/Core/BackgroundFilters/LocalPreviewRenderer.cs`, `Packages/StreamVideo/Runtime/Core/DeviceManagers/IStreamVideoDeviceManager.cs`
- `Packages/StreamVideo/Samples~/VideoChatUIToolkit/**` (`CallParticipantTile.cs`, `VideoSurface.cs`, `ParticipantTileView.cs`, `LobbyScreen.cs`, `ParticipantTile.uxml/.uss`, asmdef)
- `Packages/StreamVideo/package.json`, `Packages/StreamVideo/Runtime/Core/StreamVideo.Core.asmdef`, `Packages/StreamVideo/Runtime/Libs/StreamVideo.Libs.asmdef`
- `.github/workflows/main.ci.cd.workflow.yml`, `.github/workflows/release_step1_create-release-pr.yml`
- Unity 2021.3 Scripting API: <https://docs.unity3d.com/2021.3/Documentation/ScriptReference/UIElements.Image.html>
- Video platform docs: <https://getstream.io/video/docs/unity/platforms/overview/>
- Test conventions: `.cursor/skills/write-test/SKILL.md`

**Progress notes:**
- 2026-11-03: Spec created. No implementation started.
- 2026-11-04: Implemented on `feature/uni-211-create-new-sample-project` (the spec snapshot). `package.json` was not changed (Q02). `CHANGELOG.md` was not edited.

**D05 main-thread evidence:** `PeerConnectionBase.OnTrack` runs from `RTCPeerConnection.PCOnTrack`, which uses `WebRTC.Sync` (`SynchronizationContext.Post` onto the context captured on the main thread). That path calls `StreamVideoCallParticipant.SetTrack`, which disposes the previous `StreamVideoTrack`. Participant removal disposes tracks from `CacheRepository.Remove`, reached from SFU `ParticipantLeft` / coordinator participant-left handling inside `ProcessMessages`. `BasePersistentWebSocket.Update` calls `ProcessMessages`, and `StreamVideoClient.Update` calls that from the main-thread update loop. D05 is kept.

**T03 known limitation:** `com.unity.ugui` and `com.unity.modules.uielements` stay undeclared. Projects that remove them can fail to compile the SDK.

**T09 release note** (for the PR description; not yet in a PR):

- Added `StreamVideoTrack.SetRenderTarget(UnityEngine.UIElements.Image)` to render participant video directly in UI Toolkit. One target per track; the last call wins. Call it again after reconnect (`TrackAdded`). Size the element with USS and set `scaleMode` to `ScaleMode.ScaleAndCrop`. The image stays null until the first frame.
- The UI Toolkit sample no longer creates hidden `RawImage` holders. The local preview calls `MarkDirtyRepaint()` when its texture is resized in place.
- `SetRenderTarget(null)` and `SetRenderTarget(default)` are now ambiguous at compile time. They already threw `ArgumentNullException`.
- `com.unity.ugui` and `com.unity.modules.uielements` are still not declared. A project that removes them can fail to compile the SDK.

**Editor tests:** `Packages/StreamVideo/Tests/Editor/StreamVideoTrackTests.cs` constructs a sending `VideoStreamTrack`. `WebRTC.Context` is created on assembly reload. Edit Mode does not run `RuntimeInitializeOnLoadMethod`, so `WebRTC`'s sync context is null and disposing a track throws. The tests install an inline context only when that field is null. If construction throws, the tests call `Assert.Ignore`.

**Verified 2026-11-04:** Unity `6000.0.72f1` Edit Mode, `StreamVideo.Tests.Editor.StreamVideoTrackTests`, 6 passed, 0 failed. The project compiled, including the UI Toolkit sample under `Assets/Samples` and `DocsCodeSamples`. The two sample trees were hash-compared and matched. `Image.image` is `Texture` on the 6000.0.72f1 and 2022.3.71f1 UIElements modules (DLL inspection). 2022.3 was not used to open this Unity 6 project.

**Runtime tests, same Editor, Play Mode, batch mode:** `WebRTC.Update` yields `WaitForEndOfFrame` and no decoded texture is created (`decoding=True`, `texture=null`) even though WeCam opened at 1280x720 and the subscriber connected. The tests `Assert.Ignore` in that case. A reflection copy of the WebRTC frame submit was removed after it crashed the editor in `webrtc!UnityPluginUnload`.

**Runtime tests, same Editor, Play Mode, not batch mode (`-runTests` without `-batchmode` or `-quit`):** WeCam at 1280x720. Each test passed when run alone, exit 0: texture delivery (6.4s), retarget both ways (6.2s), null rejection plus D05 clear (6.4s on the second solo run). The first solo dispose run and two full-fixture runs crashed in `webrtc!UnityPluginUnload` during editor shutdown after the track dispose, before results were written. Batch-mode Play Mode still does not decode a frame. The UI Toolkit sample script copies were hash-identical (14 files, no `RawImage` holders).

**Verification still open:** Unity 2021.3 is not installed locally (T06 manual check Unverified). No device player was run (T07 Unverified). No clean consumer project was created (T08 Unverified). CI was not run. The docs-site change is not submitted (Q05). Release-note text is above and is not yet in a PR (T09). Task checkboxes stay open until those gates pass.
