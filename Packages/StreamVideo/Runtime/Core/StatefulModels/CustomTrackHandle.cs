namespace StreamVideo.Core.StatefulModels
{
    public readonly struct CustomTrackHandle
    {
        public readonly string Id;

        public CustomTrackHandle(string id)
        {
            Id = id;
        }
    }

    internal interface ICustomVideoSource
    {
        
    }
}