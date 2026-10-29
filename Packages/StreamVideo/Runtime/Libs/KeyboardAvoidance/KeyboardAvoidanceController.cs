using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
// Device.Screen follows the simulated device in the Device Simulator.
using Screen = UnityEngine.Device.Screen;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    /// <summary>
    /// Settings for <see cref="KeyboardAvoidanceController"/>.
    /// Distances are UI Toolkit panel units.
    /// </summary>
    public sealed class KeyboardAvoidanceSettings
    {
        public float BottomMargin = 12f;
        public float? MaximumTranslation;
        public bool DebugLogging;
        public Func<TextField, bool> IgnoreInput;
        public bool RestoreOnFocusLoss = true;
    }

    /// <summary>
    /// Keeps the focused text field above the software keyboard by translating one chosen element
    /// the minimum distance. The element is explicit: this does not move the document root.
    /// Call <see cref="Tick"/> from Update while the controller is enabled.
    /// </summary>
    public sealed class KeyboardAvoidanceController : IDisposable
    {
        public KeyboardAvoidanceController(VisualElement focusRoot, VisualElement movable,
            KeyboardAvoidanceSettings settings = null)
        {
            _focusRoot = focusRoot;
            _movable = movable;
            _settings = settings ?? new KeyboardAvoidanceSettings();
            _onFocusIn = _ => MarkDirty();
            _onFocusOut = _ => MarkDirty();
            _onGeometry = _ => MarkDirty();
        }

        public bool IsEnabled => _wantsEnabled && !_disposed;
        public float CurrentShift => _appliedShift;
        public TextField CurrentFocusedElement => _focused;
        public KeyboardAvoidanceStatus CurrentStatus => _status;

        public event Action<float> AvoidanceChanged;
        public event Action<KeyboardAvoidanceDiagnosticCode, string> AvoidanceFailed;

        public void Enable()
        {
            if (_disposed || _wantsEnabled)
            {
                return;
            }

            _wantsEnabled = true;
            _dirty = true;
            TryAttach();
        }

        public void Disable()
        {
            if (_disposed || !_wantsEnabled)
            {
                return;
            }

            _wantsEnabled = false;
            DetachCallbacks();
            _attached = false;
            _focused = null;
            CommitShift(0f);
            SetStatus(KeyboardAvoidanceStatus.Disabled);
        }

        public void Refresh()
        {
            if (!_disposed)
            {
                _dirty = true;
            }
        }

        public void Tick()
        {
            if (_disposed || !_wantsEnabled)
            {
                return;
            }

#if !UNITY_EDITOR
            if (!Application.isFocused)
            {
                if (_status != KeyboardAvoidanceStatus.Suspended)
                {
                    KeyboardAvoidanceDebug.Log("ctrl", "suspended: application lost focus");
                    CommitShift(0f);
                    SetStatus(KeyboardAvoidanceStatus.Suspended);
                    _resumePending = true;
                }

                return;
            }
#endif

            if (!TryAttach())
            {
                return;
            }

#if !UNITY_EDITOR
            if (_resumePending)
            {
                _resumePending = false;
                _service.Reacquire();
                _dirty = true;
            }
#endif

            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != _screenSize)
            {
                _screenSize = screen;
                _service.Reacquire();
                _dirty = true;
            }

            var field = ReadFocusedField();
            if (field != _focused)
            {
                KeyboardAvoidanceDebug.Log("ctrl.focus", field != null ? "field=" + field.name : "none");
                _focused = field;
                _reportedDiagnostic = KeyboardAvoidanceDiagnosticCode.None;
                TrackField(field);
                _dirty = true;
            }

            if (_focused != null || _appliedShift > 0f || _watchKeyboard)
            {
                _service.Poll();
            }

            if (_service.Revision != _seenRevision)
            {
                _seenRevision = _service.Revision;
                _dirty = true;
            }

            if (!_dirty)
            {
                return;
            }

            _dirty = false;
            Recalculate();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _wantsEnabled = false;
            DetachCallbacks();
            _attached = false;
            _focused = null;
            RestoreTranslate();
            ReleaseOwnership();
            if (_serviceAcquired)
            {
                _service.Dispose();
                _service = null;
                _serviceAcquired = false;
            }

            _status = KeyboardAvoidanceStatus.Disposed;
        }

        private bool TryAttach()
        {
            if (_attached)
            {
                return true;
            }

            if (_movable == null)
            {
                Report(KeyboardAvoidanceDiagnosticCode.MovableContainerMissing,
                    "Keyboard avoidance needs the element it is allowed to move.");
                SetStatus(KeyboardAvoidanceStatus.Disabled);
                return false;
            }

            if (_focusRoot == null || _focusRoot.panel == null)
            {
                SetStatus(KeyboardAvoidanceStatus.WaitingForPanel);
                return false;
            }

            if (_movable.panel != null && _movable.panel != _focusRoot.panel)
            {
                Report(KeyboardAvoidanceDiagnosticCode.InvalidHierarchy,
                    "The element keyboard avoidance moves is not in the same panel as the UI it watches.");
                return false;
            }

            if (!_capturedTranslate)
            {
                if (HasForeignTranslate())
                {
                    Report(KeyboardAvoidanceDiagnosticCode.TransformConflict,
                        "Keyboard avoidance cannot translate this element because its translation is already in use.");
                    return false;
                }

                _originalTranslate = _movable.style.translate;
                _capturedTranslate = true;
            }

            if (!TryOwn())
            {
                return false;
            }

            _focusRoot.RegisterCallback(_onFocusIn);
            _focusRoot.RegisterCallback(_onFocusOut);
            _focusRoot.RegisterCallback(_onGeometry);
            if (_movable != _focusRoot)
            {
                _movable.RegisterCallback(_onGeometry);
            }

            _callbacksRegistered = true;

            if (!_serviceAcquired)
            {
                _service = KeyboardGeometryService.Acquire();
                _serviceAcquired = true;
            }

            _attached = true;
            _dirty = true;
            return true;
        }

        private void Recalculate()
        {
            var snapshot = _service.Current;
            _watchKeyboard = snapshot.State == KeyboardVisibility.Visible
                             || snapshot.State == KeyboardVisibility.Transitioning
                             || snapshot.State == KeyboardVisibility.Unknown
                             || !snapshot.IsValid;

            if (_focused == null)
            {
                var keep = !_settings.RestoreOnFocusLoss
                           && _appliedShift > 0f
                           && snapshot.IsValid
                           && snapshot.State == KeyboardVisibility.Visible;
                if (!keep)
                {
                    RestoreAfterPointerRelease();
                }

                KeyboardAvoidanceDebug.Log("ctrl", "no focused field, keep=" + keep + " "
                                                   + KeyboardAvoidanceDebug.Describe(snapshot));
                return;
            }

            if (!snapshot.IsValid || snapshot.State == KeyboardVisibility.Unknown)
            {
                KeyboardAvoidanceDebug.Log("ctrl", "waiting for geometry field=" + _focused.name + " "
                                                   + KeyboardAvoidanceDebug.Describe(snapshot)
                                                   + " failure=" + _service.MeasurementFailure);
                SetStatus(KeyboardAvoidanceStatus.WaitingForGeometry);
                if (!string.IsNullOrEmpty(_service.MeasurementFailure))
                {
                    Report(KeyboardAvoidanceDiagnosticCode.KeyboardGeometryUnknown, _service.MeasurementFailure);
                }

                return;
            }

            if (snapshot.State == KeyboardVisibility.Hidden)
            {
                KeyboardAvoidanceDebug.Log("ctrl", "keyboard hidden field=" + _focused.name);
                RestoreAfterPointerRelease();
                return;
            }

            if (snapshot.State == KeyboardVisibility.Transitioning && snapshot.OcclusionCount == 0)
            {
                SetStatus(KeyboardAvoidanceStatus.WaitingForGeometry);
                return;
            }

            if (Screen.width <= 0 || Screen.height <= 0 || _focused.worldBound.height <= 0f)
            {
                _dirty = true;
                return;
            }

            if (!LayoutCaughtUp())
            {
                return;
            }

            var measured = PanelRect.FromRect(_focused.worldBound);

            var baseline = measured.UnshiftedUp(_appliedShift);
            var viewportTop = _focusRoot.panel.visualTree.worldBound.yMin;
            var panel = _focusRoot.panel;
            var best = default(KeyboardDisplacementResult);
            var found = false;
            for (var i = 0; i < snapshot.OcclusionCount; i++)
            {
                var normalized = snapshot.Occlusion(i);
                if (normalized.IsEmpty)
                {
                    continue;
                }

                // Normalized fractions map onto Unity screen pixels. ScreenToPanel expects that
                // top-left pixel space and converts it to panel units for the active scale mode.
                var occlusion = PanelCoordinateConversion.ToPanelRect(
                    normalized,
                    Screen.width,
                    Screen.height,
                    point => RuntimePanelUtils.ScreenToPanel(panel, point));
                var result = KeyboardDisplacement.Calculate(
                    baseline,
                    occlusion,
                    _settings.BottomMargin,
                    _settings.MaximumTranslation,
                    viewportTop);
                KeyboardAvoidanceDebug.Log("ctrl.occlusion" + i,
                    KeyboardAvoidanceDebug.Describe(normalized) + " -> panel " + KeyboardAvoidanceDebug.Describe(occlusion)
                    + " -> shift=" + KeyboardAvoidanceDebug.F(result.UpwardShift) + " " + result.Status);
                best = KeyboardDisplacement.MostDemanding(best, found, result);
                found = true;
            }

            KeyboardAvoidanceDebug.Log("ctrl",
                "field=" + _focused.name + " measured=" + KeyboardAvoidanceDebug.Describe(measured)
                + " baseline=" + KeyboardAvoidanceDebug.Describe(baseline)
                + " applied=" + KeyboardAvoidanceDebug.F(_appliedShift)
                + " panel=" + KeyboardAvoidanceDebug.Describe(panel.visualTree.worldBound)
                + " viewportTop=" + KeyboardAvoidanceDebug.F(viewportTop)
                + " screen=" + Screen.width + "x" + Screen.height
                + " -> " + (found ? "shift=" + KeyboardAvoidanceDebug.F(best.UpwardShift) + " " + best.Status : "no intersection")
                + " | " + KeyboardAvoidanceDebug.Describe(snapshot));

            if (!found)
            {
                // A visible keyboard that does not intersect the Unity view leaves the field where it is.
                CommitShift(0f);
                SetStatus(KeyboardAvoidanceStatus.VisibleWithoutAdjustment);
                return;
            }

            CommitShift(best.UpwardShift);
            SetStatus(best.Status);
            if (best.Diagnostic != KeyboardAvoidanceDiagnosticCode.None)
            {
                Report(best.Diagnostic, best.Message);
            }
        }

        private void RestoreAfterPointerRelease()
        {
            // UI Toolkit hit-tests the release again, so moving a pressed button away from the pointer cancels its click.
            var waitForRelease = _appliedShift > 0f && IsPointerCaptured();
            KeyboardAvoidanceDebug.Log("ctrl.restore", waitForRelease ? "waiting for pointer release" : "now");
            if (waitForRelease)
            {
                _dirty = true;
                return;
            }

            CommitShift(0f);
            SetStatus(KeyboardAvoidanceStatus.Idle);
        }

        private bool IsPointerCaptured()
        {
            var panel = _focusRoot.panel;
            if (panel == null)
            {
                return false;
            }

            for (var pointerId = 0; pointerId < PointerId.maxPointers; pointerId++)
            {
                if (panel.GetCapturingElement(pointerId) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private TextField ReadFocusedField()
        {
            var focused = _focusRoot.panel?.focusController?.focusedElement as VisualElement;
            var field = TextFieldFocus.FindTextField(focused);
            if (field != null && _focusRoot.Contains(field) && !_movable.Contains(field))
            {
                Report(KeyboardAvoidanceDiagnosticCode.InvalidHierarchy,
                    "The focused field is not inside the element keyboard avoidance is allowed to move.");
                return null;
            }

            return TextFieldFocus.IsEligible(field, _focusRoot, _movable, _settings.IgnoreInput) ? field : null;
        }

        private bool LayoutCaughtUp()
        {
            if (!_layoutPending)
            {
                return true;
            }

            // The translation is applied after this Update, so worldBound includes it on the next frame.
            if (UnityEngine.Time.frameCount <= _appliedFrame)
            {
                _dirty = true;
                return false;
            }

            _layoutPending = false;
            return true;
        }

        private void CommitShift(float shift)
        {
            if (!ApplyShift(shift))
            {
                return;
            }

            _appliedFrame = UnityEngine.Time.frameCount;
            _layoutPending = true;
            _dirty = true;
        }

        private bool ApplyShift(float shift)
        {
            if (shift < 0f)
            {
                shift = 0f;
            }

            if (Mathf.Abs(shift - _appliedShift) < KeyboardDisplacement.ShiftEpsilon)
            {
                return false;
            }

            _appliedShift = shift;
            if (_movable == null)
            {
                return false;
            }

            if (shift <= 0f)
            {
                if (_hasApplied)
                {
                    _movable.style.translate = _originalTranslate;
                }

                _hasApplied = false;
            }
            else
            {
                _movable.style.translate = new Translate(0f, -shift, 0f);
                _hasApplied = true;
            }

            if (_settings.DebugLogging)
            {
                Debug.Log("Keyboard avoidance moved the focused field by " + shift.ToString("0.#") + " panel units.");
            }

            AvoidanceChanged?.Invoke(shift);
            return true;
        }

        private void RestoreTranslate()
        {
            _appliedShift = 0f;
            _layoutPending = false;
            _appliedFrame = 0;
            if (!_hasApplied || _movable == null)
            {
                _hasApplied = false;
                return;
            }

            try
            {
                _movable.style.translate = _originalTranslate;
            }
            catch (Exception)
            {
                // The element can already be detached during teardown.
            }

            _hasApplied = false;
        }

        private bool HasForeignTranslate()
        {
            var translate = _movable.resolvedStyle.translate;
            return Mathf.Abs(translate.x) > KeyboardDisplacement.ShiftEpsilon
                   || Mathf.Abs(translate.y) > KeyboardDisplacement.ShiftEpsilon;
        }

        private bool TryOwn()
        {
            if (_owns)
            {
                return true;
            }

            if (!_owners.Add(_movable))
            {
                Report(KeyboardAvoidanceDiagnosticCode.MultipleOwners,
                    "Keyboard avoidance is already moving this element from another controller.");
                return false;
            }

            _owns = true;
            return true;
        }

        private void ReleaseOwnership()
        {
            if (!_owns || _movable == null)
            {
                _owns = false;
                return;
            }

            _owners.Remove(_movable);
            _owns = false;
        }

        private void DetachCallbacks()
        {
            TrackField(null);
            if (_focusRoot == null || !_callbacksRegistered)
            {
                _callbacksRegistered = false;
                return;
            }

            _focusRoot.UnregisterCallback(_onFocusIn);
            _focusRoot.UnregisterCallback(_onFocusOut);
            _focusRoot.UnregisterCallback(_onGeometry);
            if (_movable != null && _movable != _focusRoot)
            {
                _movable.UnregisterCallback(_onGeometry);
            }

            _callbacksRegistered = false;
        }

        private void TrackField(TextField field)
        {
            if (_trackedField == field)
            {
                return;
            }

            if (_trackedField != null)
            {
                _trackedField.UnregisterCallback(_onGeometry);
            }

            _trackedField = field;
            if (_trackedField != null)
            {
                _trackedField.RegisterCallback(_onGeometry);
            }
        }

        private void MarkDirty()
        {
            if (!_disposed)
            {
                _dirty = true;
            }
        }

        private void SetStatus(KeyboardAvoidanceStatus status)
        {
            if (status == KeyboardAvoidanceStatus.Idle
                || status == KeyboardAvoidanceStatus.Avoiding
                || status == KeyboardAvoidanceStatus.VisibleWithoutAdjustment
                || status == KeyboardAvoidanceStatus.Disabled
                || status == KeyboardAvoidanceStatus.Suspended)
            {
                _reportedDiagnostic = KeyboardAvoidanceDiagnosticCode.None;
            }

            _status = status;
        }

        private void Report(KeyboardAvoidanceDiagnosticCode code, string message)
        {
            if (code == KeyboardAvoidanceDiagnosticCode.None || _reportedDiagnostic == code)
            {
                return;
            }

            _reportedDiagnostic = code;
            Debug.LogWarning(message);
            AvoidanceFailed?.Invoke(code, message);
        }

        private static readonly HashSet<VisualElement> _owners = new HashSet<VisualElement>();

        private readonly VisualElement _focusRoot;
        private readonly VisualElement _movable;
        private readonly KeyboardAvoidanceSettings _settings;
        private readonly EventCallback<FocusInEvent> _onFocusIn;
        private readonly EventCallback<FocusOutEvent> _onFocusOut;
        private readonly EventCallback<GeometryChangedEvent> _onGeometry;

        private KeyboardGeometryService _service;
        private TextField _focused;
        private TextField _trackedField;
        private StyleTranslate _originalTranslate;
        private KeyboardAvoidanceStatus _status = KeyboardAvoidanceStatus.Disabled;
        private KeyboardAvoidanceDiagnosticCode _reportedDiagnostic;
        private Vector2Int _screenSize;
        private float _appliedShift;
        private int _appliedFrame;
        private int _seenRevision;
        private bool _wantsEnabled;
        private bool _dirty;
        private bool _attached;
        private bool _callbacksRegistered;
        private bool _serviceAcquired;
        private bool _owns;
        private bool _capturedTranslate;
        private bool _hasApplied;
        private bool _watchKeyboard;
        private bool _layoutPending;
        private bool _resumePending;
        private bool _disposed;
    }
}
