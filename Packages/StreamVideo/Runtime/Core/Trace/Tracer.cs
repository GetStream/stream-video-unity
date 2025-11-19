using System;
using System.Collections.Generic;

namespace StreamVideo.Core.Trace
{
    /// <summary>
    /// An append-only, thread-safe trace buffer that can be snapshotted and rolled back.
    /// </summary>
    internal class Tracer
    {
        public string Id { get; }

        public bool IsEnabled => _enabled;

        public Tracer(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Enables or disables tracing.
        /// Switching state clears any existing buffered entries.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            lock (_buffer)
            {
                if (_enabled == enabled)
                {
                    return;
                }

                _enabled = enabled;
                _buffer.Clear();
            }
        }

        /// <summary>
        /// Records a trace entry when tracing is enabled.
        /// </summary>
        public void Trace(string tag, object data = null)
        {
            lock (_buffer)
            {
                if (!_enabled)
                {
                    return;
                }

                _buffer.Add(new TraceRecord(
                    tag: tag,
                    id: Id,
                    data: data,
                    timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        /// <summary>
        /// Returns a snapshot of the current buffer and clears it.
        /// </summary>
        public TraceSlice Take()
        {
            lock (_buffer)
            {
                //StreamTODO: optimize this to re-use the same buffers for stats generation
                var snapshot = new List<TraceRecord>(_buffer);
                _buffer.Clear();

                return new TraceSlice(
                    snapshot: snapshot,
                    rollback: () =>
                    {
                        lock (_buffer)
                        {
                            _buffer.InsertRange(0, snapshot);
                        }
                    }
                );
            }
        }

        /// <summary>
        /// Permanently discards all buffered trace entries.
        /// </summary>
        public void Dispose()
        {
            lock (_buffer)
            {
                _buffer.Clear();
            }
        }

        private readonly List<TraceRecord> _buffer = new List<TraceRecord>();
        private bool _enabled = true;
    }

    /// <summary>
    /// Immutable view of a drained trace buffer together with a rollback action.
    /// </summary>
    internal struct TraceSlice
    {
        public List<TraceRecord> Snapshot;
        public Action Rollback;

        public TraceSlice(List<TraceRecord> snapshot, Action rollback)
        {
            Snapshot = snapshot;
            Rollback = rollback;
        }
    }
}

