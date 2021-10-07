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
            string messageText = ConstProvider.Sent_message_text;

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageText,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageEntityTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageEntity(sampleQueueEntity,
                JsonConvert.SerializeObject, AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageEntityTest2()// Failed test
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageEntity(sampleQueueEntity,
                ent => throw new Exception(), AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponseWithException(_sendMessageResponseAct, "throws exception serializing object");
        }

        [Fact, TestPriority(100)]
        public void SendMessageEntityTest3()// Failed test
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper.SendMessageEntity(sampleQueueEntity,
                ent => null, AzQueueUnitTestHelper.GetDefaultQueueName));
        }

        [Fact, TestPriority(100)]
        public void SendMessageEntityJsonSerializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageEntity(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }
        
        [Fact, TestPriority(100)]
        public void SendMessageEntityJsonSerializerBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageEntity(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName, true);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageTest5()//BinaryData
        {
            // Arrange
            var messageBinaryData = BinaryData.FromString(ConstProvider.Sent_message_text);

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageBinaryData,
                AzQueueUnitTestHelper.GetDefaultQueueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        #endregion

        #region Put - SendMessage async

        [Fact, TestPriority(100)]
        public void SendMessageEntityAsyncJsonSerializerTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageAsyncResponseAct = AzQueueUnitTestHelper.SendMessageEntityAsync(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageAsyncResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageEntityAsyncJsonSerializerBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            // Act
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessageEntityAsync(sampleQueueEntity,
                AzQueueUnitTestHelper.GetDefaultQueueName, true).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(100)]
        public void SendMessageAsyncTest()//BinaryData
        {
            // Arrange
            var messageBinaryData = BinaryData.FromString(ConstProvider.Sent_message_text);

            // Act
            var _sendMessageAsyncResponseAct = AzQueueUnitTestHelper.SendMessageAsync(messageBinaryData,
                AzQueueUnitTestHelper.GetDefaultQueueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageAsyncResponseAct);
        }

        #endregion

        #region Put - SendMessages

        [Fact, TestPriority(140)]
        public void SendMessageEntitiesTest()
        {
            // Arrange
            int maxMessages = 30;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var samplesQueueEntity = AzQueueUnitTestHelper.GenerateMessagesRandom(maxMessages, true);

            // Act
            var _sendMessageEntitiesResponseAct = AzQueueUnitTestHelper.SendMessageEntities(samplesQueueEntity, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_sendMessageEntitiesResponseAct);
            Assert.Equal(maxMessages, _sendMessageEntitiesResponseAct.Count);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(145)]
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

        #region Put - SendMessages async

        [Fact, TestPriority(150)]
        public void SendMessageEntitiesAsyncTest()
        {
            // Arrange
            int maxMessages = 30;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var samplesQueueEntity = AzQueueUnitTestHelper.GenerateMessagesRandom(maxMessages, true);

            // Act
            var _sendMessageEntitiesAsyncResponseAct = AzQueueUnitTestHelper
                .SendMessageEntitiesAsync(samplesQueueEntity, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_sendMessageEntitiesAsyncResponseAct);
            Assert.Equal(maxMessages, _sendMessageEntitiesAsyncResponseAct.Count);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(155)]
        public void SendMessagesAsyncTest2()
        {
            // Arrange
            int maxMessages = 30;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var samplesQueueEntity = AzQueueUnitTestHelper.GenerateMessagesRandom(maxMessages, true)
                .Select(ent => JsonConvert.SerializeObject(ent));

            // Act
            var _sendMessagesResponseAct = AzQueueUnitTestHelper
                .SendMessagesAsync(samplesQueueEntity, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_sendMessagesResponseAct);
            Assert.Equal(maxMessages, _sendMessagesResponseAct.Count);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
