using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Screens;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI
{
    public class UIScreensSet : MonoBehaviour
    {
        public void Init(StreamVideoManager videoManager, UIManager uiManager)
        {
            _mainScreen.Init(videoManager, uiManager);
            _callScreen.Init(videoManager, uiManager);
        }
        
        public void ShowMainScreen()
        {
            _callScreen.Hide();
            _mainScreen.Show();
        }
        
        public void ShowCallScreen(IStreamCall call)
        {
            _mainScreen.Hide();
            _callScreen.Show(new CallScreenView.ShowArgs(call));
        }
        
        [SerializeField]
        private CallScreenView _callScreen;

        [SerializeField]
        private MainScreenView _mainScreen;
    }
}