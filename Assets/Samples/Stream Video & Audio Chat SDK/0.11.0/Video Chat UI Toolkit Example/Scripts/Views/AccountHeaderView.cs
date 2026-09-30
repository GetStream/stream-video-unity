using System;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    /// <summary>
    /// Header with the connected user's avatar and name. This is sample-only UI, not part of the SDK.
    /// </summary>
    internal sealed class AccountHeaderView
    {
        public event Action CloseClicked;

        public AccountHeaderView(VisualElement root, bool showCloseButton)
        {
            _initials = root.Q<Label>("avatar-initials");
            _name = root.Q<Label>("user-name");

            var closeButton = root.Q<Button>("close-button");
            closeButton.EnableInClassList("hidden", !showCloseButton);
            closeButton.clicked += () => CloseClicked?.Invoke();
        }

        public void SetUser(string userName)
        {
            var text = string.IsNullOrEmpty(userName) ? "Connecting..." : userName;
            if (_name.text == text)
            {
                return;
            }

            _name.text = text;
            _initials.text = ParticipantTileView.GetInitials(userName);
        }

        private readonly Label _initials;
        private readonly Label _name;
    }
}
