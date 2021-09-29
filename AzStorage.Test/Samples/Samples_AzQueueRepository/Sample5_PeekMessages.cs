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
    public class Sample5_PeekMessages
    {
        #region Peek messages

        [Fact, TestPriority(510)]
        public void PeekMessagesTest()// Json deserializer
        {
            // Arrange
            int maxMessages = 32;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity);

            // Act
            var _peekMessagesResponseAct = AzQueueUnitTestHelper.PeekMessages(maxMessages, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessagesResponseAct);
            Assert.Equal(maxMessages, _peekMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_peekMessagesResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(510)]
        public void PeekMessagesTest2()// Json deserializer & base64Encoding
        {
            // Arrange
            int maxMessages = 32;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _peekMessagesResponseAct = AzQueueUnitTestHelper.PeekMessages(maxMessages, queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessagesResponseAct);
            Assert.Equal(maxMessages, _peekMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_peekMessagesResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Peek messages async

        [Fact, TestPriority(510)]
        public void PeekMessagesAsyncTest()// Json deserializer
        {
            // Arrange
            int maxMessages = 32;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity);

            // Act
            var _peekMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessagesAsync(maxMessages, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessagesAsyncResponseAct);
            Assert.Equal(maxMessages, _peekMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_peekMessagesAsyncResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(510)]
        public void PeekMessagesAsyncTest2()// Json deserializer & base64Encoding
        {
            // Arrange
            int maxMessages = 32;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _peekMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessagesAsync(maxMessages, queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessagesAsyncResponseAct);
            Assert.Equal(maxMessages, _peekMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_peekMessagesAsyncResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(510)]
        public void PeekMessagesAsyncTest3() // Empty queue
        {
            // Arrange
            int maxMessages = 32;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Act
            var _peekMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .PeekMessagesAsync(maxMessages, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_peekMessagesAsyncResponseAct);
            Assert.Empty(_peekMessagesAsyncResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(510)]
        public void PeekMessagesAsyncTest4() // Invalid maxMessages, throws ArgumentOutOfRangeException
        {
            // Arrange
            int maxMessages0 = 0;
            int maxMessages33 = 33;
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .PeekMessagesAsync(maxMessages0, queueName).WaitAndUnwrapException());
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .PeekMessagesAsync(maxMessages33, queueName).WaitAndUnwrapException());
        }

        #endregion
    }
}
