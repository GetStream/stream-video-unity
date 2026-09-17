#import <Accelerate/Accelerate.h>
#import <CoreVideo/CoreVideo.h>
#import <Foundation/Foundation.h>
#import <Vision/Vision.h>

#include <string.h>

// Async Vision person segmenter for Unity. Reuses the last mask and never blocks
// the caller. VNGeneratePersonSegmentationRequest is iOS 15+. Quality is chosen
// by managed code (Balanced on A12+, Fast otherwise) to match Stream's Swift SDK.
// destroy() is non-blocking: in-flight perform releases native resources when it
// finishes.

@interface StreamVisionPersonSegmenter : NSObject
- (BOOL)createWithBalancedQuality:(BOOL)useBalanced;
- (void)destroy;
- (BOOL)isBusy;
- (void)processAsync:(const uint8_t *)rgba
              length:(int)length
               width:(int)width
              height:(int)height
            rotation:(int)rotationDegrees;
- (int)takeMaskIfNew:(uint8_t *)dest
          destLength:(int)destLength
               width:(int *)width
              height:(int *)height
            rotation:(int *)rotation;
@end

@implementation StreamVisionPersonSegmenter {
    NSObject *_lock;
    dispatch_queue_t _queue;
    VNSequenceRequestHandler *_handler;
    id _request;
    CVPixelBufferRef _inputBuffer;
    NSMutableData *_maskPacked;
    int _maskWidth;
    int _maskHeight;
    int _maskRotation;
    int _inFlightRotation;
    BOOL _maskDirty;
    BOOL _inFlight;
    BOOL _destroyed;
    BOOL _created;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        _lock = [NSObject new];
        _queue = dispatch_queue_create("io.getstream.unity.backgroundfilters.vision", DISPATCH_QUEUE_SERIAL);
    }
    return self;
}

- (void)dealloc {
    [self releaseResources];
}

- (BOOL)createWithBalancedQuality:(BOOL)useBalanced {
    @synchronized (_lock) {
        if (_inFlight) {
            NSLog(@"[StreamBgFilter] Cannot create Vision segmenter while a process is still in flight.");
            return NO;
        }

        [self releaseResourcesLocked];
        _destroyed = NO;
        _created = NO;

        if (@available(iOS 15.0, *)) {
            VNGeneratePersonSegmentationRequest *request = [VNGeneratePersonSegmentationRequest new];
            request.qualityLevel = useBalanced
                ? VNGeneratePersonSegmentationRequestQualityLevelBalanced
                : VNGeneratePersonSegmentationRequestQualityLevelFast;
            request.outputPixelFormat = kCVPixelFormatType_OneComponent8;
            _request = request;
            _handler = [VNSequenceRequestHandler new];
            _created = YES;
            return YES;
        }

        NSLog(@"[StreamBgFilter] Vision person segmentation requires iOS 15 or later.");
        return NO;
    }
}

- (void)destroy {
    @synchronized (_lock) {
        _destroyed = YES;
        _maskDirty = NO;
        _maskPacked = nil;
        _maskWidth = 0;
        _maskHeight = 0;
        _maskRotation = 0;
        if (!_inFlight) {
            [self releaseResourcesLocked];
        }
    }
}

- (BOOL)isBusy {
    @synchronized (_lock) {
        return _inFlight;
    }
}

- (void)processAsync:(const uint8_t *)rgba
              length:(int)length
               width:(int)width
              height:(int)height
            rotation:(int)rotationDegrees {
    if (rgba == NULL || length < width * height * 4 || width <= 0 || height <= 0) {
        return;
    }

    NSData *frameCopy = nil;
    @synchronized (_lock) {
        if (_destroyed || !_created || _inFlight) {
            return;
        }

        _inFlight = YES;
        _inFlightRotation = rotationDegrees;
        frameCopy = [NSData dataWithBytes:rgba length:(NSUInteger)(width * height * 4)];
    }

    dispatch_async(_queue, ^{
        [self processFrame:frameCopy width:width height:height];
    });
}

