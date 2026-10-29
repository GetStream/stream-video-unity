using System;
using StreamVideo.ExampleProject.UIToolkit.Views;
using StreamVideo.Libs.KeyboardAvoidance;
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

            // PointerDown focuses the button before clicked runs. Button derives from TextElement and
            // matches this field's keyboard settings, so UI Toolkit keeps the soft keyboard open and
            // drops the handle. Close it while this field still owns the keyboard.
            _joinButton.RegisterCallback<PointerDownEvent>(_ => DismissKeyboard(), TrickleDown.TrickleDown);
            _startNewCallButton.RegisterCallback<PointerDownEvent>(_ => DismissKeyboard(), TrickleDown.TrickleDown);

            _joinButton.clicked += OnJoinClicked;
            _startNewCallButton.clicked += OnStartNewCallClicked;

#if UNITY_2022_3_OR_NEWER
            // Unity's Android input strip is its own window above the keyboard, and Unity reports no geometry
            // for it, so it would cover this field. Typing straight into the field avoids that.
            _callIdInput.hideMobileInput = true;
#endif

            // This screen is the element allowed to move. It shifts only enough to uncover the focused field.
            _keyboardAvoidance = new KeyboardAvoidanceController(Root, Root);

            OnCallIdChanged();
        }

        public void Show()
        {
            SetVisible();
            SetError(null);
            _keyboardAvoidance.Enable();
        }

        internal void Dispose()
        {
            _keyboardAvoidance.Dispose();
        }

        protected override void OnHide()
        {
            // Enter submits without focusing a button, so the field can still own the keyboard here.
            DismissKeyboard();
            _keyboardAvoidance.Disable();
        }

        protected override void OnUpdate()
        {
            _header.SetUser(App.LocalUserName);
            SubmitOnKeyboardDone();
            _keyboardAvoidance.Tick();
        }

        private const string FocusedClass = "is-focused";
        private const string ErrorClass = "is-error";
        private const string DisabledClass = "is-disabled";
        private const string BusyClass = "is-busy";

        private readonly AccountHeaderView _header;
        private readonly VisualElement _callIdField;
        private readonly TextField _callIdInput;
        private readonly Label _placeholder;
        private readonly VisualElement _error;
        private readonly Label _errorLabel;
        private readonly Button _joinButton;
        private readonly Button _startNewCallButton;
        private readonly KeyboardAvoidanceController _keyboardAvoidance;

        private bool _keyboardDone;
        private bool _isBusy;

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

        // The soft keyboard's Done key sends no Return key event, so watch the keyboard status instead. Android
        // also reports Done after a tap outside closes the keyboard, so only a Done seen while the field still
        // owns the keyboard counts. Submit after UI Toolkit closes it, when the field holds the final text.
        private void SubmitOnKeyboardDone()
        {
            var keyboard = _callIdInput.touchScreenKeyboard;
            if (keyboard != null)
            {
                _keyboardDone = keyboard.status == TouchScreenKeyboard.Status.Done;
                return;
            }

            if (_keyboardDone)
            {
                _keyboardDone = false;
                OnJoinClicked();
            }
        }

        private async void OnJoinClicked()
        {
            DismissKeyboard();
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
            DismissKeyboard();
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

        private void DismissKeyboard()
        {
            var keyboard = _callIdInput.touchScreenKeyboard;
            if (keyboard != null)
            {
                keyboard.active = false;
            }

            _keyboardDone = false;
            _callIdInput.Blur();
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
