package io.getstream.unitybackgroundfilters;

import android.graphics.Bitmap;
import android.util.Log;

import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.segmentation.Segmentation;
import com.google.mlkit.vision.segmentation.SegmentationMask;
import com.google.mlkit.vision.segmentation.Segmenter;
import com.google.mlkit.vision.segmentation.selfie.SelfieSegmenterOptions;

import java.nio.ByteBuffer;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Async ML Kit selfie segmenter for Unity. Reuses the last mask and never blocks the caller.
 */
public class UnityMlKitPersonSegmenter {
    private static final String TAG = "StreamBgFilter";

    private final AtomicBoolean inFlight = new AtomicBoolean(false);
    private Segmenter segmenter;
    private Bitmap reusableBitmap;
    private int[] argbScratch;

    private byte[] latestMask;
    private int maskWidth;
    private int maskHeight;
    private boolean maskDirty;

    public boolean isSupported() {
        try {
            Class.forName("com.google.mlkit.vision.segmentation.Segmentation");
            return true;
        } catch (Throwable t) {
            Log.w(TAG, "ML Kit selfie segmentation is not on the classpath.", t);
            return false;
        }
    }

    public boolean create() {
        destroy();
        try {
            SelfieSegmenterOptions options = new SelfieSegmenterOptions.Builder()
                    .setDetectorMode(SelfieSegmenterOptions.STREAM_MODE)
                    .enableRawSizeMask()
                    .build();
            segmenter = Segmentation.getClient(options);
            return true;
        } catch (Throwable t) {
            Log.w(TAG, "Failed to create ML Kit segmenter.", t);
            segmenter = null;
            return false;
        }
    }

    public boolean isBusy() {
        return inFlight.get();
    }

    public void processAsync(byte[] rgba, int width, int height) {
        if (segmenter == null || rgba == null || width <= 0 || height <= 0) {
            return;
        }

        if (!inFlight.compareAndSet(false, true)) {
            return;
        }

        try {
            Bitmap bitmap = getBitmap(width, height);
            copyRgbaToBitmap(rgba, width, height, bitmap);
            InputImage image = InputImage.fromBitmap(bitmap, 0);
            segmenter.process(image)
                    .addOnSuccessListener(this::onMaskSuccess)
                    .addOnFailureListener(this::onMaskFailure);
        } catch (Throwable t) {
            inFlight.set(false);
            Log.w(TAG, "Failed to submit frame to ML Kit.", t);
        }
    }

    public synchronized byte[] takeMaskIfNew() {
        if (!maskDirty) {
            return null;
        }

        maskDirty = false;
        return latestMask;
    }

    public synchronized int getMaskWidth() {
        return maskWidth;
    }

    public synchronized int getMaskHeight() {
        return maskHeight;
    }

    public void destroy() {
        inFlight.set(false);
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

        synchronized (this) {
            latestMask = null;
            maskWidth = 0;
            maskHeight = 0;
            maskDirty = false;
        }
    }

    private Bitmap getBitmap(int width, int height) {
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

    private void onMaskSuccess(SegmentationMask mask) {
        try {
            int width = mask.getWidth();
            int height = mask.getHeight();
            ByteBuffer buffer = mask.getBuffer();
            buffer.rewind();
            int pixelCount = width * height;
            byte[] packed = new byte[pixelCount];
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

            synchronized (this) {
                latestMask = packed;
                maskWidth = width;
                maskHeight = height;
                maskDirty = true;
            }
        } catch (Throwable t) {
            Log.w(TAG, "Failed to copy ML Kit mask.", t);
        } finally {
            inFlight.set(false);
        }
    }

    private void onMaskFailure(Exception e) {
        Log.w(TAG, "ML Kit segmentation failed.", e);
        inFlight.set(false);
    }
}
