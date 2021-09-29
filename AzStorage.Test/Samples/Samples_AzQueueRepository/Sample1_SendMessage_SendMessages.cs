using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_SendMessage_SendMessages
    {
        #region Put - SendMessage

        [Fact, TestPriority(100)]
        public void SendMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            // Act
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

            // Act
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

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                ent => throw new Exception(), AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponseWithException(_sendMessageResponseAct, "throws exception serializing object");
        }

        [Fact, TestPriority(100)]
        public void SendMessageTest4()// Failed test
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                ent => null, AzQueueUnitTestHelper.GetDefaultQueueName));
        }

        [Fact, TestPriority(100)]
        public void SendMessageJsonSerializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }
        
        [Fact, TestPriority(100)]
        public void SendMessageJsonSerializerBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName, true);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }
        
        #endregion

        #region Put - SendMessage async

        [Fact, TestPriority(100)]
        public void SendMessageJsonSerializerAsyncTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageAsyncResponseAct = AzQueueUnitTestHelper.SendMessageAsync(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageAsyncResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageJsonSerializerAsyncBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageAsync(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName, true).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        #endregion

        #region Put - SendMessages

        [Fact, TestPriority(110)]
        public void SendMessagesTest()
        {
            // Arrange
            int maxMessages = 30;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var samplesQueueEntity = AzQueueUnitTestHelper.GenerateMessagesRandom(maxMessages, true);

            // Act
            var _sendMessagesResponseAct = AzQueueUnitTestHelper.SendMessages(samplesQueueEntity, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_sendMessagesResponseAct);
            Assert.Equal(maxMessages, _sendMessagesResponseAct.Count);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(110)]
        public void SendMessagesTest2()
        {
            // Arrange
            int maxMessages = 30;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var samplesQueueEntity = AzQueueUnitTestHelper.GenerateMessagesRandom(maxMessages, true)
                .Select(ent => JsonConvert.SerializeObject(ent));

            // Act
            var _sendMessagesResponseAct = AzQueueUnitTestHelper.SendMessages(samplesQueueEntity, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_sendMessagesResponseAct);
            Assert.Equal(maxMessages, _sendMessagesResponseAct.Count);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
