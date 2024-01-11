using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace Tests.Runtime
{
    internal class CustomDataTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_setting_call_custom_data_expect_custom_data_set()
            => ConnectAndExecute(When_setting_call_custom_data_expect_custom_data_set_Async);

        private async Task When_setting_call_custom_data_expect_custom_data_set_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            Assert.AreEqual(0, streamCall.CustomData.Count);

            await streamCall.CustomData.SetAsync("number", 34);

            var retrievedNumber = streamCall.CustomData.Get<int>("number");
            Assert.AreEqual(34, retrievedNumber);
            Assert.AreEqual(1, 0, streamCall.CustomData.Count);
        }

        [UnityTest]
        public IEnumerator When_setting_call_custom_many_data_expect_custom_data_set()
            => ConnectAndExecute(When_setting_call_custom_many_data_expect_custom_data_set_Async);

        private async Task When_setting_call_custom_many_data_expect_custom_data_set_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            Assert.AreEqual(0, streamCall.CustomData.Count);

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


            Assert.AreEqual(2, 0, streamCall.CustomData.Count);
        }

        private struct TestAbilityStruct
        {
            public string Name { get; set; }
            public int Cost { get; set; }
            public Dictionary<string, int> Attributes { get; set; }
        }
    }
}