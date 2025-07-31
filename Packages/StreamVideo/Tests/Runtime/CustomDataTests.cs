﻿#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    internal class CustomDataTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_setting_call_custom_data_expect_custom_data_set()
            => ConnectAndExecute(When_setting_call_custom_data_expect_custom_data_set_Async);

        private async Task When_setting_call_custom_data_expect_custom_data_set_Async(ITestClient client)
        {
            var streamCall = await client.JoinRandomCallAsync();

            await streamCall.CustomData.SetAsync("number", 34);

            var retrievedNumber = streamCall.CustomData.Get<int>("number");
            Assert.AreEqual(34, retrievedNumber);
        }

        [UnityTest]
        public IEnumerator When_setting_call_custom_many_data_expect_custom_data_set()
            => ConnectAndExecute(When_setting_call_custom_many_data_expect_custom_data_set_Async);

        private async Task When_setting_call_custom_many_data_expect_custom_data_set_Async(ITestClient client)
        {
            var streamCall = await client.JoinRandomCallAsync();

            var testStruct = new TestAbilityStruct
            {
                Name = "TestSkill",
                Cost = 50,
                Attributes = new Dictionary<string, int>()
                {
                    { "Armor", 30 },
                    { "MagicDefence", 80 },
                    { "Agility", -15 }
                }
            };

            var dataToSet = new Dictionary<string, object> { { "number", 34 }, { "ability", testStruct } };

            await streamCall.CustomData.SetManyAsync(dataToSet.Select(pair => (pair.Key, pair.Value)));

            var retrievedNumber = streamCall.CustomData.Get<int>("number");
            var retrievedAbility = streamCall.CustomData.Get<TestAbilityStruct>("ability");
            Assert.AreEqual(34, retrievedNumber);
            Assert.AreEqual("TestSkill", retrievedAbility.Name);
            Assert.AreEqual(50, retrievedAbility.Cost);
            Assert.AreEqual(30, retrievedAbility.Attributes["Armor"]);
            Assert.AreEqual(80, retrievedAbility.Attributes["MagicDefence"]);
            Assert.AreEqual(-15, retrievedAbility.Attributes["Agility"]);

            //StreamTodo: we should intercept the HttpClient and check that the data is actually serialized and send to the API. Currently we're just checking if local write works
        }

        private struct TestAbilityStruct
        {
            public string Name { get; set; }
            public int Cost { get; set; }
            public Dictionary<string, int> Attributes { get; set; }
        }
        
        [UnityTest]
        public IEnumerator When_setting_participant_custom_data_expect_custom_data_set()
            => ConnectAndExecute(When_setting_participant_custom_data_expect_custom_data_set_Async);

        private async Task When_setting_participant_custom_data_expect_custom_data_set_Async(ITestClient client)
        {
            var streamCall = await client.JoinRandomCallAsync();
            var participant = streamCall.Participants.FirstOrDefault();
            Assert.NotNull(participant);
            
            await participant.CustomData.SetAsync("position", new Vector3(1, 2, 3));
            var position = participant.CustomData.Get<Vector3>("position");
            Assert.AreEqual(new Vector3(1, 2, 3), position);
        }
    }
}
#endif