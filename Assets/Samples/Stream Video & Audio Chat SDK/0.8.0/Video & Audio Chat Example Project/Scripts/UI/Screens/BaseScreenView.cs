﻿using System;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Screens
{
    public abstract class BaseScreenView<TInitArgs> : MonoBehaviour
    {
        public void Init(StreamVideoManager streamVideoManager, UIManager uiManager)
        {
            VideoManager = streamVideoManager
                ? streamVideoManager
                : throw new ArgumentNullException(nameof(streamVideoManager));
            UIManager = uiManager ? uiManager : throw new ArgumentNullException(nameof(uiManager));
            
            _gameObject = gameObject;
            
            // Hide every screen view by default. The UIManager controls which screen should become visible
            _gameObject.SetActive(false);
            
            OnInit();
        }

        public void Show(TInitArgs initArgs)
        {
            if (_gameObject.activeSelf)
            {
                return;
            }

            _gameObject.SetActive(true);
            OnShow(initArgs);
        }

        public void Hide()
        {
            if (!_gameObject.activeSelf)
            {
                return;
            }

            _gameObject.SetActive(false);
            OnHide();
        }

        protected StreamVideoManager VideoManager { get; private set; }
        protected UIManager UIManager { get; private set; }

        protected abstract void OnInit();
        
        protected abstract void OnShow(TInitArgs initArgs);

        protected abstract void OnHide();

        private GameObject _gameObject;
    }
}