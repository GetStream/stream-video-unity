package io.getstream.unitybackgroundfilters;

import android.graphics.Bitmap;
import android.util.Log;

import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.segmentation.Segmentation;
import com.google.mlkit.vision.segmentation.SegmentationMask;
import com.google.mlkit.vision.segmentation.Segmenter;
import com.google.mlkit.vision.segmentation.selfie.SelfieSegmenterOptions;

import java.nio.ByteBuffer;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Async ML Kit selfie segmenter for Unity. Reuses the last mask and never blocks the caller.
 * {@link #destroy()} is non-blocking: in-flight {@code process} releases native resources from
 * its listener so the bitmap is not recycled while ML Kit still holds it.
 */
public class UnityMlKitPersonSegmenter {
    private static final String TAG = "StreamBgFilter";

    private final Object lock = new Object();
    private final AtomicBoolean inFlight = new AtomicBoolean(false);
    private boolean destroyed;

    private boolean debugLogs;
    private final Map<String, String> lastDebugByKey = new HashMap<String, String>();
    private Segmenter segmenter;
    private Bitmap reusableBitmap;
    private int[] argbScratch;

    private byte[] latestMask;
    private int maskWidth;
    private int maskHeight;
    private boolean maskDirty;
    private int inFlightRotation;
    private int maskRotation;

    /**
     * Classpath check only. Does not call {@link Segmentation#getClient}.
     */
    public static boolean isSupported() {
        try {
            Class.forName("com.google.mlkit.vision.segmentation.Segmentation");
            return true;
        } catch (Throwable t) {
            Log.w(TAG, "ML Kit selfie segmentation is not on the classpath.", t);
            return false;
        }
    }

    public boolean create() {
        synchronized (lock) {
            if (inFlight.get()) {
                Log.w(TAG, "Cannot create ML Kit segmenter while a process is still in flight.");
                return false;
            }

            releaseLocked();
            destroyed = false;

            try {
                // Do not enableRawSizeMask(): that returns the 256x256 model tensor, which we were
                // stretching onto the 16:9 camera frame. Let ML Kit rescale the mask to the bitmap size.
                SelfieSegmenterOptions options = new SelfieSegmenterOptions.Builder()
                        .setDetectorMode(SelfieSegmenterOptions.STREAM_MODE)
                        .build();
                segmenter = Segmentation.getClient(options);
                return true;
            } catch (Throwable t) {
                Log.w(TAG, "Failed to create ML Kit segmenter.", t);
                segmenter = null;
                return false;
            }
        }
    }

    public void setDebugLogs(boolean enabled) {
        debugLogs = enabled;
        debug("init", "debug logs " + (enabled ? "on" : "off"));
    }

    public boolean isBusy() {
        return inFlight.get();
    }

    /**
     * {@code rgba} must already be upright (Unity rotates webcam pixels clockwise by
     * {@code rotationDegrees} before calling). {@code fromBitmap} stays at 0 so ML Kit
     * does not swap mask dimensions. {@code rotationDegrees} is stored and returned with
     * the mask so Unity can inverse-rotate back to webcam UVs.
     */
    public void processAsync(byte[] rgba, int width, int height, int rotationDegrees) {
        if (rgba == null || width <= 0 || height <= 0) {
            return;
        }

        Segmenter active;
        InputImage image;
        synchronized (lock) {
            if (destroyed || segmenter == null || !inFlight.compareAndSet(false, true)) {
                return;
            }

            try {
                Bitmap bitmap = getBitmapLocked(width, height);
                copyRgbaToBitmap(rgba, width, height, bitmap);
                inFlightRotation = rotationDegrees;
                // Pixels are already upright; passing webcam rotation here would swap mask size.
                image = InputImage.fromBitmap(bitmap, 0);
                active = segmenter;
            } catch (Throwable t) {
                inFlight.set(false);
                Log.w(TAG, "Failed to copy frame for ML Kit.", t);
                return;
            }
        }

        try {
            debug("submit", "processAsync bitmap=" + width + "x" + height
                    + " mlkitRotationDegrees=0 pixelsRotatedCW=" + rotationDegrees
                    + " rgbaBytes=" + rgba.length);
            active.process(image)
                    .addOnSuccessListener(this::onMaskSuccess)
                    .addOnFailureListener(this::onMaskFailure);
        } catch (Throwable t) {
            onProcessFinished(null, 0, 0);
            Log.w(TAG, "Failed to submit frame to ML Kit.", t);
        }
    }

    public byte[] takeMaskIfNew() {
        synchronized (lock) {
            if (!maskDirty) {
                return null;
            }

            maskDirty = false;
            return latestMask;
        }
    }

    public int getMaskWidth() {
        synchronized (lock) {
            return maskWidth;
        }
    }

    public int getMaskHeight() {
        synchronized (lock) {
            return maskHeight;
        }
    }

    public int getMaskRotation() {
        synchronized (lock) {
            return maskRotation;
        }
    }

    public void destroy() {
        synchronized (lock) {
            destroyed = true;
            clearMaskLocked();
            if (!inFlight.get()) {
                releaseLocked();
            }
        }
    }

    private void onMaskSuccess(SegmentationMask mask) {
        byte[] packed = null;
        int width = 0;
        int height = 0;
        try {
            if (mask != null) {
                width = mask.getWidth();
                height = mask.getHeight();
                ByteBuffer buffer = mask.getBuffer();
                buffer.rewind();
                int pixelCount = width * height;
                packed = new byte[pixelCount];
                for (int i = 0; i < pixelCount; i++) {
                    float confidence = buffer.getFloat();
                    int value = (int) (confidence * 255.0f);
                    if (value < 0) {
                        value = 0;
                    } else if (value > 255) {
                        value = 255;
                    }
                    packed[i] = (byte) value;
                }

                debug("mask", "onMaskSuccess mask=" + width + "x" + height
                        + " bitmap=" + describeBitmap()
                        + " aspectMatch=" + bitmapAspectMatches(width, height));
            }
        } catch (Throwable t) {
            Log.w(TAG, "Failed to copy ML Kit mask.", t);
            packed = null;
            width = 0;
            height = 0;
        }

        onProcessFinished(packed, width, height);
    }

    private void onMaskFailure(Exception e) {
        Log.w(TAG, "ML Kit segmentation failed.", e);
        onProcessFinished(null, 0, 0);
    }

    private void onProcessFinished(byte[] packed, int width, int height) {
        synchronized (lock) {
            inFlight.set(false);

            if (destroyed) {
                releaseLocked();
                return;
            }

            if (packed == null || width <= 0 || height <= 0) {
                return;
            }

            latestMask = packed;
            maskWidth = width;
            maskHeight = height;
            maskRotation = inFlightRotation;
            maskDirty = true;
        }
    }

    private void releaseLocked() {
        if (segmenter != null) {
            try {
                segmenter.close();
            } catch (Throwable t) {
                Log.w(TAG, "Failed to close ML Kit segmenter.", t);
            }
            segmenter = null;
        }

        if (reusableBitmap != null && !reusableBitmap.isRecycled()) {
            reusableBitmap.recycle();
        }
        reusableBitmap = null;
        argbScratch = null;

        synchronized (lastDebugByKey) {
            lastDebugByKey.clear();
        }
    }

    private void clearMaskLocked() {
        latestMask = null;
        maskWidth = 0;
        maskHeight = 0;
        maskRotation = 0;
        maskDirty = false;
    }

    private Bitmap getBitmapLocked(int width, int height) {
        if (reusableBitmap == null
                || reusableBitmap.isRecycled()
                || reusableBitmap.getWidth() != width
                || reusableBitmap.getHeight() != height) {
            if (reusableBitmap != null && !reusableBitmap.isRecycled()) {
                reusableBitmap.recycle();
            }
            reusableBitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
        }

        return reusableBitmap;
    }

    private void copyRgbaToBitmap(byte[] rgba, int width, int height, Bitmap bitmap) {
        int pixelCount = width * height;
        if (argbScratch == null || argbScratch.length < pixelCount) {
            argbScratch = new int[pixelCount];
        }

        int required = pixelCount * 4;
        if (rgba.length < required) {
            throw new IllegalArgumentException("RGBA buffer is smaller than width*height*4");
        }

        for (int i = 0, p = 0; i < pixelCount; i++, p += 4) {
            int r = rgba[p] & 0xFF;
            int g = rgba[p + 1] & 0xFF;
            int b = rgba[p + 2] & 0xFF;
            int a = rgba[p + 3] & 0xFF;
            argbScratch[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        bitmap.setPixels(argbScratch, 0, width, 0, 0, width, height);
    }

    private String describeBitmap() {
        Bitmap bitmap = reusableBitmap;
        if (bitmap == null || bitmap.isRecycled()) {
            return "null";
        }

        return bitmap.getWidth() + "x" + bitmap.getHeight();
    }

    private boolean bitmapAspectMatches(int width, int height) {
        Bitmap bitmap = reusableBitmap;
        return bitmap != null && !bitmap.isRecycled()
                && width * bitmap.getHeight() == height * bitmap.getWidth();
    }

    private void debug(String key, String message) {
        if (!debugLogs || message == null) {
            return;
        }

        synchronized (lastDebugByKey) {
            String previous = lastDebugByKey.get(key);
            if (message.equals(previous)) {
                return;
            }

            lastDebugByKey.put(key, message);
        }
        Log.i(TAG, message);
    }
}
