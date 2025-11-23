using System;
using System.Text;

namespace StreamVideo.Core.Utils
{
    internal readonly struct StringBuilderPoolScope : IDisposable
    {
        public StringBuilderPoolScope(out StringBuilder sb)
        {
            _sb = StringBuilderPool.Rent();
            sb = _sb;
        }

        public void Dispose()
        {
            if (_sb != null)
            {
                StringBuilderPool.Release(_sb);
            }
        }

        private readonly StringBuilder _sb;
    }
}