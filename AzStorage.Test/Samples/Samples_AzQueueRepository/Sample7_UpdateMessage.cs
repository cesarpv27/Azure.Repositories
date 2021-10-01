using AzStorage.Core.Queues;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample7_UpdateMessage
    {
        #region Update message

        [Fact, TestPriority(710)]
        public void UpdateMessageTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);
            var messageTextToUpdate = ConstProvider.Updated_message_text;

            // Act
            var _updateMessageResponseAct = AzQueueUnitTestHelper
                .UpdateMessage(new ReceiptMetadata(messageId, popReceipt), queueName, messageTextToUpdate);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(712)]
        public void UpdateMessageTest2()// base64Encoding true
        {
            // Arrange
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, base64Encoding, false);
            var messageTextToUpdate = ConstProvider.Updated_message_text;

            // Act
            var _updateMessageResponseAct = AzQueueUnitTestHelper
                .UpdateMessage(new ReceiptMetadata(messageId, popReceipt), queueName, messageTextToUpdate,
                base64Encoding: base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(714)]
        public void UpdateMessageTest3()// base64Encoding true & default messageText & visibilityTimeout 8 seg
        {
            // Arrange
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, base64Encoding, false);
            string messageTextToUpdate = default;
            var visibilityTimeout = new TimeSpan(0, 0, 8);

            // Act
            var _updateMessageResponseAct = AzQueueUnitTestHelper
                .UpdateMessage(new ReceiptMetadata(messageId, popReceipt), queueName, messageTextToUpdate,
                visibilityTimeout, base64Encoding);

            var _receiveMessagesResponseAct1 = AzQueueUnitTestHelper.ReceiveMessages(1, queueName, base64Encoding);

            Thread.Sleep(visibilityTimeout);

            var _receiveMessagesResponseAct2 = AzQueueUnitTestHelper.ReceiveMessages(1, queueName, base64Encoding);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageResponseAct);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct1);
            Assert.Empty(_receiveMessagesResponseAct1.Value);

            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_receiveMessagesResponseAct2);
            Assert.NotEmpty(_receiveMessagesResponseAct2.Value);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(716)]
        public void UpdateMessageTest4()// BinaryData message
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            var messageBinaryDataToUpdate = BinaryData.FromString(ConstProvider.Updated_message_text);

            // Act
            var _updateMessageResponseAct = AzQueueUnitTestHelper
                .UpdateMessage(new ReceiptMetadata(messageId, popReceipt), messageBinaryDataToUpdate, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Update message async

        [Fact, TestPriority(750)]
        public void UpdateMessageAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);
            var messageTextToUpdate = ConstProvider.Updated_message_text;

            // Act
            var _updateMessageAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessageAsync(new ReceiptMetadata(messageId, popReceipt), queueName, messageTextToUpdate)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(752)]
        public void UpdateMessageAsyncTest2()// base64Encoding true
        {
            // Arrange
            bool base64Encoding = true;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, base64Encoding, false);
            var messageTextToUpdate = ConstProvider.Updated_message_text;

            // Act
            var _updateMessageAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessageAsync(new ReceiptMetadata(messageId, popReceipt), queueName, messageTextToUpdate,
                base64Encoding: base64Encoding).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(754)]
        public void UpdateMessageAsyncTest3()// BinaryData message
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            var messageBinaryDataToUpdate = BinaryData.FromString(ConstProvider.Updated_message_text);

            // Act
            var _updateMessageAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessageAsync(new ReceiptMetadata(messageId, popReceipt), messageBinaryDataToUpdate, queueName)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_updateMessageAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
