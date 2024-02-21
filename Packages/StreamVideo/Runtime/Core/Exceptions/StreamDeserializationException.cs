using System;

namespace StreamVideo.Core.Exceptions
{
    /// <summary>
    /// Thrown when deserialization fails
    /// </summary>
    public class StreamDeserializationException : Exception
    {
        public string Content { get; }
        public Type TargetType { get; }

        public StreamDeserializationException(string content, Type targetType, Exception innerException)
            : base($"Deserialization Failed. Type: `{targetType.Name}`. Error: `{innerException.Message}`. Content: {content}", innerException)
        {
            TargetType = targetType;
            Content = content;
        }
    }
}