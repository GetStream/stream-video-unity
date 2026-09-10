var UnityWebRTCVideoRenderer = {
  CreateVideoRenderer: function (contextPtr, callback, needFlip) {
    var renderer = {
      callback: callback,
      needFlip: !!needFlip
    };
    uwcom_addManageObj(renderer);
    return renderer.managePtr;
  },

  CreateNativeTexture: function() {
    //console.log('nativeTexture');
    var texPtr = 0;
    for(var texPtr = 0; texPtr < GL.textures.length; texPtr++) {
      if(GL.textures[texPtr] === undefined)
        break;
    }
    var tex = GLctx.createTexture();
    tex.name = texPtr;
    GL.textures[texPtr] = tex;
    //console.log('nativeTexture' + texPtr);
    return texPtr;
  },
  
  VideoSourceGetSyncApplicationFramerate: function (sourcePtr) {
    return 0;
  },

  VideoSourceSetSyncApplicationFramerate: function (sourcePtr, value) {
  },

  SetGraphicsSyncTimeout: function(nSecTimeout) {
  },
  
  GetVideoRendererId: function (sinkPtr) {
    return sinkPtr;
  },

  DeleteVideoRenderer: function (contextPtr, sinkPtr) {
  },

  UpdateRendererTexture: function (trackPtr, renderTexturePtr, needFlip) {
    // console.log('UpdateRendererTexture');
    if (!uwcom_existsCheck(trackPtr, 'UpdateRendererTexture', 'track')) return;
    if (!uwcom_remoteVideoTracks[trackPtr]) return;
    //console.log('UpdateRendererTexture', renderTexturePtr);
    var video = uwcom_remoteVideoTracks[trackPtr].video;
    var tex = GL.textures[renderTexturePtr];
    GLctx.bindTexture(GLctx.TEXTURE_2D, tex);
    if (!!needFlip){
      //GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
    }
    // For now: Flip every time, since we want the correct image transfered over WebRTC
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
    try {
      GLctx.texSubImage2D(GLctx.TEXTURE_2D, 0, 0, 0, GLctx.RGBA, GLctx.UNSIGNED_BYTE, video);
    } catch (err) {
      GLctx.texImage2D(GLctx.TEXTURE_2D, 0, GLctx.RGBA, GLctx.RGBA, GLctx.UNSIGNED_BYTE, video);
    }
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, false);
    GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_MAG_FILTER, GLctx.LINEAR);
    GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_MIN_FILTER, GLctx.LINEAR);
    GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_WRAP_S, GLctx.CLAMP_TO_EDGE);
    GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_WRAP_T, GLctx.CLAMP_TO_EDGE);
    GLctx.bindTexture(GLctx.TEXTURE_2D, null);
  },

  // Not used in WebGL, but is here to make the NativeMethods more maintainable. 
  VideoTrackAddOrUpdateSink: function(trackPtr, sinkPtr){
    var remote = uwcom_remoteVideoTracks[trackPtr];
    var renderer = UWManaged[sinkPtr];
    if (renderer) {
      renderer.trackPtr = trackPtr;
    }
    if (!remote || !remote.video || !renderer || !renderer.callback) {
      return;
    }
    remote.sinkPtr = sinkPtr;
    var video = remote.video;
    var width = video.videoWidth || 640;
    var height = video.videoHeight || 480;
    Module.dynCall_viii(renderer.callback, sinkPtr, width, height);
  },

  VideoTrackRemoveSink: function(trackPtr, sinkPtr){
    var remote = uwcom_remoteVideoTracks[trackPtr];
    if (remote) {
      delete remote.sinkPtr;
    }
  }
};
mergeInto(LibraryManager.library, UnityWebRTCVideoRenderer);