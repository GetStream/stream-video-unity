using System;
using StreamVideo.Core.StatefulModels;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public class UIManager : MonoBehaviour
    {
        public event Action JoinClicked;

        public void AddParticipant(IStreamVideoCallParticipant participant)
        {
            var view = Instantiate(_participantViewPrefab, _participantsContainer);
            view.Init(participant);
        }

        protected void Awake()
        {
            _joinBtn.onClick.AddListener(() => JoinClicked?.Invoke());
        }
        
        [SerializeField] 
        private Button _joinBtn;

        [SerializeField]
        private Transform _participantsContainer;

        [SerializeField]
        private ParticipantView _participantViewPrefab;
    }
}