namespace StreamVideo.Libs.Serialization
{
    public class SerializationOptions : ISerializationOptions
    {
        public SerializerIgnorePropertyHandler IgnorePropertyHandler { get; set; }
        public ISerializationConverter[] Converters { get; set; }
        public SerializationNamingStrategy? NamingStrategy { get; set; }
    }
}