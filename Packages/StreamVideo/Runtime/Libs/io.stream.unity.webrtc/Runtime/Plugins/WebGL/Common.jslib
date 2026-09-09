var UnityWebRTCCommon = {
  $uwcom_logLevel: 0,
  $uwcom_managePtr: 0,
  $uwcom_localAudioTracks: {},
  $uwcom_localVideoTracks: {},
  $uwcom_remoteAudioTracks: {},
  $uwcom_remoteVideoTracks: {},
  $uwcom_audioContext: null,
  $uwcom_autoplayUnlockInstalled: 0,

  $uwcom_addManageObj: function (obj) {
    if (!obj.managePtr) {
      uwcom_managePtr++;
      obj.managePtr = uwcom_managePtr;
      UWManaged[obj.managePtr] = obj;
    }
    else if(!UWManaged[obj.managePtr]){
      UWManaged[obj.managePtr] = obj;
    }
  },
  $uwcom_strToPtr: function (str) {
    if (str == null) str = '';
    var len = lengthBytesUTF8(str) + 1;
    var ptr = _malloc(len);
    stringToUTF8(str, ptr, len);
    return ptr;
  },
  $uwcom_arrayToReturnPtr: function (arr, type) {
    var buf = (new type(arr)).buffer;
    var ui8a = new Uint8Array(buf);
    var ptr = _malloc(ui8a.byteLength + 4);
    HEAP32.set([arr.length], ptr >> 2);
    HEAPU8.set(ui8a, ptr + 4);
    setTimeout(function () {
      _free(ptr);
    }, 0);
    return ptr;
  },
  $uwcom_errorNo: function (err) {
    var errNo = UWRTCErrorType.indexOf(err.name);
    if (errNo === -1)
      errNo = 0;
    return errNo;
  },
  $uwcom_fixStatEnumValue: function (stat) {
    if (stat.type === 'codec') {
      if (stat.codecType) {
        stat.codecType = UWRTCCodecType.indexOf(stat.codecType);
        if (stat.codecType === -1) return false;
      }
    }
    if (stat.type === 'outbound-rtp') {
      if (stat.qualityLimitationReason) {
        stat.qualityLimitationReason = UWRTCQualityLimitationReason.indexOf(stat.qualityLimitationReason);
        if (stat.qualityLimitationReason === -1) return false;
      }
      if (stat.priority) {
        stat.priority = UWRTCPriorityType.indexOf(stat.priority);
        if (stat.priority === -1) return false;
      }
    }
    if (stat.type === 'media-source') {
      if (stat.kind) {
        stat.kind = UWMediaStreamTrackKind.indexOf(stat.kind);
        if (stat.kind === -1) return false;
      }
    }
    if (stat.type === 'data-channel') {
      if (stat.state) {
        stat.state = UWRTCDataChannelState.indexOf(stat.state);
        if (stat.state === -1) return false;
      }
    }
    if (stat.type === 'transport') {
      if (stat.iceRole) {
        stat.iceRole = UWRTCIceRole.indexOf(stat.iceRole);
        if (stat.iceRole === -1) return false;
      }
      if (stat.dtlsState) {
        stat.dtlsState = UWRTCDtlsTransportState.indexOf(stat.dtlsState);
        if (stat.dtlsState === -1) return false;
      }
      if (stat.iceState) {
        stat.iceState = UWRTCIceTransportState.indexOf(stat.iceState);
        if (stat.iceState === -1) return false;
      }
    }
    if (stat.type === 'local-candidate'
      || stat.type === 'remote-candidate') {
      if (stat.candidateType) {
        stat.candidateType = UWRTCIceCandidateType.indexOf(stat.candidateType);
        if (stat.candidateType === -1) return false;
      }
    }
    if (stat.type === 'candidate-pair') {
      if (stat.state) {
        stat.state = UWRTCStatsIceCandidatePairState.indexOf(stat.state);
        if (stat.state === -1) return false;
      }
    }
    stat.type = UWRTCStatsType.indexOf(stat.type);
    return true;
  },
  $uwcom_statsSerialize: function (stats) {
    var statsJsons = [];
    stats.forEach((function(stat) {
      if (uwcom_fixStatEnumValue(stat)) statsJsons.push(stat);
    }));
    var statsDataJson = JSON.stringify(statsJsons);
    var statsDataJsonPtr = uwcom_strToPtr(statsDataJson);
    return statsDataJsonPtr;
  },
  $uwcom_existsCheck: function (ptr, funcName, typeName) {
    var obj = UWManaged[ptr];
    if (obj) return true;
    console.error("[jslib] " + funcName + ": Unmanaged " + typeName + ". Ptr: " + ptr);
    return false;
  },
  $uwcom_getIdx: function (enum_, val) {
    enum_.indexOf()
  },
  $uwcom_debugLog: function (level, fileName, member, msg) {
    if (!level) return;
    var logLevels = ['', '', '', '', '', '', '', 'error', 'warning', 'log', 'verbose'];
    var levelNo = logLevels.indexOf(level);
    if (levelNo === -1) return;
    if ((uwcom_logLevel > 0 && uwcom_logLevel <= 3 && levelNo > 0 && (levelNo - 6) <= uwcom_logLevel) ||
      (uwcom_logLevel > 6 && uwcom_logLevel <= 9 && levelNo > 6 && levelNo <= uwcom_logLevel)) {
        msg = '[JSLIB] ' + fileName + ' : ' + member + ' : ' + msg; 
      var msgPtr = uwcom_strToPtr(msg);
      // shift level number so 9(log) => 1(NativeLoggingSeverity.Info)
      Module.dynCall_vii(uwevt_DebugLog, msgPtr, Math.min(logLevels.indexOf('log') - levelNo + 1,4));
      _free(msgPtr);
    }
  },

  $uwcom_attachHiddenMediaElement: function (el) {
    el.autoplay = true;
    el.playsInline = true;
    el.setAttribute('playsinline', '');
    el.setAttribute('webkit-playsinline', '');
    el.style.cssText = 'position:fixed;width:1px;height:1px;opacity:0;pointer-events:none;left:-9999px;bottom:0;';
    if (document.body && !el.parentNode) {
      document.body.appendChild(el);
    }
    var tryPlay = function () {
      var p = el.play();
      if (p && p.catch) {
        p.catch(function () {});
      }
    };
    tryPlay();
    el.addEventListener('canplay', tryPlay);
  },

  $uwcom_unlockPlayback: function () {
    if (uwcom_audioContext && uwcom_audioContext.state === 'suspended') {
      uwcom_audioContext.resume();
    }
    var retry = function (el) {
      if (!el) return;
      var p = el.play();
      if (p && p.catch) {
        p.catch(function () {});
      }
    };
    Object.keys(uwcom_remoteAudioTracks).forEach(function (k) {
      retry(uwcom_remoteAudioTracks[k].audio);
    });
    Object.keys(uwcom_remoteVideoTracks).forEach(function (k) {
      retry(uwcom_remoteVideoTracks[k].video);
    });
  },

  $uwcom_ensureAutoplayUnlock: function () {
    if (uwcom_autoplayUnlockInstalled) return;
    uwcom_autoplayUnlockInstalled = 1;
    var unlock = function () {
      uwcom_unlockPlayback();
      document.removeEventListener('pointerdown', unlock, true);
      document.removeEventListener('keydown', unlock, true);
    };
    document.addEventListener('pointerdown', unlock, true);
    document.addEventListener('keydown', unlock, true);
  },

  $uwcom_releaseMediaTrack: function (trackPtr) {
    var local = uwcom_localVideoTracks[trackPtr];
    if (local) {
      if (local.cnv && local.cnv.parentNode) {
        local.cnv.remove();
      }
      if (local.stream) {
        local.stream.getTracks().forEach(function (t) { t.stop(); });
      }
      delete uwcom_localVideoTracks[trackPtr];
    }
    var remoteV = uwcom_remoteVideoTracks[trackPtr];
    if (remoteV) {
      if (remoteV.video) {
        remoteV.video.remove();
      }
      if (remoteV.track && remoteV.track.stop) {
        remoteV.track.stop();
      }
      delete uwcom_remoteVideoTracks[trackPtr];
    }
    var remoteA = uwcom_remoteAudioTracks[trackPtr];
    if (remoteA) {
      if (remoteA.audio) {
        remoteA.audio.remove();
      }
      if (remoteA.track && remoteA.track.stop) {
        remoteA.track.stop();
      }
      delete uwcom_remoteAudioTracks[trackPtr];
    }
  },

  $UWManaged: {},

  $uwcom_dynCall: function (sig, funcPtr) {
    if (!funcPtr) {
      console.error('[jslib] uwcom_dynCall: null callback, sig=' + sig);
      return;
    }
    funcPtr = funcPtr | 0;
    var args = Array.prototype.slice.call(arguments, 2);
    try {
      var named = Module['dynCall_' + sig];
      if (typeof named === 'function') {
        return named.apply(null, [funcPtr].concat(args));
      }
      if (typeof Module.dynCall === 'function') {
        try {
          return Module.dynCall.apply(null, [sig, funcPtr].concat(args));
        } catch (e1) {
          return Module.dynCall(sig, funcPtr, args);
        }
      }
      var table = Module['wasmTable'] || (typeof wasmTable !== 'undefined' ? wasmTable : null);
      if (table && typeof table.get === 'function') {
        var fn = table.get(funcPtr);
        if (typeof fn === 'function') {
          return fn.apply(null, args);
        }
      }
      console.error('[jslib] uwcom_dynCall: no invoker for sig=' + sig + ' ptr=' + funcPtr);
    } catch (err) {
      console.error('[jslib] uwcom_dynCall failed, sig=' + sig + ' ptr=' + funcPtr, err);
    }
  },

  $uwevt_DebugLog: null,
  $uwevt_PCOnIceCandidate: null,
  $uwevt_PCOnIceConnectionChange: null,
  $uwevt_PCOnConnectionStateChange: null,
  $uwevt_PCOnIceGatheringChange: null,
  $uwevt_PCOnNegotiationNeeded: null,
  $uwevt_PCOnDataChannel: null,
  $uwevt_PCOnTrack: null,
  $uwevt_PCOnRemoveTrack: null,
  $uwevt_MSOnAddTrack: null,
  $uwevt_MSOnRemoveTrack: null,
  $uwevt_DCOnTextMessage: null,
  $uwevt_DCOnBinaryMessage: null,
  $uwevt_DCOnOpen: null,
  $uwevt_DCOnClose: null,
  $uwevt_DCOnError: null,
  $uwevt_OnSetSessionDescSuccess: null,
  $uwevt_OnSetSessionDescFailure: null,
  $uwevt_OnSuccessCreateSessionDesc: null,
  $uwevt_OnFailureCreateSessionDesc: null,
  $uwevt_OnStatsDeliveredCallback: null,

  RegisterDebugLog: function (debugLogPtr,enableNativeLog,nativeLoggingSeverity) {
    var logLevels = ['', '', '', '', '', '', '', 'error', 'warning', 'log', 'verbose'];
    // shift level number so 1(NativeLoggingSeverity.Info) => 9(log)
    uwcom_logLevel = logLevels.indexOf('log') - nativeLoggingSeverity + 1;
    uwevt_DebugLog = debugLogPtr;
  },

};
autoAddDeps(UnityWebRTCCommon, '$uwcom_logLevel');
autoAddDeps(UnityWebRTCCommon, '$uwcom_debugLog');
autoAddDeps(UnityWebRTCCommon, '$uwcom_managePtr');
autoAddDeps(UnityWebRTCCommon, '$uwcom_localAudioTracks');
autoAddDeps(UnityWebRTCCommon, '$uwcom_localVideoTracks');
autoAddDeps(UnityWebRTCCommon, '$uwcom_remoteAudioTracks');
autoAddDeps(UnityWebRTCCommon, '$uwcom_remoteVideoTracks');
autoAddDeps(UnityWebRTCCommon, '$uwcom_audioContext');
autoAddDeps(UnityWebRTCCommon, '$uwcom_autoplayUnlockInstalled');
autoAddDeps(UnityWebRTCCommon, '$uwcom_attachHiddenMediaElement');
autoAddDeps(UnityWebRTCCommon, '$uwcom_unlockPlayback');
autoAddDeps(UnityWebRTCCommon, '$uwcom_ensureAutoplayUnlock');
autoAddDeps(UnityWebRTCCommon, '$uwcom_releaseMediaTrack');
autoAddDeps(UnityWebRTCCommon, '$UWManaged');
autoAddDeps(UnityWebRTCCommon, '$uwcom_dynCall');

