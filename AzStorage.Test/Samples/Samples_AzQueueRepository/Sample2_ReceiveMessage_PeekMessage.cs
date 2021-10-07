using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample2_ReceiveMessage_PeekMessage
    {
        #region Get - ReceiveMessage

        [Fact, TestPriority(210)]
        public void ReceiveRawMessageTest()
        {
            // Arrange
            string messageText = ConstProvider.Sent_message_text;

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageText);

            // Act
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.ReceiveRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageEntityTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageResponseAct = AzQueueUnitTestHelper.ReceiveMessageEntity(
                JsonConvert.DeserializeObject<SampleQueueEntity>, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageEntityJsonDeserializerTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessageEntity(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void ReceiveMessageEntityJsonDeserializerTest2()// Receive from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessageEntity(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveMessageJsonDeserializerResponseAct);
            Assert.Null(_receiveMessageJsonDeserializerResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(222)]
        public void ReceiveMessageEntityJsonDeserializerBase64Test()
        {
            // Arrange
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageEntity(queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(
                _receiveMessageJsonDeserializerResponseAct, 
                sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Get - ReceiveMessage async

        [Fact, TestPriority(212)]
        public void ReceiveMessageEntityAsyncJsonDeserializerTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageEntityAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageEntityAsyncJsonDeserializerBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageEntityAsync(queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region PeekMessage

        [Fact, TestPriority(218)]
        public void PeekRawMessageTest()
        {
            // Arrange
            string messageText = ConstProvider.Sent_message_text;

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageText);

            // Act
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.PeekRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageEntityJsonDeserializerTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper.PeekMessageEntity(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageEntityJsonDeserializerTest2()// Peek from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper.PeekMessageEntity(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_peekMessageResponseAct);
            Assert.Null(_peekMessageResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageEntityJsonDeserializerBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper
                .PeekMessageEntity(queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region PeekMessage async

        [Fact, TestPriority(220)]
        public void PeekMessageEntityJsonDeserializerAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _peekMessageAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageEntityAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(220)]
        public void PeekMessageEntityJsonDeserializerAsyncBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _peekMessageAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageEntityAsync(queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
