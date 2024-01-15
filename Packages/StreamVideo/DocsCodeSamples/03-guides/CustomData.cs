using System.Linq;
using StreamVideo.Core;

namespace DocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/custom-data/ page
    /// </summary>
    internal class CustomData
    {
        public async void WriteCallCustomData()
        {
            var streamCall = await _client.GetOrCreateCallAsync(StreamCallType.Default, "my-call-id");
            
            // Writing custom data
            await streamCall.CustomData.SetAsync("my_number", 34); // int type
            await streamCall.CustomData.SetAsync("my_string", "hello"); // string type

            var sampleStruct = new SampleStruct(name: "Peter", age: 27);
            
            await streamCall.CustomData.SetAsync("my_custom_type", sampleStruct); // custom type
        }
        
        //StreamTodo: provide examples on SetManyAsync
        
        public async void ReadCallCustomData()
        {
            var streamCall = await _client.GetOrCreateCallAsync(StreamCallType.Default, "my-call-id");
            
            // Reading custom data
            var myNumber = streamCall.CustomData.Get<int>("my_number"); // int type
            var myString = streamCall.CustomData.Get<string>("my_string"); // string type
            var myCustomType = streamCall.CustomData.Get<SampleStruct>("my_custom_type"); // custom type
        }
        
        public async void WriteCallParticipantCustomData()
        {
            var streamCall = await _client.GetOrCreateCallAsync(StreamCallType.Default, "my-call-id");
            
            // Get sample call participant
            var participant = streamCall.Participants.First();
            
            // Writing custom data
            await participant.CustomData.SetAsync("my_number", 34); // int type
            await participant.CustomData.SetAsync("my_string", "hello"); // string type

            var sampleStruct = new SampleStruct(name: "Peter", age: 27);
            
            await participant.CustomData.SetAsync("my_custom_type", sampleStruct); // custom type
        }
        
        public async void ReadCallParticipantCustomData()
        {
            var streamCall = await _client.GetOrCreateCallAsync(StreamCallType.Default, "my-call-id");

            // Get sample call participant
            var participant = streamCall.Participants.First();

            // Reading custom data
            var myNumber = participant.CustomData.Get<int>("my_number"); // int type
            var myString = participant.CustomData.Get<string>("my_string"); // string type
            var myCustomType = participant.CustomData.Get<SampleStruct>("my_custom_type"); // custom type
        }
        
        private IStreamVideoClient _client;

        private readonly struct SampleStruct
        {
            public readonly string Name;
            public readonly int Age;

            public SampleStruct(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }
    }
}