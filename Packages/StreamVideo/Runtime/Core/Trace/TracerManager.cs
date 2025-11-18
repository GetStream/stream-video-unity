using System.Collections.Generic;
using System.Linq;

namespace StreamVideo.Core.Trace
{
    /// <summary>
    /// Factory that provides Tracer instances.
    /// </summary>
    internal class TracerManager
    {
        public TracerManager(bool enabled = true)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Returns all tracers created by this factory.
        /// </summary>
        public List<Tracer> GetTracers()
        {
            lock (_tracers)
            {
                return _tracers.Values.ToList();
            }
        }

        /// <summary>
        /// Returns a Tracer for the given name.
        /// </summary>
        public Tracer GetTracer(string name)
        {
            lock (_tracers)
            {
                if (!_tracers.TryGetValue(name, out var tracer))
                {
                    tracer = new Tracer(name);
                    tracer.SetEnabled(_enabled);
                    _tracers[name] = tracer;
                }

                return tracer;
            }
        }

        /// <summary>
        /// Clears all tracers.
        /// </summary>
        public void Clear()
        {
            lock (_tracers)
            {
                foreach (var tracer in _tracers.Values)
                {
                    tracer.Dispose();
                }

                _tracers.Clear();
            }
        }

        /// <summary>
        /// Enables or disables tracing.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            lock (_tracers)
            {
                _enabled = enabled;
                foreach (var tracer in _tracers.Values)
                {
                    tracer.SetEnabled(enabled);
                }
            }
        }

        private readonly Dictionary<string, Tracer> _tracers = new Dictionary<string, Tracer>();
        private bool _enabled;
    }
}

