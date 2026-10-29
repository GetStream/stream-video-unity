using System;
using UnityEngine;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    internal enum KeyboardVisibility
    {
        Unknown = 0,
        Hidden,
        Visible,
        Transitioning
    }

    internal enum KeyboardProviderId
    {
        None = 0,
        Android,
        Ios,
        Simulated
    }

    public enum KeyboardAvoidanceStatus
    {
        Disabled,
        WaitingForPanel,
        Idle,
        WaitingForGeometry,
        Avoiding,
        VisibleWithoutAdjustment,
        Constrained,
        Suspended,
        Disposed
    }

    public enum KeyboardAvoidanceDiagnosticCode
    {
        None,
        PlatformUnsupported,
        NativeProviderUnavailable,
        KeyboardGeometryUnknown,
        PanelUnavailable,
        MovableContainerMissing,
        InvalidHierarchy,
        TransformConflict,
        InsufficientSpace,
        UnsupportedViewport,
        MultipleOwners
    }

    /// <summary>
    /// A rectangle in normalized Unity-view space. Origin is the top-left of the view, Y grows downward.
    /// </summary>
    internal readonly struct NormalizedRect
    {
        public NormalizedRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public static NormalizedRect Empty => default;

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public bool IsEmpty => Width <= 0f || Height <= 0f;

        public bool Approximately(NormalizedRect other)
        {
            const float epsilon = 0.001f;
            return Mathf.Abs(X - other.X) < epsilon
                   && Mathf.Abs(Y - other.Y) < epsilon
                   && Mathf.Abs(Width - other.Width) < epsilon
                   && Mathf.Abs(Height - other.Height) < epsilon;
        }
    }

    /// <summary>
    /// A rectangle in UI Toolkit panel space. Origin is the top-left, Y grows downward.
    /// </summary>
    internal readonly struct PanelRect
    {
        public PanelRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public float XMin { get; }
        public float YMin { get; }
        public float XMax { get; }
        public float YMax { get; }
        public float Width => XMax - XMin;
        public float Height => YMax - YMin;

        public static PanelRect FromRect(Rect rect) => new PanelRect(rect.xMin, rect.yMin, rect.xMax, rect.yMax);

        public bool OverlapsHorizontally(PanelRect other) => XMin < other.XMax && other.XMin < XMax;

        public PanelRect UnshiftedUp(float upwardShift)
            => new PanelRect(XMin, YMin + upwardShift, XMax, YMax + upwardShift);

        public bool IsFinite()
            => IsFinite(XMin) && IsFinite(YMin) && IsFinite(XMax) && IsFinite(YMax);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal struct KeyboardOcclusionList
    {
        public NormalizedRect R0;
        public NormalizedRect R1;
        public NormalizedRect R2;
        public NormalizedRect R3;
        public int Count;

        public void Add(NormalizedRect rect)
        {
            if (rect.IsEmpty || Count >= 4)
            {
                return;
            }

            if (Count == 0)
            {
                R0 = rect;
            }
            else if (Count == 1)
            {
                R1 = rect;
            }
            else if (Count == 2)
            {
                R2 = rect;
            }
            else
            {
                R3 = rect;
            }

            Count++;
        }
    }

    /// <summary>
    /// One measurement of how the software keyboard overlaps the Unity view.
    /// An empty occlusion with <see cref="IsValid"/> set means the keyboard does not cover the view.
    /// </summary>
    internal readonly struct KeyboardGeometrySnapshot
    {
        public KeyboardGeometrySnapshot(
            KeyboardVisibility state,
            bool isValid,
            KeyboardProviderId provider,
            int occlusionCount,
            NormalizedRect occlusion0,
            NormalizedRect occlusion1,
            NormalizedRect occlusion2,
            NormalizedRect occlusion3,
            int revision,
            double timestamp)
        {
            State = state;
            IsValid = isValid;
            Provider = provider;
            OcclusionCount = occlusionCount;
            _occlusion0 = occlusion0;
            _occlusion1 = occlusion1;
            _occlusion2 = occlusion2;
            _occlusion3 = occlusion3;
            Revision = revision;
            Timestamp = timestamp;
        }

        public KeyboardVisibility State { get; }
        public bool IsValid { get; }
        public KeyboardProviderId Provider { get; }
        public int OcclusionCount { get; }
        public int Revision { get; }
        public double Timestamp { get; }

        private readonly NormalizedRect _occlusion0;
        private readonly NormalizedRect _occlusion1;
        private readonly NormalizedRect _occlusion2;
        private readonly NormalizedRect _occlusion3;

        public static KeyboardGeometrySnapshot Hidden(KeyboardProviderId provider)
            => Create(KeyboardVisibility.Hidden, true, provider, default);

        public static KeyboardGeometrySnapshot Unknown(KeyboardProviderId provider)
            => Create(KeyboardVisibility.Unknown, false, provider, default);

        public static KeyboardGeometrySnapshot Create(
            KeyboardVisibility state,
            bool isValid,
            KeyboardProviderId provider,
            KeyboardOcclusionList occlusions)
        {
            return new KeyboardGeometrySnapshot(
                state,
                isValid,
                provider,
                occlusions.Count,
                occlusions.R0,
                occlusions.R1,
                occlusions.R2,
                occlusions.R3,
                0,
                0d);
        }

        public NormalizedRect Occlusion(int index)
        {
            switch (index)
            {
                case 0: return _occlusion0;
                case 1: return _occlusion1;
                case 2: return _occlusion2;
                case 3: return _occlusion3;
                default: return NormalizedRect.Empty;
            }
        }

        public bool SameGeometry(KeyboardGeometrySnapshot other)
        {
            if (State != other.State || IsValid != other.IsValid || Provider != other.Provider
                || OcclusionCount != other.OcclusionCount)
            {
                return false;
            }

            for (var i = 0; i < OcclusionCount; i++)
            {
                if (!Occlusion(i).Approximately(other.Occlusion(i)))
                {
                    return false;
                }
            }

            return true;
        }

        public KeyboardGeometrySnapshot WithRevision(int revision, double timestamp)
        {
            return new KeyboardGeometrySnapshot(
                State,
                IsValid,
                Provider,
                OcclusionCount,
                _occlusion0,
                _occlusion1,
                _occlusion2,
                _occlusion3,
                revision,
                timestamp);
        }
    }

    internal readonly struct KeyboardDisplacementResult
    {
        public KeyboardDisplacementResult(
            float upwardShift,
            KeyboardAvoidanceStatus status,
            KeyboardAvoidanceDiagnosticCode diagnostic,
            string message)
        {
            UpwardShift = upwardShift;
            Status = status;
            Diagnostic = diagnostic;
            Message = message;
        }

        public float UpwardShift { get; }
        public KeyboardAvoidanceStatus Status { get; }
        public KeyboardAvoidanceDiagnosticCode Diagnostic { get; }
        public string Message { get; }

        public static KeyboardDisplacementResult Unobstructed { get; } = new KeyboardDisplacementResult(
            0f,
            KeyboardAvoidanceStatus.VisibleWithoutAdjustment,
            KeyboardAvoidanceDiagnosticCode.None,
            null);

        public static KeyboardDisplacementResult Avoiding(float upwardShift)
            => new KeyboardDisplacementResult(upwardShift, KeyboardAvoidanceStatus.Avoiding,
                KeyboardAvoidanceDiagnosticCode.None, null);

        public static KeyboardDisplacementResult Constrained(float upwardShift, string message)
            => new KeyboardDisplacementResult(upwardShift, KeyboardAvoidanceStatus.Constrained,
                KeyboardAvoidanceDiagnosticCode.InsufficientSpace, message);
    }

    /// <summary>
    /// Clips keyboard rectangles to the Unity view and expresses the overlap as a normalized rect.
    /// </summary>
    internal static class KeyboardOcclusion
    {
        public static bool TryNormalizeIntersection(
            float keyboardXMin,
            float keyboardYMin,
            float keyboardXMax,
            float keyboardYMax,
            float viewX,
            float viewY,
            float viewWidth,
            float viewHeight,
            out NormalizedRect normalized)
        {
            if (viewWidth <= 0f || viewHeight <= 0f)
            {
                normalized = default;
                return false;
            }

            var left = Mathf.Max(keyboardXMin, viewX);
            var top = Mathf.Max(keyboardYMin, viewY);
            var right = Mathf.Min(keyboardXMax, viewX + viewWidth);
            var bottom = Mathf.Min(keyboardYMax, viewY + viewHeight);
            if (right - left <= 0f || bottom - top <= 0f)
            {
                normalized = NormalizedRect.Empty;
                return true;
            }

            normalized = new NormalizedRect(
                (left - viewX) / viewWidth,
                (top - viewY) / viewHeight,
                (right - left) / viewWidth,
                (bottom - top) / viewHeight);
            return true;
        }

        /// <summary>
        /// Pixels of a top-left layout rectangle that extend below <paramref name="visibleBottom"/>.
        /// The layout height is the UI Toolkit screen, which can stay tall after the native view shrinks.
        /// </summary>
        public static int BottomCoveredPixels(int originY, int layoutHeight, int visibleBottom)
        {
            if (layoutHeight <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, originY + layoutHeight - visibleBottom);
        }

        /// <summary>
        /// <paramref name="bottomLeft"/> uses Unity screen space: origin at the bottom-left, Y growing upward.
        /// </summary>
        public static bool TryFromBottomLeftScreenRect(
            Rect bottomLeft,
            float screenWidth,
            float screenHeight,
            out NormalizedRect normalized)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                normalized = default;
                return false;
            }

            var top = screenHeight - bottomLeft.yMax;
            var bottom = screenHeight - bottomLeft.yMin;
            return TryNormalizeIntersection(
                bottomLeft.xMin,
                top,
                bottomLeft.xMax,
                bottom,
                0f,
                0f,
                screenWidth,
                screenHeight,
                out normalized);
        }
    }

    /// <summary>
    /// Converts a normalized Unity-view rect into panel space.
    /// <paramref name="topLeftScreenToPanel"/> must expect top-left screen pixels, which is what
    /// <see cref="UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel"/> consumes after a bottom-left position has been flipped.
    /// </summary>
    internal static class PanelCoordinateConversion
    {
        public static PanelRect ToPanelRect(
            NormalizedRect normalized,
            float screenWidth,
            float screenHeight,
            Func<Vector2, Vector2> topLeftScreenToPanel)
        {
            var topLeft = topLeftScreenToPanel(new Vector2(normalized.X * screenWidth, normalized.Y * screenHeight));
            var bottomRight = topLeftScreenToPanel(new Vector2(
                (normalized.X + normalized.Width) * screenWidth,
                (normalized.Y + normalized.Height) * screenHeight));
            return new PanelRect(
                Mathf.Min(topLeft.x, bottomRight.x),
                Mathf.Min(topLeft.y, bottomRight.y),
                Mathf.Max(topLeft.x, bottomRight.x),
                Mathf.Max(topLeft.y, bottomRight.y));
        }
    }

    /// <summary>
    /// Absolute upward shift that puts the focused field just above a keyboard rectangle.
    /// The shift is always relative to the unshifted layout, so applying it again does not accumulate.
    /// </summary>
    internal static class KeyboardDisplacement
    {
        public const float ShiftEpsilon = 0.5f;

        public const string MaximumTranslationMessage =
            "Keyboard avoidance cannot fully reveal the focused field because the configured maximum translation has been reached.";

        public const string FieldTooTallMessage =
            "Keyboard avoidance cannot fully reveal the focused field because it is taller than the area above the keyboard.";

        public static KeyboardDisplacementResult Calculate(
            PanelRect field,
            PanelRect occlusion,
            float margin,
            float? maximumTranslation,
            float viewportTop)
        {
            if (!field.IsFinite() || !occlusion.IsFinite())
            {
                return KeyboardDisplacementResult.Unobstructed;
            }

            margin = Mathf.Max(0f, margin);

            // A floating keyboard only counts when it actually crosses the field.
            // A field that sits entirely below that rectangle is already clear of it.
            if (!field.OverlapsHorizontally(occlusion) || field.YMin >= occlusion.YMax)
            {
                return KeyboardDisplacementResult.Unobstructed;
            }

            var needed = field.YMax - occlusion.YMin + margin;
            if (needed < ShiftEpsilon)
            {
                return KeyboardDisplacementResult.Unobstructed;
            }

            var applied = needed;
            var hitLimit = false;
            if (maximumTranslation.HasValue)
            {
                var limit = Mathf.Max(0f, maximumTranslation.Value);
                if (applied > limit + ShiftEpsilon)
                {
                    applied = limit;
                    hitLimit = true;
                }
            }

            var available = occlusion.YMin - margin - viewportTop;
            var oversized = field.Height > available + ShiftEpsilon;
            if (!hitLimit && !oversized)
            {
                return KeyboardDisplacementResult.Avoiding(applied);
            }

            // Raising the translation cap cannot reveal a field that is taller than the open area.
            var message = oversized ? FieldTooTallMessage : MaximumTranslationMessage;
            return KeyboardDisplacementResult.Constrained(applied, message);
        }

        public static KeyboardDisplacementResult MostDemanding(
            KeyboardDisplacementResult current,
            bool hasCurrent,
            KeyboardDisplacementResult candidate)
        {
            if (!hasCurrent || candidate.UpwardShift > current.UpwardShift + ShiftEpsilon)
            {
                return candidate;
            }

            if (candidate.UpwardShift < current.UpwardShift - ShiftEpsilon)
            {
                return current;
            }

            if (candidate.Diagnostic != KeyboardAvoidanceDiagnosticCode.None
                && current.Diagnostic == KeyboardAvoidanceDiagnosticCode.None)
            {
                return candidate;
            }

            return current;
        }
    }
}
