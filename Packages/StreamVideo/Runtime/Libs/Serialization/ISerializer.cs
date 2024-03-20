namespace StreamVideo.Libs.Serialization
{
    //StreamTodo: rename to IJsonSerializer because we expect json format or even IStreamSerializer + prefix all other dependencies with IStream
    /// <summary>
    /// Serializes objects to string and reverse
    /// </summary>
    public interface ISerializer
    {
        string Serialize<TType>(TType obj);

        string Serialize<TType>(TType obj, ISerializationOptions serializationOptions);

        TType Deserialize<TType>(string serializedObj);

        bool TryPeekValue<TValue>(string serializedObj, string key, out TValue value);

        object DeserializeObject(string serializedObj);

        TTargetType TryConvertTo<TTargetType>(object serializedObj);
    }
}