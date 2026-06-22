# Late-Published Video Not Received — Proper Fix (publish-gated subscriptions)

**Status:** Implemented. Replaces the earlier "receiver-side reconciliation poll" safety net (that
approach was stashed/reverted).
**Scope:** Receiver-side only. `RtcSession.cs` + `StreamVideoCallParticipant.cs`. No publisher changes,
no billing impact.
**Branch:** `feature/uni-177-fix-users-not-receiving-a-video-track-until-another-user`

---

## 1. TL;DR

A participant joins with their camera **off** and enables it **later**. The SFU adds a video m-line via
renegotiation and emits `trackPublished` to existing participants. Unity re-sent `UpdateSubscriptions` on
that event, but the request was **identical** to the one already sent (because Unity was already requesting
that peer's video regardless of whether they published it). The SFU saw no change and frequently never sent
the follow-up `subscriberOffer`, so the remote video never arrived — until an unrelated participant joined
and forced a full resync.

**The fix:** subscribe to a peer's video **only while the SFU reports they are actually publishing it**
(`PublishedTracks`), exactly like the JS and Android SDKs. Now a late publish genuinely changes the
subscription set, which forces the SFU to emit the `subscriberOffer`. No polling, no reconciliation pass.

---

## 2. Symptom / customer report

- First 2–3 players in a lobby see only their **own** local camera; never receive each other's remote video.
  When a later player joins, all existing players suddenly start receiving each other's video.
- **Audio works the entire time.**
- Integration enables the local **video publisher only after** `JoinCallAsync` returns (plus a ~3s camera
  warm-up). Audio is enabled at join time — which is why audio always works.
- Trigger: **late camera enable** — video published after the peer joined and after others set up their
  subscriptions to it.
- A 2-client sequential test does **not** reproduce (the 2nd join re-queues the 1st's subscription "for free").
  The 3–4 device delayed-camera scenario reproduces reliably.

Repro spec: `Packages/StreamVideo/.agents/specs/sample-delayed-camera-enable.md`
(sample config: `_autoEnableCamera = true`, `_delayCameraEnable = true`, `_autoEnableCameraDelaySeconds = 3`,
`_autoEnableMicrophone = true`, with `STREAM_DEBUG_ENABLED`).

---

## 3. Root cause

Two layers. Only Layer B is the client bug we fix.

### Layer A — Publisher (NOT changed, by design / billing)
The publisher does not create a video transceiver or announce a video m-line until the user enables their
camera. This is correct and must stay: customers are billed for **publishing** video. We must never
pre-create a disabled video transceiver "to keep the m-line stable" — `GetAnnouncedTracks` would announce it
and risk billing for video the user never enabled.

### Layer B — Receiver subscription selection (the bug)
`GetDesiredTracksDetails()` decided what to subscribe to using **only** the incoming-video opt-in flag:

```csharp
// OLD
private bool ShouldSubscribeToVideoTrack(IStreamVideoCallParticipant participant)
    => _incomingVideoRequestedByParticipantSessionId.GetValueOrDefault(participant.SessionId, false);
```

Consequence chain:
1. Customer calls `SetIncomingVideoEnabled(true)` for peers at join (cameras still off).
2. `UpdateSubscriptions` already includes video for those peers → SFU returns SUCCESS, nothing to offer.
3. Peer enables camera → `OnSfuTrackPublished` → `QueueTracksSubscriptionRequest()` → `UpdateSubscriptions`
   is re-sent **with the same track set** (video for that peer was already being requested).
4. SFU sees no change → often **no `subscriberOffer`** → track never arrives.
5. A later unrelated join issues a fresh full subscription for everyone → SFU finally offers → video appears.

### Why JS/Android don't have this bug
They list a participant's video in the subscription set **only when the peer is actually publishing it**:
- JS `TrackSubscriptionManager.subscriptions`: `if (p.videoDimension && hasVideo(p))` where
  `hasVideo(p) = p.publishedTracks.includes(VIDEO)`.
- Android `Subscriber.defaultTracks`: `if (participant.videoEnabled.value) { ...VIDEO... }`.
- Neither re-subscribes from the `trackPublished` event handler directly; they update participant
  publish-state and let the subscription set recompute. Because the set **changes** when video is published
  late, the SFU is forced to send the offer.

(Also note: JS/Android don't request **audio** at all — the SFU implicitly subscribes audio. Unity requests
audio explicitly; that path is unchanged and works because audio is enabled at join.)

---

## 4. The fix

Mirror JS/Android: gate video subscription on the publish-state signal (`PublishedTracks`), and keep
`PublishedTracks` reliably maintained even when the SFU omits the optional participant DTO on
`trackPublished` / `trackUnpublished`.

### 4.1 `StreamVideoCallParticipant.cs` — publish-state helpers

```csharp
internal bool IsPublishingTrack(TrackType trackType) => _publishedTracks.Contains(trackType);

internal void AddPublishedTrack(TrackType trackType)
{
    if (!_publishedTracks.Contains(trackType))
    {
        _publishedTracks.Add(trackType);
    }
}

internal void RemovePublishedTrack(TrackType trackType) => _publishedTracks.Remove(trackType);
```

Important: use `PublishedTracks` as the trigger, **not** `IsVideoEnabled`. `IsVideoEnabled =>
VideoTrack?.IsEnabled ?? false` is derived from already having the track, so it can't be used to decide
whether to request it (chicken-and-egg).

### 4.2 `RtcSession.cs` — the gate

```csharp
private bool ShouldSubscribeToVideoTrack(IStreamVideoCallParticipant participant)
    => _incomingVideoRequestedByParticipantSessionId.GetValueOrDefault(participant.SessionId, false)
       && IsParticipantPublishingTrack(participant, TrackType.Video);

private bool ShouldSubscribeToAudioTrack(IStreamVideoCallParticipant participant)
    => _incomingAudioRequestedByParticipantSessionId.GetValueOrDefault(participant.SessionId, false);

private static bool IsParticipantPublishingTrack(IStreamVideoCallParticipant participant, TrackType trackType)
    => participant is StreamVideoCallParticipant concreteParticipant
       && concreteParticipant.IsPublishingTrack(trackType);
```

(`PublishedTracks` is not on the `IStreamVideoCallParticipant` interface, hence the cast to the concrete type
— same pattern the reverted reconciliation code used.)

### 4.3 `RtcSession.cs` — keep `PublishedTracks` in sync when DTO is omitted

The `participant` DTO is optional on these events (see `TrackPublished.participant` /
`TrackUnpublished.participant` in `events.proto`). Previously `_publishedTracks` was only updated from that
DTO, so without it the gate would never see the new track.

`OnSfuTrackPublished`:
```csharp
if (participant != null)
{
    if (participantSfuDto != null)
    {
        participant.UpdateFromSfu(participantSfuDto);   // authoritative (replaces PublishedTracks)
    }
    else
    {
        participant.AddPublishedTrack(type);            // keep in sync without the DTO
    }
}

QueueTracksSubscriptionRequest();
```

`OnSfuTrackUnpublished`:
```csharp
if (participant != null)
{
    participant.ClearTrackPausedByServer(type);

    if (participantSfuDto != null)
    {
        participant.UpdateFromSfu(participantSfuDto);
    }
    else
    {
        participant.RemovePublishedTrack(type);
    }
}
```

### 4.4 Fixed flow (late camera enable)

1. Peer enables camera → SFU `trackPublished TRACK_TYPE_VIDEO`.
2. `OnSfuTrackPublished` records Video in `PublishedTracks` (via DTO or `AddPublishedTrack`).
3. `QueueTracksSubscriptionRequest()` → next `UpdateSubscriptions` now **includes** that peer's video (set
   genuinely changed).
4. SFU sends `subscriberOffer` (new m-line) → `OnTrack` → `SetTrack` → `TrackAdded` → tile renders.

Recovery happens within the normal subscription debounce, not "never / only on next join".

---

## 5. Behavior changes to be aware of while debugging

1. **Remote mute now drops that peer's video subscription; unmute re-subscribes.**
   `TrackUnpublishReason` in this SFU is entirely about muting (`UserMuted`, `PermissionRevoked`,
   `Moderation`), i.e. `trackUnpublished` is effectively a mute event. Because subscriptions are now gated on
   `PublishedTracks`, a remote video mute removes Video from `PublishedTracks` → next `UpdateSubscriptions`
   drops it → unmute (`trackPublished`) re-adds and re-subscribes. **This matches JS/Android** and saves
   bandwidth on muted video, but it is a change from Unity's previous behavior (which kept the subscription
   and only disabled display). This is inherent to gating on `PublishedTracks` (the DTO-present path already
   implied it); the `RemovePublishedTrack` line just makes the DTO-absent path consistent.
   - Watch for: extra renegotiation latency on unmute; tile flicker; `OnTrack` firing again on re-subscribe
     (old `StreamVideoTrack` is disposed and a new one created → `TrackAdded` re-fires; sample's
     `ParticipantView.OnParticipantTrackAdded` re-binds the render target, which is fine).

