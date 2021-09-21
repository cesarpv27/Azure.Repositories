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

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.ReceiveRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
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
        
        [Fact, TestPriority(212)]
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

        [Fact, TestPriority(220)]
        public void ReceiveMessageJsonDeserializerTest2()// Receive from empty queue
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

        [Fact, TestPriority(222)]
        public void ReceiveMessageJsonDeserializerBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity, true);

            // Arr
            var _receiveMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageJsonDeserializer(queueName, true);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Get - ReceiveMessage async

        [Fact, TestPriority(212)]
        public void ReceiveMessageJsonDeserializerAsyncTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity);

            // Arr
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageJsonDeserializerAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_receiveMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(212)]
        public void ReceiveMessageJsonDeserializerAsyncBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity, true);

            // Arr
            var _receiveMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessageJsonDeserializerAsync(queueName, true).WaitAndUnwrapException();

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

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.PeekRawMessage(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(220)]
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

        [Fact, TestPriority(220)]
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

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity, true);

            // Arr
            var _peekMessageJsonDeserializerResponseAct = AzQueueUnitTestHelper
                .PeekMessageJsonDeserializer(queueName, true);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageJsonDeserializerResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageJsonDeserializerResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region PeekMessage async

        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerAsyncTest()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity);

            // Arr
            var _peekMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageJsonDeserializerAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(220)]
        public void PeekMessageJsonDeserializerAsyncBase64Test()
        {
            // Arrange
            var sampleQueueEntity = AzQueueUnitTestHelper.GenerateDefaultSampleQueueEntity();

            var queueName = AzQueueUnitTestHelper.SendAssertMessageRandomQueueName(sampleQueueEntity, true);

            // Arr
            var _peekMessageJsonDeserializerAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessageJsonDeserializerAsync(queueName, true).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessageJsonDeserializerAsyncResponseAct);
            AzQueueUnitTestHelper.AssertSampleQueueEntityFromResponse(_peekMessageJsonDeserializerAsyncResponseAct, sampleQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

    }
}
