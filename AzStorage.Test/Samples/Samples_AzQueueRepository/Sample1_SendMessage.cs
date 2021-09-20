using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Newtonsoft.Json;
using System;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_SendMessage
    {
        #region Put - SendMessage

        [Fact, TestPriority(100)]
        public void SendMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageContent,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageTest2()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                JsonConvert.SerializeObject, AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageTest3()// Failed test
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                ent => throw new Exception(), AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponseWithException(_sendMessageResponseAct, "throw exception serializing object");
        }

        [Fact, TestPriority(100)]
        public void SendMessageTest4()// Failed test
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                ent => null, AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(_sendMessageResponseAct, "returned null by serializing the object");
        }

        [Fact, TestPriority(100)]
        public void SendMessageJsonSerializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageJsonSerializer(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        #endregion
    }
}
