var UnityWebRTCAudioStreamTrack = {
  CreateAudioTrack__deps: ['$uwcom_ensureAutoplayUnlock'],
  CreateAudioTrack: function (labelPtr, sourcePtr) {
    if (!uwcom_audioContext) {
      uwcom_audioContext = new AudioContext();
    }
    uwcom_ensureAutoplayUnlock();
    if (uwcom_audioContext.state === 'suspended') {
      uwcom_audioContext.resume();
    }
    var dest = uwcom_audioContext.createMediaStreamDestination();
    var audioTrack = dest.stream.getAudioTracks()[0];
    uwcom_addManageObj(audioTrack);
    audioTrack.guid = UTF8ToString(labelPtr);
    if (sourcePtr && UWManaged[sourcePtr]) {
      UWManaged[sourcePtr].dest = dest;
    } else {
      audioTrack._dest = dest;
    }
    return audioTrack.managePtr;
  },

  AudioSourceProcessLocalAudio: function (sourcePtr, arrayPtr, sampleRate, channels, frames) {
    if (!uwcom_audioContext) {
      uwcom_audioContext = new AudioContext();
    }
    if (uwcom_audioContext.state === 'suspended') {
      uwcom_audioContext.resume();
    }
    var source = UWManaged[sourcePtr];
    var dest = source && source.dest;
    if (!dest) {
      return;
    }
    var count = frames * channels;
    if (count <= 0) {
      return;
    }
    var heapIndex = arrayPtr >> 2;
    var data = HEAPF32.subarray(heapIndex, heapIndex + count);
    var buffer = uwcom_audioContext.createBuffer(channels, frames, sampleRate);
    var ch;
    var i;
    if (channels === 1) {
      buffer.getChannelData(0).set(data);
    } else {
      for (ch = 0; ch < channels; ch++) {
        var channelData = buffer.getChannelData(ch);
        for (i = 0; i < frames; i++) {
          channelData[i] = data[i * channels + ch];
        }
      }
    }
    var node = uwcom_audioContext.createBufferSource();
    node.buffer = buffer;
    node.connect(dest);
    node.start();
  },

  ProcessAudio: function (data, size) {
  }
};
mergeInto(LibraryManager.library, UnityWebRTCAudioStreamTrack);
