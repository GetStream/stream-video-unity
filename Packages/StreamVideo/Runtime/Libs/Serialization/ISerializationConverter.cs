namespace StreamVideo.Libs.Serialization
{
    /// <summary>
    /// Converts a value during serialization if matched by <see cref="CanConvert"/>
    /// </summary>
    public interface ISerializationConverter
    {
        bool CanConvert(System.Type objectType);

        object Convert(object value);
    }
}