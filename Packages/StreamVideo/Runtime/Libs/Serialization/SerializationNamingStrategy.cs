namespace StreamVideo.Libs.Serialization
{
    /// <summary>
    /// Naming strategy for the serialized keys and property names
    /// </summary>
    public enum SerializationNamingStrategy
    {
        Default = 0,
        CamelCase = 1,
        KebabCase = 2,
        SnakeCase = 3,
    }
}