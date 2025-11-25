using System;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when trying to join a <see cref="IStreamCall"/> but another <see cref="IStreamCall"/> is already joined or currently joining
    /// </summary>
    public class StreamCallInProgressException : Exception
    {
        public StreamCallInProgressException(string message) : base(message)
        {
        }
    }
}