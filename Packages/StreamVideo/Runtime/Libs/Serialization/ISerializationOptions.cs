namespace StreamVideo.Libs.Serialization
{
    /// <summary>
    /// Options for the serialization process
    /// </summary>
    public interface ISerializationOptions
    {
        /// <summary>
        /// Determines whether a property should be ignored during serialization. Return true if the property should be ignored or false otherwise.
        /// </summary>
        SerializerIgnorePropertyHandler IgnorePropertyHandler { get; }
        
        /// <summary>
        /// Converters used during serialization
        /// </summary>
        ISerializationConverter[] Converters { get; }

        SerializationNamingStrategy? NamingStrategy { get; set; }
    }
}