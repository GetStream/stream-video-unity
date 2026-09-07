var UnityWebRTCRtpSender = {
  DeleteSender: function (senderPtr) {
    if (!uwcom_existsCheck(senderPtr, 'DeleteSender', 'sender')) return;
    delete UWManaged[senderPtr];
  },

  SenderGetTrack: function (senderPtr) {
    if (!uwcom_existsCheck(senderPtr, 'SenderGetTrack', 'sender')) return;
    var sender = UWManaged[senderPtr];
    if(sender.track){
      uwcom_addManageObj(sender.track);
      return sender.track.managePtr;
    }
  },

  SenderGetParameters: function (senderPtr) {
    if (!uwcom_existsCheck(senderPtr, 'SenderGetParameters', 'sender')) return;
    var sender = UWManaged[senderPtr];
    var parameters = sender.getParameters();
    var parametersJson = JSON.stringify(parameters);
    var parametersJsonPtr = uwcom_strToPtr(parametersJson);
    return parametersJsonPtr;
  },

  SenderSetParameters: function (senderPtr, parametersJsonPtr) {
    if (!uwcom_existsCheck(senderPtr, 'SenderSetParameters', 'sender')) return 11;
    var sender = UWManaged[senderPtr];
    var parametersJson = UTF8ToString(parametersJsonPtr);
    var incoming = JSON.parse(parametersJson);
    var current = sender.getParameters();
    if (incoming.encodings && current.encodings) {
      for (var i = 0; i < incoming.encodings.length && i < current.encodings.length; i++) {
        var src = incoming.encodings[i];
        var dst = current.encodings[i];
        dst.active = src.active;
        if (src.maxBitrate) dst.maxBitrate = src.maxBitrate;
        if (src.minBitrate) dst.minBitrate = src.minBitrate;
        if (src.maxFramerate) dst.maxFramerate = src.maxFramerate;
        if (src.scaleResolutionDownBy) dst.scaleResolutionDownBy = src.scaleResolutionDownBy;
        if (src.rid) dst.rid = src.rid;
      }
    }
    if (incoming.transactionId) current.transactionId = incoming.transactionId;
    sender.setParameters(current).catch(function (err) {
      console.error(err);
    });
    return 0;
  },

  SenderReplaceTrack: function (senderPtr, trackPtr) {
    if (!uwcom_existsCheck(senderPtr, 'SenderReplaceTrack', 'sender')) return 0;
    if (trackPtr && !uwcom_existsCheck(trackPtr, 'SenderReplaceTrack', 'track')) return 0;
    var sender = UWManaged[senderPtr];
    var track = trackPtr ? UWManaged[trackPtr] : null;
    sender.replaceTrack(track);
    return 1;
  },

  SenderSetTransform: function (senderPtr, transformPtr) {
    return 0;
  }
};
mergeInto(LibraryManager.library, UnityWebRTCRtpSender);