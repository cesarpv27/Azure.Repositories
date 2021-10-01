using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample6_ReceiveMessages
    {
        #region Receive messages

        [Fact, TestPriority(610)]
        public void ReceiveMessagesTest()// Json deserializer
        {
            // Arrange
            int maxMessages = 32;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity);

            // Act
            var _receiveMessagesResponseAct = AzQueueUnitTestHelper.ReceiveMessages(maxMessages, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(614)]
        public void ReceiveMessagesTest2()// Json deserializer & base64Encoding
        {
            // Arrange
            int maxMessages = 32;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _receiveMessagesResponseAct = AzQueueUnitTestHelper.ReceiveMessages(maxMessages, queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(615)]
        public void ReceiveMessagesTest3() // Empty queue
        {
            // Arrange
            int maxMessages = 32;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Act
            var _receiveMessagesResponseAct = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct);
            Assert.Empty(_receiveMessagesResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(618)]
        public void ReceiveMessagesTest4() // Invalid maxMessages, throws ArgumentOutOfRangeException
        {
            // Arrange
            int maxMessages0 = 0;
            int maxMessages33 = 33;
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages0, queueName));
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages33, queueName));
        }

        [Fact, TestPriority(622)]
        public void ReceiveMessagesTest5()// Default visibilityTimeout
        {
            // Arrange
            int maxMessages = 10;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            var _receiveMessagesResponseArr = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages / 2, queueName, base64Encoding);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseArr);
            Assert.Equal(maxMessages / 2, _receiveMessagesResponseArr.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseArr, samplesQueueEntity);

            // Act
            var _receiveMessagesResponseAct = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages / 2, queueName, base64Encoding);

            var _receiveMessagesResponseAct2 = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages / 2, queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct);
            Assert.Equal(maxMessages / 2, _receiveMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseAct, samplesQueueEntity);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct2);
            Assert.Empty(_receiveMessagesResponseAct2.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(626)]
        public void ReceiveMessagesTest6() // Custom visibilityTimeout
        {
            // Arrange
            int maxMessages = 10;
            bool base64Encoding = true;
            TimeSpan visibilityTimeout = new TimeSpan(0, 0, 8);
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _receiveMessagesResponseAct = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages, queueName, base64Encoding, visibilityTimeout);

            var _receiveMessagesResponseAct2 = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages, queueName, base64Encoding);

            Thread.Sleep(visibilityTimeout);

            var _receiveMessagesResponseAct3 = AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages, queueName, base64Encoding, visibilityTimeout);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseAct, samplesQueueEntity);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct2);
            Assert.Empty(_receiveMessagesResponseAct2.Value);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct3);
            Assert.Equal(maxMessages, _receiveMessagesResponseAct3.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesResponseAct3, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(630)]
        public void ReceiveMessagesTest7()// Failed: Throws InvalidOperationException. Json deserializer & base64Encoding true only when sending a message.
        {
            // Arrange
            int maxMessages = 1;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);
            base64Encoding = false;

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper
                .ReceiveMessages(maxMessages, queueName, base64Encoding));

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Receive messages

        [Fact, TestPriority(650)]
        public void ReceiveMessagesAsyncTest()// Json deserializer
        {
            // Arrange
            int maxMessages = 32;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity);

            // Act
            var _receiveMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(654)]
        public void ReceiveMessagesAsyncTest2()// Json deserializer & base64Encoding
        {
            // Arrange
            int maxMessages = 32;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _receiveMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseAct, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(658)]
        public void ReceiveMessagesAsyncTest3() // Empty queue
        {
            // Arrange
            int maxMessages = 32;
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Act
            var _receiveMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct);
            Assert.Empty(_receiveMessagesAsyncResponseAct.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(662)]
        public void ReceiveMessagesAsyncTest4() // Invalid maxMessages, throws ArgumentOutOfRangeException
        {
            // Arrange
            int maxMessages0 = 0;
            int maxMessages33 = 33;
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages0, queueName).WaitAndUnwrapException());
            Assert.Throws<ArgumentOutOfRangeException>("maxMessages", () => AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages33, queueName).WaitAndUnwrapException());
        }

        [Fact, TestPriority(665)]
        public void ReceiveMessagesAsyncTest5()// Default visibilityTimeout
        {
            // Arrange
            int maxMessages = 10;
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            var _receiveMessagesAsyncResponseArr = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages / 2, queueName, base64Encoding).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseArr);
            Assert.Equal(maxMessages / 2, _receiveMessagesAsyncResponseArr.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseArr, samplesQueueEntity);

            // Act
            var _receiveMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages / 2, queueName, base64Encoding).WaitAndUnwrapException();
            
            var _receiveMessagesAsyncResponseAct2 = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages / 2, queueName, base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct);
            Assert.Equal(maxMessages / 2, _receiveMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseAct, samplesQueueEntity);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct2);
            Assert.Empty(_receiveMessagesAsyncResponseAct2.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(670)]
        public void ReceiveMessagesAsyncTest6() // Custom visibilityTimeout
        {
            // Arrange
            int maxMessages = 10;
            bool base64Encoding = true;
            TimeSpan visibilityTimeout = new TimeSpan(0, 0, 8);
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(maxMessages,
                out List<SampleQueueEntity> samplesQueueEntity, base64Encoding);

            // Act
            var _receiveMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName, base64Encoding, visibilityTimeout).WaitAndUnwrapException();

            var _receiveMessagesAsyncResponseAct2 = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName, base64Encoding).WaitAndUnwrapException();

            Thread.Sleep(visibilityTimeout);

            var _receiveMessagesAsyncResponseAct3 = AzQueueUnitTestHelper
                .ReceiveMessagesAsync(maxMessages, queueName, base64Encoding, visibilityTimeout).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct);
            Assert.Equal(maxMessages, _receiveMessagesAsyncResponseAct.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseAct, samplesQueueEntity);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct2);
            Assert.Empty(_receiveMessagesAsyncResponseAct2.Value);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesAsyncResponseAct3);
            Assert.Equal(maxMessages, _receiveMessagesAsyncResponseAct3.Value.Count);
            AzQueueUnitTestHelper.AssertSamplesQueueEntityFromResponse(_receiveMessagesAsyncResponseAct3, samplesQueueEntity);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
