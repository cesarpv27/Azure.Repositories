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
            string messageContent = "create queue test";

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageContent);

            // Act
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.ReceiveRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageResponseAct = AzQueueUnitTestHelper.ReceiveMessage(
                JsonConvert.DeserializeObject<SampleQueueEntity>, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageJsonDeserializerTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void ReceiveMessageJsonDeserializerTest2()// Receive from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper.ReceiveMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveMessageJsonDeserializerResponseAct);
            Assert.Null(_receiveMessageJsonDeserializerResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(222)]
        public void ReceiveMessageJsonDeserializerBase64Test()
        {
            // Arrange
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper
                .ReceiveMessage(queueName, base64Encoding);

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
        public void ReceiveMessageJsonDeserializerAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageJsonDeserializerAsyncBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageAsync(queueName, base64Encoding).WaitAndUnwrapException();

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
            string messageContent = "create queue test";

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(messageContent);

            // Act
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.PeekRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper.PeekMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerTest2()// Peek from empty queue
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper.PeekMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_peekMessageResponseAct);
            Assert.Null(_peekMessageResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _peekMessageResponseAct = AzQueueUnitTestHelper
                .PeekMessage(queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region PeekMessage async

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity);

            // Act
            var _peekMessageAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerAsyncBase64Test()
        {
            // Arrange
            var base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out SampleQueueEntity sampleQueueEntity, base64Encoding);

            // Act
            var _peekMessageAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageAsync(queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
