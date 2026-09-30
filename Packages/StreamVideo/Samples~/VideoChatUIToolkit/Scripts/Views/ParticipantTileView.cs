using StreamVideo.Core.Models.Sfu;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    /// <summary>
    /// View over the ParticipantTile.uxml template. Used for call participants and for the lobby camera preview.
    /// </summary>
    internal sealed class ParticipantTileView
    {
        public VisualElement Root { get; }

        public ParticipantTileView(VisualElement root)
        {
            Root = root;
            _tile = root.Q("participant-tile");
            _video = new VideoSurface(root.Q("video"));
            _placeholder = root.Q("placeholder");
            _initials = root.Q<Label>("initials");
            _name = root.Q<Label>("name");
            _qualityBadge = root.Q("quality-badge");
        }

        public void SetName(string name)
        {
            name = name ?? string.Empty;
            if (_name.text == name)
            {
                return;
            }

            _name.text = name;
            _initials.text = GetInitials(name);
        }

        /// <param name="texture">Video texture. Placeholder is shown when null or when video is disabled</param>
        /// <param name="rotationAngle">Clockwise rotation required to display the video upright</param>
        /// <param name="isVideoEnabled">Is video being published</param>
        public void SetVideo(Texture texture, int rotationAngle, bool isVideoEnabled)
        {
            var showVideo = isVideoEnabled && texture != null;
            _video.SetSource(showVideo ? texture : null, rotationAngle);
            _placeholder.EnableInClassList(HiddenClass, showVideo);
        }

        public void SetAudio(bool isAudioEnabled, bool isSpeaking)
        {
            _tile.EnableInClassList(MutedClass, !isAudioEnabled);
            _tile.EnableInClassList(SpeakingClass, isAudioEnabled && isSpeaking);
        }

        public void SetHighlighted(bool isHighlighted) => _tile.EnableInClassList(HighlightedClass, isHighlighted);

        /// <param name="quality">Connection quality or null to hide the indicator</param>
        public void SetConnectionQuality(ConnectionQuality? quality)
        {
            var isKnown = quality.HasValue && quality.Value != ConnectionQuality.Unspecified;
            _qualityBadge.EnableInClassList(HiddenClass, !isKnown);
            _qualityBadge.EnableInClassList(QualityGoodClass, quality == ConnectionQuality.Good);
            _qualityBadge.EnableInClassList(QualityPoorClass, quality == ConnectionQuality.Poor);
        }

        private const string HiddenClass = "hidden";
        private const string MutedClass = "participant-tile--muted";
        private const string SpeakingClass = "participant-tile--speaking";
        private const string HighlightedClass = "participant-tile--highlighted";
        private const string QualityGoodClass = "participant-tile__quality--good";
        private const string QualityPoorClass = "participant-tile__quality--poor";

        private readonly VisualElement _tile;
        private readonly VideoSurface _video;
        private readonly VisualElement _placeholder;
        private readonly Label _initials;
        private readonly Label _name;
        private readonly VisualElement _qualityBadge;

        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var parts = name.Trim().Split(' ');
            var initials = parts[0].Substring(0, 1);
            if (parts.Length > 1 && parts[parts.Length - 1].Length > 0)
            {
                initials += parts[parts.Length - 1].Substring(0, 1);
            }

            return initials.ToUpperInvariant();
        }
    }
}
