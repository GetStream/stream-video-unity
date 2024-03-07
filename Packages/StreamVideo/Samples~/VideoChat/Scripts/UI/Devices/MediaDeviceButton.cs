using System;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MediaDeviceButton : MonoBehaviour
    {
        public event Action Clicked;
        
        public void Init(Sprite onSprite, Sprite offSprite)
        {
            _onSprite = onSprite;
            _offSprite = offSprite;
        }
        
        public void UpdateSprite(bool isActive)
        {
            var sprite = isActive ? _onSprite : _offSprite;
            _image.sprite = sprite;
        }

        // Called by Unity
        protected void Awake()
        {
            _image = _button.GetComponent<Image>();
            _button.onClick.AddListener(() => Clicked?.Invoke());
        }
        
        [SerializeField]
        private Button _button;

        private Sprite _onSprite;
        private Sprite _offSprite;
        private Image _image;
    }
}