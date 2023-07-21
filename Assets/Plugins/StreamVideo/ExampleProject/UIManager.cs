using System;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public class UIManager : MonoBehaviour
    {
        public event Action JoinClicked;

        protected void Awake()
        {
            _joinBtn.onClick.AddListener(() => JoinClicked?.Invoke());
        }
        
        [SerializeField] 
        private Button _joinBtn;
    }
}