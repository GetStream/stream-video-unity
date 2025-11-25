using System;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a <see cref="IStreamCall"/> with provided ID is not found
    /// </summary>
    public class StreamCallNotFoundException : Exception
    {
        public StreamCallNotFoundException(string message) : base(message)
        {
            
        }
    }
}