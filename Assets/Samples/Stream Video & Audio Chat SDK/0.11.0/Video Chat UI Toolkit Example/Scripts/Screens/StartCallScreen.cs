using System;
using StreamVideo.ExampleProject.UIToolkit.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Screens
{
    /// <summary>
    /// Entry screen: start a new call or join an existing one by call ID. Both paths lead to the lobby.
    /// </summary>
    internal sealed class StartCallScreen : ScreenBase
    {
        public StartCallScreen(VideoChatApp app, VisualElement root)
            : base(app, root)
        {
            _header = new AccountHeaderView(root.Q("header"), showCloseButton: false);

            _callIdField = root.Q("call-id-field");
            _callIdInput = root.Q<TextField>("call-id-input");
            _placeholder = root.Q<Label>("call-id-placeholder");
            _error = root.Q("call-id-error");
            _errorLabel = root.Q<Label>("call-id-error-label");
            _joinButton = root.Q<Button>("join-button");
            _startNewCallButton = root.Q<Button>("start-new-call-button");

            _callIdInput.RegisterValueChangedCallback(_ => OnCallIdChanged());
            _callIdInput.RegisterCallback<FocusInEvent>(_ => _callIdField.AddToClassList(FocusedClass));
            _callIdInput.RegisterCallback<FocusOutEvent>(_ => _callIdField.RemoveFromClassList(FocusedClass));
            _callIdInput.RegisterCallback<KeyDownEvent>(OnCallIdKeyDown, TrickleDown.TrickleDown);

            _joinButton.clicked += OnJoinClicked;
            _startNewCallButton.clicked += OnStartNewCallClicked;

            OnCallIdChanged();
        }

        public void Show()
        {
            SetVisible();
            SetError(null);
        }

        protected override void OnHide()
        {
            // Leaving the field open keeps the keyboard up and the next screen inset.
            _callIdInput.Blur();
            ResetKeyboardShift();
        }

        protected override void OnUpdate()
        {
            _header.SetUser(App.LocalUserName);
            KeepCallIdAboveKeyboard();
        }

        private const string FocusedClass = "is-focused";
        private const string ErrorClass = "is-error";
        private const string DisabledClass = "is-disabled";
        private const string BusyClass = "is-busy";
        private const float KeyboardClearance = 12f;

        private readonly AccountHeaderView _header;
        private readonly VisualElement _callIdField;
        private readonly TextField _callIdInput;
        private readonly Label _placeholder;
        private readonly VisualElement _error;
        private readonly Label _errorLabel;
        private readonly Button _joinButton;
        private readonly Button _startNewCallButton;

        private bool _isBusy;
        private float _keyboardShift;
        private float _keyboardLastFrame;

        private string CallId => _callIdInput.value?.Trim() ?? string.Empty;

        private void OnCallIdChanged()
        {
            var isEmpty = string.IsNullOrEmpty(_callIdInput.value);
            _placeholder.EnableInClassList(HiddenClass, !isEmpty);
            _joinButton.EnableInClassList(DisabledClass, CallId.Length == 0);

            // The error clears on the next edit
            SetError(null);
        }

        private void OnCallIdKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OnJoinClicked();
            }
        }

        private async void OnJoinClicked()
        {
            var callId = CallId;
            if (_isBusy || callId.Length == 0 || !App.IsConnected)
            {
                return;
            }

            SetBusy(_joinButton, true);
            try
            {
                if (await App.CallExistsAsync(callId))
                {
                    App.ShowLobby(callId, isNewCall: false);
                }
                else
                {
                    SetError("Incorrect Call ID. Try again.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetError("Something went wrong. Try again.");
            }
            finally
            {
                SetBusy(_joinButton, false);
            }
        }

        private async void OnStartNewCallClicked()
        {
            if (_isBusy || !App.IsConnected)
            {
                return;
            }

            SetBusy(_startNewCallButton, true);
            try
            {
                var callId = await App.CreateCallIdAsync();
                App.ShowLobby(callId, isNewCall: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                SetBusy(_startNewCallButton, false);
            }
        }

        /// <summary>
        /// The bottom inset recenters this screen in the space above the keyboard.
        /// When the centered content is taller than that space, slide the screen up until the call ID field is visible.
        /// </summary>
        private void KeepCallIdAboveKeyboard()
        {
            var keyboard = MobileKeyboard.CoverageFromBottom();
            if (keyboard <= 0f || Root.parent == null || _callIdField.layout.height <= 0f)
            {
                _keyboardLastFrame = 0f;
                ResetKeyboardShift();
                return;
            }

            // ScreenAdapter applies the inset during this Update, but the layout still reflects the previous frame.
            var layoutIncludesKeyboard = _keyboardLastFrame > 0f;
            _keyboardLastFrame = keyboard;
            if (!layoutIncludesKeyboard)
            {
                return;
            }

            // worldBound includes the shift from earlier frames. Adding it back is the centered position.
            var visibleBottom = Root.parent.worldBound.yMax - KeyboardClearance;
            var restingFieldBottom = _callIdField.worldBound.yMax + _keyboardShift;
            var needed = Mathf.Max(0f, restingFieldBottom - visibleBottom);

            var visibleTop = Root.parent.worldBound.yMin + KeyboardClearance;
            var restingFieldTop = _callIdField.worldBound.yMin + _keyboardShift;
            if (restingFieldTop - needed < visibleTop)
            {
                needed = Mathf.Max(0f, restingFieldTop - visibleTop);
            }

            if (Mathf.Abs(needed - _keyboardShift) < 0.5f)
            {
                return;
            }

            _keyboardShift = needed;
            Root.style.translate = new Translate(0f, -needed, 0f);
        }

        private void ResetKeyboardShift()
        {
            if (_keyboardShift <= 0f)
            {
                return;
            }

            _keyboardShift = 0f;
            Root.style.translate = new Translate(0f, 0f, 0f);
        }

        private void SetBusy(VisualElement button, bool isBusy)
        {
            _isBusy = isBusy;
            button.EnableInClassList(BusyClass, isBusy);
        }

        private void SetError(string message)
        {
            var hasError = !string.IsNullOrEmpty(message);
            _callIdField.EnableInClassList(ErrorClass, hasError);
            _error.EnableInClassList(HiddenClass, !hasError);
            if (hasError)
            {
                _errorLabel.text = message;
            }
        }
    }
}
