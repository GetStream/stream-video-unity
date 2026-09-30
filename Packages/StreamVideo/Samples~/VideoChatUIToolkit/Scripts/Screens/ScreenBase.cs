using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Screens
{
    internal abstract class ScreenBase
    {
        public bool IsVisible { get; private set; }

        public void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            IsVisible = false;
            Root.AddToClassList(HiddenClass);
            OnHide();
        }

        public void Update()
        {
            if (IsVisible)
            {
                OnUpdate();
            }
        }

        protected const string HiddenClass = "hidden";

        protected VideoChatApp App { get; }
        protected VisualElement Root { get; }

        protected ScreenBase(VideoChatApp app, VisualElement root)
        {
            App = app;
            Root = root;
            Root.AddToClassList(HiddenClass);
        }

        protected void SetVisible()
        {
            if (IsVisible)
            {
                return;
            }

            IsVisible = true;
            Root.RemoveFromClassList(HiddenClass);
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnUpdate()
        {
        }
    }
}
