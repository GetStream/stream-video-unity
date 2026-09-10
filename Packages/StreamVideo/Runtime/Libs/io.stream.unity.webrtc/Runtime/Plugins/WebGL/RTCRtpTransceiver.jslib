var UnityWebRTCRtpTransceiver = {
  DeleteTransceiver: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'DeleteTransceiver', 'transceiver')) return;
    delete UWManaged[transceiverPtr];
  },

  TransceiverGetMid: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverGetMid', 'transceiver')) return 0;
    var transceiver = UWManaged[transceiverPtr];
    if (transceiver.mid == null || transceiver.mid === '') {
      return 0;
    }
    return uwcom_strToPtr(transceiver.mid);
  },

  TransceiverGetDirection: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverGetDirection', 'transceiver')) return;
    var transceiver = UWManaged[transceiverPtr];
    return UWRTCRtpTransceiverDirection.indexOf(transceiver.direction);
  },

  TransceiverSetDirection: function (transceiverPtr, directionIdx) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverSetDirection', 'transceiver')) return 0;
    var transceiver = UWManaged[transceiverPtr];
    transceiver.direction = UWRTCRtpTransceiverDirection[directionIdx];
    return 0;
  },

  TransceiverGetCurrentDirection: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverGetCurrentDirection', 'transceiver')) return;
    var transceiver = UWManaged[transceiverPtr];
    return UWRTCRtpTransceiverDirection.indexOf(transceiver.currentDirection);
  },

  TransceiverGetReceiver: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverGetReceiver', 'transceiver')) return;
    var transceiver = UWManaged[transceiverPtr];
    uwcom_addManageObj(transceiver.receiver);
    return transceiver.receiver.managePtr;
  },

  TransceiverGetSender: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverGetSender', 'transceiver')) return;
    var transceiver = UWManaged[transceiverPtr];
    uwcom_addManageObj(transceiver.sender);
    return transceiver.sender.managePtr;
  },

  TransceiverSetCodecPreferences: function (transceiverPtr, codecsPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverSetCodecPreferences', 'transceiver')) return UWRTCErrorType.indexOf("OperationErrorWithData");

    var transceiver = UWManaged[transceiverPtr];
    var codecsJson = UTF8ToString(codecsPtr);
    var codecs = JSON.parse(codecsJson);

    const supportsSetCodecPreferences = window.RTCRtpTransceiver && 'setCodecPreferences' in window.RTCRtpTransceiver.prototype;
    if (!supportsSetCodecPreferences) {
      return UWRTCErrorType.indexOf("UnsupportedOperation");
    }

    try {
      // Chrome rejects reconstructed JSON codecs (extra channels:0, etc). Map back onto
      // the RTCRtpCodecCapability objects returned by getCapabilities().
      var kind = (transceiver.sender && transceiver.sender.track && transceiver.sender.track.kind) ||
                 (transceiver.receiver && transceiver.receiver.track && transceiver.receiver.track.kind) ||
                 'video';
      var caps = RTCRtpSender.getCapabilities(kind);
      var mapped = [];
      if (caps && caps.codecs && codecs && codecs.length) {
        for (var i = 0; i < codecs.length; i++) {
          var want = codecs[i];
          for (var j = 0; j < caps.codecs.length; j++) {
            var have = caps.codecs[j];
            if (have.mimeType === want.mimeType &&
                have.clockRate === want.clockRate &&
                (have.sdpFmtpLine || '') === (want.sdpFmtpLine || '')) {
              mapped.push(have);
              break;
            }
          }
        }
      }
      transceiver.setCodecPreferences(mapped.length ? mapped : codecs);
      return UWRTCErrorType.indexOf("None");
    } catch (err) {
      return UWRTCErrorType.indexOf("InvalidModification");
    }
  },

  TransceiverStop: function (transceiverPtr) {
    if (!uwcom_existsCheck(transceiverPtr, 'TransceiverStop', 'transceiver')) return;
    try {
      var transceiver = UWManaged[transceiverPtr];
      transceiver.stop();
      return true;
    } catch (err) {
      return false;
    }
  }
};
mergeInto(LibraryManager.library, UnityWebRTCRtpTransceiver);