- (void)processFrame:(NSData *)rgbaData width:(int)width height:(int)height {
    uint8_t *packed = NULL;
    int packedWidth = 0;
    int packedHeight = 0;

    if (@available(iOS 15.0, *)) {
        VNGeneratePersonSegmentationRequest *request = nil;
        VNSequenceRequestHandler *handler = nil;
        @synchronized (_lock) {
            if (_destroyed || !_created) {
                [self onProcessFinished:NULL width:0 height:0];
                return;
            }

            request = _request;
            handler = _handler;
        }

        CVPixelBufferRef buffer = [self inputBufferWithWidth:width height:height];
        if (request != nil && handler != nil && buffer != NULL
            && [self copyRgba:rgbaData intoBGRA:buffer width:width height:height]) {
            NSError *error = nil;
            [handler performRequests:@[request] onCVPixelBuffer:buffer error:&error];
            VNPixelBufferObservation *observation = request.results.firstObject;
            if (error == nil && observation.pixelBuffer != NULL) {
                [self copyScaledMask:observation.pixelBuffer
                           destWidth:width
                          destHeight:height
                              packed:&packed
                         packedWidth:&packedWidth
                        packedHeight:&packedHeight];
            } else if (error != nil) {
                NSLog(@"[StreamBgFilter] Vision segmentation failed: %@", error);
            }
        }
    }

    [self onProcessFinished:packed width:packedWidth height:packedHeight];
    free(packed);
}

- (void)onProcessFinished:(uint8_t *)packed width:(int)width height:(int)height {
    @synchronized (_lock) {
        _inFlight = NO;

        if (_destroyed) {
            [self releaseResourcesLocked];
            return;
        }

        if (packed == NULL || width <= 0 || height <= 0) {
            return;
        }

        NSUInteger byteCount = (NSUInteger)width * (NSUInteger)height;
        if (_maskPacked == nil || _maskPacked.length != byteCount) {
            _maskPacked = [NSMutableData dataWithLength:byteCount];
        }
        memcpy(_maskPacked.mutableBytes, packed, byteCount);
        _maskWidth = width;
        _maskHeight = height;
        _maskRotation = _inFlightRotation;
        _maskDirty = YES;
    }
}

- (int)takeMaskIfNew:(uint8_t *)dest
          destLength:(int)destLength
               width:(int *)width
              height:(int *)height
            rotation:(int *)rotation {
    if (width != NULL) {
        *width = 0;
    }
    if (height != NULL) {
        *height = 0;
    }
    if (rotation != NULL) {
        *rotation = 0;
    }

    @synchronized (_lock) {
        if (!_maskDirty || _maskPacked == nil || dest == NULL) {
            return 0;
        }

        int needed = _maskWidth * _maskHeight;
        if (needed <= 0) {
            return 0;
        }

        if (destLength < needed) {
            return -needed;
        }

        memcpy(dest, _maskPacked.bytes, (NSUInteger)needed);
        if (width != NULL) {
            *width = _maskWidth;
        }
        if (height != NULL) {
            *height = _maskHeight;
        }
        if (rotation != NULL) {
            *rotation = _maskRotation;
        }
        _maskDirty = NO;
        return needed;
    }
}

- (CVPixelBufferRef)inputBufferWithWidth:(int)width height:(int)height {
    @synchronized (_lock) {
        if (_inputBuffer != NULL
            && (int)CVPixelBufferGetWidth(_inputBuffer) == width
            && (int)CVPixelBufferGetHeight(_inputBuffer) == height) {
            return _inputBuffer;
        }

        if (_inputBuffer != NULL) {
            CVPixelBufferRelease(_inputBuffer);
            _inputBuffer = NULL;
        }

        NSDictionary *attrs = @{
            (id)kCVPixelBufferIOSurfacePropertiesKey: @{},
        };
        CVReturn status = CVPixelBufferCreate(
            kCFAllocatorDefault,
            (size_t)width,
            (size_t)height,
            kCVPixelFormatType_32BGRA,
            (__bridge CFDictionaryRef)attrs,
            &_inputBuffer);
        if (status != kCVReturnSuccess) {
            NSLog(@"[StreamBgFilter] Failed to create Vision input buffer: %d", (int)status);
            _inputBuffer = NULL;
        }

        return _inputBuffer;
    }
}

- (BOOL)copyRgba:(NSData *)rgbaData
        intoBGRA:(CVPixelBufferRef)buffer
           width:(int)width
          height:(int)height {
    if (buffer == NULL || rgbaData.length < (NSUInteger)width * (NSUInteger)height * 4) {
        return NO;
    }

    CVPixelBufferLockBaseAddress(buffer, 0);
    uint8_t *dst = (uint8_t *)CVPixelBufferGetBaseAddress(buffer);
    if (dst == NULL) {
        CVPixelBufferUnlockBaseAddress(buffer, 0);
        return NO;
    }
    size_t bytesPerRow = CVPixelBufferGetBytesPerRow(buffer);
    const uint8_t *src = (const uint8_t *)rgbaData.bytes;
    for (int y = 0; y < height; y++) {
        uint8_t *dstRow = dst + (size_t)y * bytesPerRow;
        const uint8_t *srcRow = src + (size_t)y * (size_t)width * 4;
        for (int x = 0; x < width; x++) {
            dstRow[x * 4 + 0] = srcRow[x * 4 + 2];
            dstRow[x * 4 + 1] = srcRow[x * 4 + 1];
            dstRow[x * 4 + 2] = srcRow[x * 4 + 0];
            dstRow[x * 4 + 3] = srcRow[x * 4 + 3];
        }
    }
    CVPixelBufferUnlockBaseAddress(buffer, 0);
    return YES;
}

