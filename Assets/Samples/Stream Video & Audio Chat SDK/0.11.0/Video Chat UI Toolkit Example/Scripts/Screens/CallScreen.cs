using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UIToolkit.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Screens
{
    /// <summary>
    /// Active call: participant grid, call duration, and call controls.
    /// Shows "Joining..." until the call is started, then renders participants.
    /// </summary>
    internal sealed class CallScreen : ScreenBase
    {
        public CallScreen(VideoChatApp app, VisualElement root)
            : base(app, root)
        {
            _timerLabel = root.Q<Label>("timer-label");
            _joiningLabel = root.Q<Label>("joining-label");
            _tilesContainer = root.Q("tiles");
            _flipCameraButton = root.Q<Button>("flip-camera-button");

            _micButton = root.Q<Button>("mic-button");
            _micIcon = root.Q("mic-icon");
            _cameraButton = root.Q<Button>("camera-button");
            _cameraIcon = root.Q("camera-icon");

            _meetingLive = root.Q("meeting-live");
            _meetingLiveDetails = root.Q("meeting-live-details");
            _meetingLiveChevron = root.Q("meeting-live-chevron");
            _callIdLabel = root.Q<Label>("call-id-label");

            root.Q<Button>("leave-button").clicked += App.LeaveCall;
            root.Q<Button>("meeting-live-toggle").clicked += ToggleMeetingLiveDetails;
            root.Q<Button>("copy-call-id-button").clicked += () => GUIUtility.systemCopyBuffer = _callIdLabel.text;
            _flipCameraButton.clicked += App.SwitchCamera;
            _micButton.clicked += () => App.Client.AudioDeviceManager.SetEnabled(!App.Client.AudioDeviceManager.IsEnabled);
            _cameraButton.clicked += () => App.Client.VideoDeviceManager.SetEnabled(!App.Client.VideoDeviceManager.IsEnabled);
        }

        /// <summary>
        /// Show the screen while the call connection is being established. The call duration starts counting from now.
        /// </summary>
        public void ShowJoining(string callId)
        {
            Unbind();

            _callIdLabel.text = callId;
            _joiningLabel.RemoveFromClassList(HiddenClass);
            _meetingLive.AddToClassList(HiddenClass);
            SetMeetingLiveExpanded(false);
            _flipCameraButton.EnableInClassList(HiddenClass, !App.CanSwitchCamera());

            _callDuration.Restart();
            SetVisible();
        }

        public void Bind(IStreamCall call)
        {
            Unbind();

            _call = call;
            _callIdLabel.text = call.Id;
            _joiningLabel.AddToClassList(HiddenClass);

            foreach (var participant in call.Participants)
            {
                AddParticipant(participant);
            }

            _call.ParticipantJoined += OnParticipantJoined;
            _call.ParticipantLeft += OnParticipantLeft;
            _call.SortedParticipantsUpdated += OnSortedParticipantsUpdated;
        }

        protected override void OnHide()
        {
            Unbind();
            _callDuration.Stop();
        }

        protected override void OnUpdate()
        {
            _timerLabel.text = FormatDuration(_callDuration.Elapsed);

            var client = App.Client;
            CallControls.UpdateDeviceButton(_cameraButton, _cameraIcon, client.VideoDeviceManager.IsEnabled,
                "icon--videocam", "icon--videocam-off");
            CallControls.UpdateDeviceButton(_micButton, _micIcon, client.AudioDeviceManager.IsEnabled,
                "icon--mic", "icon--mic-off");

            if (_call == null)
            {
                return;
            }

            var isAlone = _tiles.Count <= 1;
            _meetingLive.EnableInClassList(HiddenClass, !isAlone);

            foreach (var tile in _tiles.Values)
            {
                tile.Update(_call, highlightSpeaker: !isAlone);
            }

            var stageSize = _tilesContainer.contentRect.size;
            if (_isLayoutDirty || stageSize != _lastStageSize)
            {
                _isLayoutDirty = false;
                _lastStageSize = stageSize;
                LayoutTiles(stageSize);
            }
        }

        private const float TileGap = 8;

        private readonly Label _timerLabel;
        private readonly Label _joiningLabel;
        private readonly VisualElement _tilesContainer;
        private readonly Button _flipCameraButton;
        private readonly Button _micButton;
        private readonly VisualElement _micIcon;
        private readonly Button _cameraButton;
        private readonly VisualElement _cameraIcon;
        private readonly VisualElement _meetingLive;
        private readonly VisualElement _meetingLiveDetails;
        private readonly VisualElement _meetingLiveChevron;
        private readonly Label _callIdLabel;

        // Keyed by session ID because a single user can join from multiple devices
        private readonly Dictionary<string, CallParticipantTile> _tiles = new Dictionary<string, CallParticipantTile>();
        private readonly Stopwatch _callDuration = new Stopwatch();

        private IStreamCall _call;
        private bool _isLayoutDirty;
        private bool _isMeetingLiveExpanded;
        private Vector2 _lastStageSize;

        private void Unbind()
        {
            if (_call != null)
            {
                _call.ParticipantJoined -= OnParticipantJoined;
                _call.ParticipantLeft -= OnParticipantLeft;
                _call.SortedParticipantsUpdated -= OnSortedParticipantsUpdated;
                _call = null;
            }

            foreach (var tile in _tiles.Values)
            {
                tile.Dispose();
            }

            _tiles.Clear();
        }

        private void OnParticipantJoined(IStreamVideoCallParticipant participant) => AddParticipant(participant);

        private void OnParticipantLeft(string sessionId, string userId)
        {
            if (_tiles.TryGetValue(sessionId, out var tile))
            {
                tile.Dispose();
                _tiles.Remove(sessionId);
                _isLayoutDirty = true;
            }
        }

        private void OnSortedParticipantsUpdated() => _isLayoutDirty = true;

        private void AddParticipant(IStreamVideoCallParticipant participant)
        {
            if (_tiles.ContainsKey(participant.SessionId))
            {
                return;
            }

            var root = App.ParticipantTileAsset.Instantiate();
            root.AddToClassList("call__tile");
            _tilesContainer.Add(root);

            _tiles.Add(participant.SessionId, new CallParticipantTile(participant, root, App));
            _isLayoutDirty = true;
        }

        /// <summary>
        /// Arrange tiles in a grid whose cells are as close to square as possible.
        /// This gives a vertical stack in portrait and a side-by-side layout in landscape.
        /// </summary>
        private void LayoutTiles(Vector2 area)
        {
            var count = _tiles.Count;
            if (count == 0 || !(area.x > 0) || !(area.y > 0))
            {
                return;
            }

            var columns = 1;
            var bestScore = float.MaxValue;
            for (var candidate = 1; candidate <= count; candidate++)
            {
                var rows = Mathf.CeilToInt(count / (float)candidate);
                var cellWidth = (area.x - TileGap * (candidate - 1)) / candidate;
                var cellHeight = (area.y - TileGap * (rows - 1)) / rows;
                if (cellWidth <= 0 || cellHeight <= 0)
                {
                    continue;
                }

                var emptyCells = rows * candidate - count;
                var score = Mathf.Abs(Mathf.Log(cellWidth / cellHeight)) + emptyCells * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    columns = candidate;
                }
            }

            var rowCount = Mathf.CeilToInt(count / (float)columns);
            var width = (area.x - TileGap * (columns - 1)) / columns;
            var height = (area.y - TileGap * (rowCount - 1)) / rowCount;

            var index = 0;
            foreach (var tile in GetOrderedTiles())
            {
                var row = index / columns;
                var column = index % columns;
                var itemsInRow = Mathf.Min(columns, count - row * columns);

                // Center the last row when it is not full
                var rowOffset = (area.x - (itemsInRow * width + TileGap * (itemsInRow - 1))) / 2f;

                var style = tile.Root.style;
                style.left = rowOffset + column * (width + TileGap);
                style.top = row * (height + TileGap);
                style.width = width;
                style.height = height;
                index++;
            }
        }

        /// <summary>
        /// Remote participants in the SDK's sorted order, local participant last
        /// </summary>
        private IEnumerable<CallParticipantTile> GetOrderedTiles()
        {
            var sorted = _call.SortedParticipants
                .Where(p => !p.IsLocalParticipant)
                .Select(p => _tiles.TryGetValue(p.SessionId, out var tile) ? tile : null)
                .Where(t => t != null)
                .ToList();

            foreach (var tile in _tiles.Values)
            {
                if (!sorted.Contains(tile) && !tile.Participant.IsLocalParticipant)
                {
                    sorted.Add(tile);
                }
            }

            sorted.AddRange(_tiles.Values.Where(t => t.Participant.IsLocalParticipant));
            return sorted;
        }

        private void ToggleMeetingLiveDetails() => SetMeetingLiveExpanded(!_isMeetingLiveExpanded);

        private void SetMeetingLiveExpanded(bool isExpanded)
        {
            _isMeetingLiveExpanded = isExpanded;
            _meetingLiveDetails.EnableInClassList(HiddenClass, !isExpanded);
            _meetingLiveChevron.EnableInClassList("icon--expand-more", !isExpanded);
            _meetingLiveChevron.EnableInClassList("icon--expand-less", isExpanded);
        }

        private static string FormatDuration(TimeSpan duration)
            => duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
