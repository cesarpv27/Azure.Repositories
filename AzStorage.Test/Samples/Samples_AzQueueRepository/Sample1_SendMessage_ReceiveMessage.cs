using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_SendMessage_ReceiveMessage
    {
        #region SendMessage

        [Fact, TestPriority(100)]
        public void SendMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageContent,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
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
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
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

        //[Fact, TestPriority(100)]
        //public void SendMessageTest2()
        //{
        //    // Arrange
        //    string messageContent = "create queue test";
        //    var rdm = new Random();
        //    string queueName = AzQueueUnitTestHelper.GetDefaultQueueName + rdm.Next(1, int.MaxValue);

        //    // Arr
        //    var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageContent,
        //        AzQueueUnitTestHelper.GetDefaultQueueName);

        //    // Assert
        //    UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        //}

        #endregion

        #region ReceiveMessage

        [Fact, TestPriority(110)]
        public void ReceiveRawMessageTest()
        {
            // Arrange
            var rdm = new Random();
            var id = rdm.Next(1, int.MaxValue);
            string messageContent = $"create queue test - {id}";
            string queueName = AzQueueUnitTestHelper.GetDefaultQueueName + id;

            AzQueueUnitTestHelper.SendAssertMessage(messageContent, queueName);

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.ReceiveRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(112)]
        public void ReceiveMessageTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();
            var rdm = new Random();
            var id = rdm.Next(1, int.MaxValue);
            string queueName = AzQueueUnitTestHelper.GetDefaultQueueName + id;

            AzQueueUnitTestHelper.SendAssertMessage(sampleQueueEntity, queueName);

            // Arr
            var _receiveMessageResponseAct = AzQueueUnitTestHelper.ReceiveMessage(
                JsonConvert.DeserializeObject<SampleQueueEntity>, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveMessageResponseAct);
            Assert.NotNull(_receiveMessageResponseAct.Value);

            var recoveredEntity = _receiveMessageResponseAct.Value;
            Assert.Equal(sampleQueueEntity.Prop1, recoveredEntity.Prop1);
            Assert.Equal(sampleQueueEntity.Prop2, recoveredEntity.Prop2);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

    }
}
