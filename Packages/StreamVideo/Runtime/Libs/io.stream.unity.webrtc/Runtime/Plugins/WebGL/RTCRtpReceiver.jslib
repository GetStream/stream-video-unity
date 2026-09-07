var UnityWebRTCRtpReceiver = {
  DeleteReceiver: function (receiverPtr) {
    if (!uwcom_existsCheck(receiverPtr, 'DeleteReceiver', 'receiver')) return;
    delete UWManaged[receiverPtr];
  },

  ReceiverGetTrack: function (receiverPtr) {
    if (!uwcom_existsCheck(receiverPtr, 'ReceiverGetTrack', 'receiver')) return;
    var receiver = UWManaged[receiverPtr];
    if (!receiver.track) return 0;
    uwcom_addManageObj(receiver.track);
    return receiver.track.managePtr;
  },
  
  ReceiverGetStreams: function(receiverPtr){
    if (!uwcom_existsCheck(receiverPtr, 'ReceiverGetStreams', 'receiver')) return;
    var receiver = UWManaged[receiverPtr];
    var streams = receiver._streams || [];
    var ptrs = [];
    streams.forEach(function (stream) {
      uwcom_addManageObj(stream);
      ptrs.push(stream.managePtr);
    });
    return uwcom_arrayToReturnPtr(ptrs, Int32Array);
  },

  ReceiverGetSources: function(receiverPtr){
    return uwcom_arrayToReturnPtr([], Int32Array);
  },

  ReceiverSetTransform: function (receiverPtr, transformPtr) {
  }
};
mergeInto(LibraryManager.library, UnityWebRTCRtpReceiver);