2. **`MaxParticipantsForVideoAutoSubscription` is currently `0`** in the working tree (was `5`). This is the
   only other uncommitted `RtcSession.cs` change and is independent of this fix — it makes incoming video
   fully manual opt-in (`NotifyParticipantJoined` sets `requestVideo = count <= 0 = false`). With this fix,
   video then requires `SetIncomingVideoEnabled(true)` **and** the peer publishing. Revert to `5` if that
   wasn't intentional.

---

## 6. Files changed

- `Packages/StreamVideo/Runtime/Core/StatefulModels/StreamVideoCallParticipant.cs`
  - `IsPublishingTrack`, `AddPublishedTrack`, `RemovePublishedTrack`.
- `Packages/StreamVideo/Runtime/Core/LowLevelClient/RtcSession.cs`
  - `ShouldSubscribeToVideoTrack` gated on `PublishedTracks`; new `IsParticipantPublishingTrack` helper.
  - `OnSfuTrackPublished` / `OnSfuTrackUnpublished` maintain `PublishedTracks` when DTO omitted.
  - Replaced the misleading "validated this is how Android/JS handle this" comment with an accurate one.

No linter errors. Not yet compiled in the Unity editor (do this as the first verification step).

---

## 7. How to verify

### 7.1 Reproduce / confirm fix (3–4 devices, delayed camera)
1. Build the sample to 3–4 devices with the repro config (§2) and `STREAM_DEBUG_ENABLED`.
2. Device A joins; ~3s later its camera turns on. Device B joins; ~3s later its camera turns on.
3. **Expected (fixed):** A and B see each other's video within a few seconds of camera enable, **without**
   needing Device C to join.

