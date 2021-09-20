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
    public class Sample1_SendMessage_ReceiveMessage_PeekMessage
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

        #region Get - ReceiveMessage

        [Fact, TestPriority(110)]
        public void ReceiveRawMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageContent);

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.ReceiveRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(112)]
        public void ReceiveMessageTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity);

            // Arr
            var _receiveMessageResponseAct = AzQueueUnitTestHelper.ReceiveMessage(
                JsonConvert.DeserializeObject<SampleQueueEntity>, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(112)]
        public void ReceiveMessageJsonDeserializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity);

            // Arr
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessageJsonDeserializer(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(120)]
        public void ReceiveMessageJsonDeserializer2()// Receive from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessageJsonDeserializer(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveMessageJsonDeserializerResponseAct);
            Assert.Null(_receiveMessageJsonDeserializerResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region PeekMessage

        [Fact, TestPriority(118)]
        public void PeekRawMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageContent);

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.PeekRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(120)]
        public void PeekMessageJsonDeserializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity);

            // Arr
            var _peekMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.PeekMessageJsonDeserializer(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(120)]
        public void PeekMessageJsonDeserializerTest2()// Peek from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _peekMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.PeekMessageJsonDeserializer(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_peekMessageJsonDeserializerResponseAct);
            Assert.Null(_peekMessageJsonDeserializerResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        #endregion
    }
}
