using System;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Stores call-scoped subscribers and forwards them onto a session-lifetime source
    /// only while attached. <see cref="Detach"/> on leave so handlers cannot fire for the next call.
    /// </summary>
    internal sealed class CallScopedControllerEvent<T>
    {
        public void Add(Action<T> handler)
        {
            _handlers += handler;
            if (_attached)
            {
                _controllerAdd(handler);
            }
        }

        public void Remove(Action<T> handler)
        {
            _handlers -= handler;
            if (_attached)
            {
                _controllerRemove(handler);
            }
        }

        public void Attach(Action<Action<T>> controllerAdd, Action<Action<T>> controllerRemove)
        {
            if (_attached || controllerAdd == null || controllerRemove == null)
            {
                return;
            }

            _controllerAdd = controllerAdd;
            _controllerRemove = controllerRemove;

            if (_handlers != null)
            {
                foreach (Action<T> handler in _handlers.GetInvocationList())
                {
                    controllerAdd(handler);
                }
            }

            _attached = true;
        }

        public void Detach()
        {
            if (!_attached)
            {
                return;
            }

            if (_handlers != null)
            {
                foreach (Action<T> handler in _handlers.GetInvocationList())
                {
                    _controllerRemove(handler);
                }
            }

            _attached = false;
            _controllerAdd = null;
            _controllerRemove = null;
        }

        private Action<T> _handlers;
        private Action<Action<T>> _controllerAdd;
        private Action<Action<T>> _controllerRemove;
        private bool _attached;
    }
}
