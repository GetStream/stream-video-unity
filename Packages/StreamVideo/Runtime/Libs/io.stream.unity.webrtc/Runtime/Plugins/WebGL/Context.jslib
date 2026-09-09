var UnityWebRTCContext = {
  GetHardwareEncoderSupport: function () {
    return true;
  },

  ContextCreate__deps: ['$UWEncoderType'],
  ContextCreate: function (uid, encodeType) {
    var context = {
      id: uid,
      refPtr: new Set(),
      encodeType: UWEncoderType[encodeType]
    };
    uwcom_addManageObj(context);
    return context.managePtr;
  },

  ContextDestroy: function (uid) {
    var contextPtrs = Object.keys(UWManaged).filter(function (contextPtr) {
      return 'id' in UWManaged[contextPtr] && UWManaged[contextPtr].id === uid;
    });
    if (contextPtrs.length > 1) {
      console.error('ContextDestroy: multiple Contexts with the same id');
    } else if (!contextPtrs.length) {
      console.error('ContextDestroy: There is no context with id = ' + uid.toString());
    }
    contextPtrs.forEach(function (contextPtr) {
      delete UWManaged[contextPtr];
    });
  },

  SetCurrentContext: function (contextPtr) {
    UWManaged["__currentContext__"] = UWManaged[contextPtr];
  },

  // TODO
  ContextAddRefPtr: function (contextPtr,ptr){
    if (!uwcom_existsCheck(contextPtr, "ContextDeleteRefPtr", "context")) return;
    /** @type {{ refPtr: Set }} */
    var context = UWManaged[contextPtr];
    if(context && context.refPtr)
      context.refPtr.add(ptr);
  },

  // TODO
  ContextDeleteRefPtr: function(contextPtr,ptr){
    if (!uwcom_existsCheck(contextPtr, "ContextDeleteRefPtr", "context")) return;
    /** @type {{ refPtr: Set }} */
    var context = UWManaged[contextPtr];
    if(context.refPtr)
      context.refPtr.delete(ptr);
  },

  // TODO
  ContextCreateAudioTrackSource: function(contextPtr){
    if (!uwcom_existsCheck(contextPtr, "ContextCreateAudioTrackSource", "context")) return;
    const audioTrackSource = {};
    uwcom_addManageObj(audioTrackSource);
    return audioTrackSource.managePtr;
  },

  // TODO
  ContextCreateVideoTrackSource: function(contextPtr){
    if (!uwcom_existsCheck(contextPtr, "ContextCreateVideoTrackSource", "context")) return;
    const videoTrackSource = {};
    uwcom_addManageObj(videoTrackSource);
    return videoTrackSource.managePtr;
  },

  ContextGetEncoderType: function (contextPtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextGetEncoderType', 'context')) return;
    var context = UWManaged[contextPtr];
    var encodeTypeIdx = UWEncoderType.indexOf(context.encodeType);
    return encodeTypeIdx;
  },

  ContextCreatePeerConnection__deps: ['CreatePeerConnection'],
  ContextCreatePeerConnection: function (contextPtr, conf) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreatePeerConnection', 'context')) return;
    return _CreatePeerConnection(conf);
  },

  ContextCreatePeerConnectionWithConfig__deps: ['CreatePeerConnectionWithConfig'],
  ContextCreatePeerConnectionWithConfig: function (contextPtr, confPtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreatePeerConnectionWithConfig', 'context')) return;
    return _CreatePeerConnectionWithConfig(confPtr);
  },

  ContextDeletePeerConnection: function (contextPtr, peerPtr) {
    //if (!uwcom_existsCheck(contextPtr, 'ContextDeletePeerConnection', 'context')) return;
    if (!uwcom_existsCheck(peerPtr, 'ContextDeletePeerConnection', 'peer')) return;
    var peer = UWManaged[peerPtr];
    if (peer.readyState !== 'closed' || peer.signalingState !== 'closed')
      peer.close();
    delete UWManaged[peerPtr];
  },

  PeerConnectionSetLocalDescription__deps: ['PeerConnectionSetDescription'],
  PeerConnectionSetLocalDescription: function (contextPtr, peerPtr, typeIdx, sdpPtr) {
    if (!uwcom_existsCheck(contextPtr, 'PeerConnectionSetLocalDescription', 'context')) return 11; // OperationErrorWithData
    if (!uwcom_existsCheck(peerPtr, 'PeerConnectionSetLocalDescription', 'peer')) return 11; // OperationErrorWithData
    return _PeerConnectionSetDescription(peerPtr, typeIdx, sdpPtr, 'Local');
  },

  PeerConnectionSetRemoteDescription__deps: ['PeerConnectionSetDescription'],
  PeerConnectionSetRemoteDescription: function (contextPtr, peerPtr, typeIdx, sdpPtr) {
    if (!uwcom_existsCheck(contextPtr, 'PeerConnectionSetRemoteDescription', 'context')) return 11; // OperationErrorWithData
    if (!uwcom_existsCheck(peerPtr, 'PeerConnectionSetRemoteDescription', 'peer')) return 11; // OperationErrorWithData
    return _PeerConnectionSetDescription(peerPtr, typeIdx, sdpPtr, 'Remote');
  },

  PeerConnectionSetLocalDescriptionWithoutDescription__deps: ['PeerConnectionSetDescriptionWithoutDescription'],
  PeerConnectionSetLocalDescriptionWithoutDescription: function (contextPtr, peerPtr) {
    if (!uwcom_existsCheck(contextPtr, 'PeerConnectionSetLocalDescriptionWithoutDescription', 'context')) return 11; // OperationErrorWithData
    if (!uwcom_existsCheck(peerPtr, 'PeerConnectionSetLocalDescriptionWithoutDescription', 'peer')) return 11; // OperationErrorWithData
    return _PeerConnectionSetDescriptionWithoutDescription(peerPtr);
  },

  PeerConnectionRegisterOnSetSessionDescSuccess: function (contextPtr, peerPtr, OnSetSessionDescSuccess) {
    if (!uwcom_existsCheck(contextPtr, 'PeerConnectionRegisterOnSetSessionDescSuccess', 'context')) return;
    if (!uwcom_existsCheck(peerPtr, 'PeerConnectionRegisterOnSetSessionDescSuccess', 'peer')) return;
    uwevt_OnSetSessionDescSuccess = OnSetSessionDescSuccess;
  },

  PeerConnectionRegisterOnSetSessionDescFailure: function (contextPtr, peerPtr, OnSetSessionDescFailure) {
    if (!uwcom_existsCheck(contextPtr, 'PeerConnectionRegisterOnSetSessionDescFailure', 'context')) return;
    if (!uwcom_existsCheck(peerPtr, 'PeerConnectionRegisterOnSetSessionDescFailure', 'peer')) return;
    uwevt_OnSetSessionDescFailure = OnSetSessionDescFailure;
  },

  ContextCreateDataChannel__deps: ['CreateDataChannel'],
  ContextCreateDataChannel: function (contextPtr, peerPtr, labelPtr, optionsJsonPtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreateDataChannel', 'context')) return;
    if (!uwcom_existsCheck(peerPtr, 'ContextCreateDataChannel', 'peer')) return;
    return _CreateDataChannel(peerPtr, labelPtr, optionsJsonPtr);
  },

  ContextDeleteDataChannel: function (contextPtr, dataChannelPtr) {
    //if (!uwcom_existsCheck(contextPtr, 'ContextDeleteDataChannel', 'context')) return;
    if (!uwcom_existsCheck(dataChannelPtr, 'ContextDeleteDataChannel', 'dataChannel')) return;
    delete UWManaged[dataChannelPtr];
  },

  ContextCreateMediaStream__deps: ['CreateMediaStream'],
  ContextCreateMediaStream: function (contextPtr, labelPtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreateMediaStream', 'context')) return;
    return _CreateMediaStream(labelPtr);
  },

  ContextDeleteMediaStream__deps: ['DeleteMediaStream'],
  ContextDeleteMediaStream: function (contextPtr, streamPtr) {
    //if (!uwcom_existsCheck(contextPtr, 'ContextDeleteMediaStream', 'context')) return;
    if (!uwcom_existsCheck(streamPtr, 'ContextDeleteMediaStream', 'stream')) return;
    _DeleteMediaStream(streamPtr);
  },

  ContextRegisterMediaStreamObserver: function (contextPtr, streamPtr) {
    if (!uwcom_existsCheck(contextPtr, 'MediaStreamRegisterOnAddTrack', 'context')) return;
    if (!uwcom_existsCheck(streamPtr, 'MediaStreamRegisterOnAddTrack', 'stream')) return;

    var stream = UWManaged[streamPtr];
    stream.onaddtrack = (function(evt) {
      uwcom_addManageObj(evt.track);
      Module.dynCall_vii(uwevt_MSOnAddTrack, stream.managePtr, evt.track.managePtr);
    });
    stream.onremovetrack = (function(evt) {
      if (!uwcom_existsCheck(evt.track.managePtr, "stream.onremovetrack", "track")) return;
      Module.dynCall_vii(uwevt_MSOnRemoveTrack, stream.managePtr, evt.track.managePtr);
    });
  },

  ContextUnRegisterMediaStreamObserver: function (contextPtr, streamPtr) {
    if (!uwcom_existsCheck(contextPtr, 'MediaStreamRegisterOnAddTrack', 'context')) return;
    if (!uwcom_existsCheck(streamPtr, 'MediaStreamRegisterOnAddTrack', 'stream')) return;
    var context = UWManaged[contextPtr];
    var stream = UWManaged[streamPtr];
  },

  MediaStreamRegisterOnAddTrack: function (contextPtr, streamPtr, MediaStreamOnAddTrack) {
    if (!uwcom_existsCheck(contextPtr, 'MediaStreamRegisterOnAddTrack', 'context')) return;
    if (!uwcom_existsCheck(streamPtr, 'MediaStreamRegisterOnAddTrack', 'stream')) return;
    var context = UWManaged[contextPtr];
    var stream = UWManaged[streamPtr];
    uwevt_MSOnAddTrack = MediaStreamOnAddTrack;
  },

  MediaStreamRegisterOnRemoveTrack: function (contextPtr, streamPtr, MediaStreamOnRemoveTrack) {
    if (!uwcom_existsCheck(contextPtr, 'MediaStreamRegisterOnRemoveTrack', 'context')) return;
    if (!uwcom_existsCheck(streamPtr, 'MediaStreamRegisterOnRemoveTrack', 'stream')) return;
    var context = UWManaged[contextPtr];
    var stream = UWManaged[streamPtr];
    uwevt_MSOnRemoveTrack = MediaStreamOnRemoveTrack;
  },

  StatsCollectorRegisterCallback: function(onCollectStatsCallback) {
    uwevt_OnStatsDeliveredCallback = onCollectStatsCallback;
  },
  CreateSessionDescriptionObserverRegisterCallback: function(nativeCreateSessionDescCallback) {
  },
  SetLocalDescriptionObserverRegisterCallback: function(setLocalDescriptionCallback) {
  },
  SetRemoteDescriptionObserverRegisterCallback: function(setRemoteDescriptionCallback) {
  },
  SetTransformedFrameRegisterCallback: function(transformedFrameCallback) {
  },

  WebGLRegisterCreateSessionCallbacks__deps: [
    '$uwevt_OnSuccessCreateSessionDesc',
    '$uwevt_OnFailureCreateSessionDesc'
  ],
  WebGLRegisterCreateSessionCallbacks: function(successPtr, failurePtr) {
    uwevt_OnSuccessCreateSessionDesc = successPtr;
    uwevt_OnFailureCreateSessionDesc = failurePtr;
  },
  WebGLRegisterSetSessionCallbacks__deps: [
    '$uwevt_OnSetSessionDescSuccess',
    '$uwevt_OnSetSessionDescFailure'
  ],
  WebGLRegisterSetSessionCallbacks: function(successPtr, failurePtr) {
    uwevt_OnSetSessionDescSuccess = successPtr;
    uwevt_OnSetSessionDescFailure = failurePtr;
  },

  GetBatchUpdateEventFunc: function (contextPtr) {
    return 0;
  },

  GetBatchUpdateEventID: function () {
    return -1;
  },

  AudioTrackAddSink: function(trackPtr, sinkPtr) {
    if (!uwcom_existsCheck(trackPtr, 'AudioTrackAddSink', 'track')) return;
    if (!uwcom_existsCheck(sinkPtr, 'AudioTrackAddSink', 'sink')) return;
    var track = UWManaged[trackPtr];
    var sink = UWManaged[sinkPtr];
  },

  AudioTrackRemoveSink: function(trackPtr, sinkPtr) {
    if (!uwcom_existsCheck(trackPtr, 'AudioTrackRemoveSink', 'track')) return;
    if (!uwcom_existsCheck(sinkPtr, 'AudioTrackRemoveSink', 'sink')) return;
  },

  AudioTrackSinkProcessAudio: function(sinkPtr, data, length, channels, sampleRate) {
  },

  ContextCreateAudioTrackSink: function (contextPtr) {
    var sink = {};
    uwcom_addManageObj(sink);
    return sink.managePtr;
  },

  ContextDeleteAudioTrackSink: function (contextPtr, sinkPtr) {
    delete UWManaged[sinkPtr];
  },

  ContextUnregisterAudioReceiveCallback: function (contextPtr, trackPtr){
  },

  FrameGetTimestamp: function (framePtr) { return 0; },
  FrameGetSsrc: function (framePtr) { return 0; },
  FrameGetData: function (framePtr, dataPtr, sizePtr) { },
  FrameSetData: function (framePtr, dataPtr, size) { },
  VideoFrameGetMetadata: function (framePtr) { return 0; },
  VideoFrameIsKeyFrame: function (framePtr) { return 0; },
  FrameTransformerSendFrameToSink: function (transformPtr, framePtr) { },

  GetUpdateTextureFunc: function (contextPtr) {
    return 0;
  },

  ContextSetVideoEncoderParameter: function (contextPtr, trackPtr, width, height, format, texturePtr) {
  },

  ContextCreateFrameTransformer: function (contextPtr) {
    return 0;
  },

  ContextCreateAudioTrack__deps: ['CreateAudioTrack'],
  ContextCreateAudioTrack: function (contextPtr, labelPtr, sourcePtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreateAudioTrack', 'context')) return;
    return _CreateAudioTrack(labelPtr, sourcePtr);
  },

  ContextCreateVideoTrack__deps: ['CreateVideoTrack'],
  ContextCreateVideoTrack: function (contextPtr, srcTexturePtr, dstTexturePtr, width, height) {
    if (!uwcom_existsCheck(contextPtr, 'ContextCreateVideoTrack', 'context')) return;
    return _CreateVideoTrack(srcTexturePtr, dstTexturePtr, width, height);
  },

  ContextStopMediaStreamTrack__deps: ['$uwcom_releaseMediaTrack'],
  ContextStopMediaStreamTrack: function (contextPtr, trackPtr) {
    if (!uwcom_existsCheck(contextPtr, 'ContextStopMediaStreamTrack', 'context')) return;
    if (!uwcom_existsCheck(trackPtr, 'ContextStopMediaStreamTrack', 'track')) return;
    var track = UWManaged[trackPtr];
    if (track.stop) {
      track.stop();
    }
    uwcom_releaseMediaTrack(trackPtr);
  },

  ContextDeleteMediaStreamTrack__deps: ['$uwcom_releaseMediaTrack'],
  ContextDeleteMediaStreamTrack: function (contextPtr, trackPtr) {
    if (!uwcom_existsCheck(trackPtr, 'ContextDeleteMediaStreamTrack', 'track')) return;
    uwcom_releaseMediaTrack(trackPtr);
    delete UWManaged[trackPtr];
  },

  ContextRegisterAudioReceiveCallback: function (contextPtr, trackPtr, AudioTrackOnReceive){
    if (!uwcom_existsCheck(contextPtr, 'ContextRegisterAudioReceiveCallback', 'context')) return;
    if (!uwcom_existsCheck(trackPtr, 'ContextRegisterAudioReceiveCallback', 'track')) return;
    var context = UWManaged[contextPtr];
    var track = UWManaged[trackPtr];
  },

  // CreateVideoRenderer: function(contextPtr) {

  // },

  // DeleteVideoRenderer: function(contextPtr, sinkPtr) {

  // },

  ContextDeleteStatsReport: function (contextPtr, reportPtr) {
    //if (!uwcom_existsCheck(contextPtr, 'ContextDeleteStatsReport', 'context')) return;
    if (!uwcom_existsCheck(reportPtr, 'ContextDeleteStatsReport', 'report')) return;
    delete UWManaged[reportPtr];
  },

  // ContextSetVideoEncoderParameter: function(trackPtr, width, height, format, texturePtr) {

  // },

  // GetInitializationResult: function(contextPtr, trackPtr) {

  // },

  $UWContextGetCapabilities: function (senderReceiver, kindIdx) {
    var kind = UWMediaStreamTrackKind[kindIdx];
    var capabilities = {codecs:[], headerExtensions: []};
    const supportsSetCodecPreferences = window.RTCRtpTransceiver && 'setCodecPreferences' in window.RTCRtpTransceiver.prototype;
    if(supportsSetCodecPreferences) capabilities = senderReceiver.getCapabilities(kind);
    var capabilitiesJson = JSON.stringify(capabilities);
    var capabilitiesJsonPtr = uwcom_strToPtr(capabilitiesJson);
    return capabilitiesJsonPtr;
  },

  ContextGetSenderCapabilities: function (contextPtr, kindIdx) {
    if (!uwcom_existsCheck(contextPtr, 'ContextGetSenderCapabilities', 'context')) return;
    return UWContextGetCapabilities(RTCRtpSender, kindIdx);
  },

  ContextGetReceiverCapabilities: function (contextPtr, kindIdx) {
    if (!uwcom_existsCheck(contextPtr, 'ContextGetReceiverCapabilities', 'context')) return;
    return UWContextGetCapabilities(RTCRtpReceiver, kindIdx);
  },

  StatsGetJson: function (statsPtr) { return 0; },
  StatsGetId: function (statsPtr) { return 0; },
  StatsGetType: function (statsPtr) { return 0; },
  StatsGetTimestamp: function (statsPtr) { return 0; },
  StatsGetMembers: function (statsPtr, lengthPtr) { return 0; },
  StatsMemberGetName: function (memberPtr) { return 0; },
  StatsMemberGetType: function (memberPtr) { return 0; },
  StatsMemberIsDefined: function (memberPtr) { return 0; },
  StatsMemberGetBool: function (memberPtr) { return 0; },
  StatsMemberGetInt: function (memberPtr) { return 0; },
  StatsMemberGetUnsignedInt: function (memberPtr) { return 0; },
  StatsMemberGetLong: function (memberPtr) { return 0; },
  StatsMemberGetUnsignedLong: function (memberPtr) { return 0; },
  StatsMemberGetDouble: function (memberPtr) { return 0; },
  StatsMemberGetString: function (memberPtr) { return 0; },
  StatsMemberGetBoolArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetIntArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetUnsignedIntArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetLongArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetUnsignedLongArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetDoubleArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetStringArray: function (memberPtr, lengthPtr) { return 0; },
  StatsMemberGetMapStringUint64: function (memberPtr, valuesPtr, lengthPtr) { return 0; },
  StatsMemberGetMapStringDouble: function (memberPtr, valuesPtr, lengthPtr) { return 0; }
};
autoAddDeps(UnityWebRTCContext, '$UWContextGetCapabilities');
autoAddDeps(UnityWebRTCContext, '$uwevt_OnSuccessCreateSessionDesc');
autoAddDeps(UnityWebRTCContext, '$uwevt_OnFailureCreateSessionDesc');
autoAddDeps(UnityWebRTCContext, '$uwevt_OnSetSessionDescSuccess');
autoAddDeps(UnityWebRTCContext, '$uwevt_OnSetSessionDescFailure');
autoAddDeps(UnityWebRTCContext, '$uwevt_MSOnAddTrack');
autoAddDeps(UnityWebRTCContext, '$uwevt_MSOnRemoveTrack');
autoAddDeps(UnityWebRTCContext, '$uwevt_OnStatsDeliveredCallback');
mergeInto(LibraryManager.library, UnityWebRTCContext);