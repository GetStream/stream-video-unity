var UnityWebRTCMediaStreamTrack = {
  MediaStreamTrackGetEnabled: function (trackPtr) {
    if (!uwcom_existsCheck(trackPtr, 'MediaStreamTrackGetEnabled', 'track')) return;
    var track = UWManaged[trackPtr];
    return track.enabled;
  },

  MediaStreamTrackSetEnabled: function (trackPtr, enabled) {
    if (!uwcom_existsCheck(trackPtr, 'MediaStreamTrackSetEnabled', 'track')) return;
    var track = UWManaged[trackPtr];
    track.enabled = !!enabled;
  },

  MediaStreamTrackGetReadyState: function (trackPtr) {
    if (!uwcom_existsCheck(trackPtr, 'MediaStreamTrackGetReadyState', 'track')) return;
    var track = UWManaged[trackPtr];
    return UWMediaStreamTrackState.indexOf(track.readyState);
  },

  MediaStreamTrackGetKind: function (trackPtr) {
    if (!uwcom_existsCheck(trackPtr, 'MediaStreamTrackGetKind', 'track')) return;
    var track = UWManaged[trackPtr];
    return UWMediaStreamTrackKind.indexOf(track.kind);
  },

  MediaStreamTrackGetID: function (trackPtr) {
    if (!uwcom_existsCheck(trackPtr, 'MediaStreamTrackGetID', 'track')) return;
    var track = UWManaged[trackPtr];
    // SFU SetPublisher track ids must match SDP a=msid track ids (browser track.id).
    var id = track.id || track.guid;
    var idPtr = uwcom_strToPtr(id);
    return idPtr;
  }
};
mergeInto(LibraryManager.library, UnityWebRTCMediaStreamTrack);