autoAddDeps(UnityWebRTCCommon, '$uwevt_DebugLog');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnIceCandidate');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnIceConnectionChange');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnConnectionStateChange');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnIceGatheringChange');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnNegotiationNeeded');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnDataChannel');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnTrack');
autoAddDeps(UnityWebRTCCommon, '$uwevt_PCOnRemoveTrack');
autoAddDeps(UnityWebRTCCommon, '$uwevt_MSOnAddTrack');
autoAddDeps(UnityWebRTCCommon, '$uwevt_MSOnRemoveTrack');
autoAddDeps(UnityWebRTCCommon, '$uwevt_DCOnTextMessage');
autoAddDeps(UnityWebRTCCommon, '$uwevt_DCOnBinaryMessage');
autoAddDeps(UnityWebRTCCommon, '$uwevt_DCOnOpen');
autoAddDeps(UnityWebRTCCommon, '$uwevt_DCOnClose');
autoAddDeps(UnityWebRTCCommon, '$uwevt_DCOnError');
autoAddDeps(UnityWebRTCCommon, '$uwevt_OnSetSessionDescSuccess');
autoAddDeps(UnityWebRTCCommon, '$uwevt_OnSetSessionDescFailure');
autoAddDeps(UnityWebRTCCommon, '$uwevt_OnSuccessCreateSessionDesc');
autoAddDeps(UnityWebRTCCommon, '$uwevt_OnFailureCreateSessionDesc');
autoAddDeps(UnityWebRTCCommon, '$uwevt_OnStatsDeliveredCallback');

autoAddDeps(UnityWebRTCCommon, '$uwcom_addManageObj');
autoAddDeps(UnityWebRTCCommon, '$uwcom_strToPtr');
autoAddDeps(UnityWebRTCCommon, '$uwcom_arrayToReturnPtr');
autoAddDeps(UnityWebRTCCommon, '$uwcom_errorNo');
autoAddDeps(UnityWebRTCCommon, '$uwcom_fixStatEnumValue');
autoAddDeps(UnityWebRTCCommon, '$uwcom_statsSerialize');
autoAddDeps(UnityWebRTCCommon, '$uwcom_existsCheck');

mergeInto(LibraryManager.library, UnityWebRTCCommon);