### 7.2 Expected healthy log sequence on the receiver after a peer enables camera late
1. `trackPublished TRACK_TYPE_VIDEO` for the publishing peer.
2. `UpdateSubscriptionsRequest` that **now contains that peer's video** (it didn't before the publish).
3. `subscriberOffer` with a new video m-line.
4. `OnTrack` / `VideoStreamTrack` received.
5. `Track received from <user>` (sample `ParticipantView.OnParticipantTrackAdded`).

### 7.3 Negative signal (bug still present / not this build)
- `trackPublished TRACK_TYPE_VIDEO`, but the subsequent `UpdateSubscriptionsRequest` does **not** include
  that peer's video → the `PublishedTracks` sync isn't happening (check `OnSfuTrackPublished` DTO-absent path
  and that the participant exists in `ActiveCall.Participants` at that moment).
- `UpdateSubscriptions` includes the video and returns SUCCESS but no `subscriberOffer` follows → likely a
  server-side issue (see §8).

---

## 8. Edge cases & follow-ups

- **`trackPublished` arrives before the participant exists** in `ActiveCall.Participants`:
  `UpdateParticipantTracksState` returns `participant == null`, so `AddPublishedTrack` is skipped — but
  `QueueTracksSubscriptionRequest()` still fires, and when the participant later joins, its DTO carries
  `publishedTracks` (incl. video) and `OnSfuParticipantJoined` re-queues. Self-heals.
- **Audio** is intentionally left ungated (Unity requests audio explicitly; works because audio is enabled at
  join). JS/Android rely on implicit SFU audio subscription. Could be aligned later but is out of scope.
- **Screen share** subscription selection is still not implemented (pre-existing TODO in
  `GetDesiredTracksDetails`).
- **Server-side caveat:** if logs show the SFU genuinely never sends `subscriberOffer` after a changed
  `UpdateSubscriptions` (§7.3 second bullet), that's a server issue no client change can fully resolve.
- **`STREAM_DEBUG_ENABLED`** gates the verbose `UpdateSubscriptionsRequest` log; the behavior itself runs in
  all builds.

---

## 9. Reference: how the SDKs compare

| Aspect | JS / Android | Unity (before) | Unity (this fix) |
|--------|--------------|----------------|------------------|
| Include video in subscription | Only if peer is **publishing** (`hasVideo`/`videoEnabled`) | If `SetIncomingVideoEnabled(true)`, even pre-publish | Opt-in **and** peer publishing |
| Re-subscribe on late publish | Subscription set recomputes (set changes) | `OnSfuTrackPublished` → re-send identical set (no-op) | Set changes → SFU offers |
| Mute (`trackUnpublished`) | Drops video sub; unmute re-subscribes | Keeps sub, disables display | Drops video sub; unmute re-subscribes |
| Audio subscription | Implicit (not requested) | Explicit request flag | Explicit request flag (unchanged) |

Key JS references:
- `stream-video-js/packages/client/src/helpers/TrackSubscriptionManager.ts` (`subscriptions` getter).
- `stream-video-js/packages/client/src/helpers/participantUtils.ts` (`hasVideo`).
- `stream-video-js/packages/client/src/events/participant.ts` (`watchTrackPublished` /
  `watchTrackUnpublished` — update publish-state only, don't re-subscribe directly).

Key Android references:
- `stream-video-android-core/.../call/connection/Subscriber.kt` (`defaultTracks` / `visibleTracks`).
- `stream-video-android-core/.../call/RtcSession.kt` (`TrackPublishedEvent` → `updatePublishState`, not a
  direct re-subscribe).

---

## 10. Related notes / files

- `Packages/StreamVideo/.agents/notes/late-video-subscription-fix.md` — the **earlier** reconciliation-poll
  approach (now superseded by this publish-gated fix; kept for history).
- `Packages/StreamVideo/.agents/notes/video-subscription-investigation.md` — original investigation (some
  sections describe latch bugs already handled in current code).
- `Packages/StreamVideo/.agents/specs/sample-delayed-camera-enable.md` — reproduction sample spec.