- (void)copyScaledMask:(CVPixelBufferRef)mask
             destWidth:(int)destWidth
            destHeight:(int)destHeight
                packed:(uint8_t **)packed
           packedWidth:(int *)packedWidth
          packedHeight:(int *)packedHeight {
    if (mask == NULL || destWidth <= 0 || destHeight <= 0) {
        return;
    }

    CVPixelBufferLockBaseAddress(mask, kCVPixelBufferLock_ReadOnly);
    size_t srcW = CVPixelBufferGetWidth(mask);
    size_t srcH = CVPixelBufferGetHeight(mask);
    size_t srcRowBytes = CVPixelBufferGetBytesPerRow(mask);
    const uint8_t *src = (const uint8_t *)CVPixelBufferGetBaseAddress(mask);
    if (src == NULL || srcW == 0 || srcH == 0) {
        CVPixelBufferUnlockBaseAddress(mask, kCVPixelBufferLock_ReadOnly);
        return;
    }

    uint8_t *dest = (uint8_t *)malloc((size_t)destWidth * (size_t)destHeight);
    if (dest == NULL) {
        CVPixelBufferUnlockBaseAddress(mask, kCVPixelBufferLock_ReadOnly);
        return;
    }

    vImage_Buffer srcBuf = {
        .data = (void *)src,
        .width = srcW,
        .height = srcH,
        .rowBytes = srcRowBytes,
    };
    vImage_Buffer dstBuf = {
        .data = dest,
        .width = (vImagePixelCount)destWidth,
        .height = (vImagePixelCount)destHeight,
        .rowBytes = (size_t)destWidth,
    };
    vImage_Error scaleError = vImageScale_Planar8(&srcBuf, &dstBuf, NULL, kvImageNoFlags);
    CVPixelBufferUnlockBaseAddress(mask, kCVPixelBufferLock_ReadOnly);
    if (scaleError != kvImageNoError) {
        NSLog(@"[StreamBgFilter] Failed to scale Vision mask: %ld", (long)scaleError);
        free(dest);
        return;
    }

    *packed = dest;
    *packedWidth = destWidth;
    *packedHeight = destHeight;
}

- (void)releaseResources {
    @synchronized (_lock) {
        [self releaseResourcesLocked];
    }
}

- (void)releaseResourcesLocked {
    _handler = nil;
    _request = nil;
    _created = NO;
    _maskPacked = nil;
    _maskWidth = 0;
    _maskHeight = 0;
    _maskRotation = 0;
    _maskDirty = NO;
    if (_inputBuffer != NULL) {
        CVPixelBufferRelease(_inputBuffer);
        _inputBuffer = NULL;
    }
}

@end

static StreamVisionPersonSegmenter *s_segmenter;

extern "C" {

bool _StreamVisionPersonSegmenter_IsSupported(void) {
    if (@available(iOS 15.0, *)) {
        return YES;
    }
    return NO;
}

bool _StreamVisionPersonSegmenter_Create(int useBalanced) {
    if (s_segmenter == nil) {
        s_segmenter = [StreamVisionPersonSegmenter new];
    }
    return [s_segmenter createWithBalancedQuality:useBalanced != 0];
}

void _StreamVisionPersonSegmenter_Destroy(void) {
    [s_segmenter destroy];
}

bool _StreamVisionPersonSegmenter_IsBusy(void) {
    return [s_segmenter isBusy];
}

void _StreamVisionPersonSegmenter_ProcessAsync(const uint8_t *rgba, int length, int width, int height,
                                               int rotationDegrees) {
    [s_segmenter processAsync:rgba length:length width:width height:height rotation:rotationDegrees];
}

int _StreamVisionPersonSegmenter_TakeMaskIfNew(uint8_t *dest, int destLength, int *width, int *height,
                                               int *rotation) {
    if (width != NULL) {
        *width = 0;
    }
    if (height != NULL) {
        *height = 0;
    }
    if (rotation != NULL) {
        *rotation = 0;
    }

    return [s_segmenter takeMaskIfNew:dest destLength:destLength width:width height:height rotation:rotation];
}

}
