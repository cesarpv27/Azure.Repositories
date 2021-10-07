using AzStorage.Core.Queues;
using AzStorage.Core.Texting;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample7_UpdateMessage_UpdateMessages
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

            var _receiveMessagesResponseAct1 = AzQueueUnitTestHelper.ReceiveMessageEntities(1, queueName, base64Encoding);

            Thread.Sleep(visibilityTimeout);

            var _receiveMessagesResponseAct2 = AzQueueUnitTestHelper.ReceiveMessageEntities(1, queueName, base64Encoding);

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

        [Fact, TestPriority(718)]
        public void UpdateMessageTest5()// Failed: both Message text & BinaryData filled
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = "Updated -> 1";
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryData = new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, BinaryData.FromString(updatedText), visibilityTimeout);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper
                .UpdateMessage(expandedReceiptMetadataBinaryData, queueName));

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(720)]
        public void UpdateMessageTest6()// Failed: Message text & BinaryData neither defined
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = default;
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryData = new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, default, visibilityTimeout);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper
                .UpdateMessage(expandedReceiptMetadataBinaryData, queueName));

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

        [Fact, TestPriority(756)]
        public void UpdateMessageAsyncTest4()// Failed: both Message text & BinaryData filled
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = "Updated -> 1";
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryData = new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, BinaryData.FromString(updatedText), visibilityTimeout);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper
                .UpdateMessageAsync(expandedReceiptMetadataBinaryData, queueName).WaitAndUnwrapException());

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(758)]
        public void UpdateMessageAsyncTest5()// Failed: Message text & BinaryData neither defined
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = default;
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryData = new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, default, visibilityTimeout);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => AzQueueUnitTestHelper
                .UpdateMessageAsync(expandedReceiptMetadataBinaryData, queueName).WaitAndUnwrapException());

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Update messages

        [Fact, TestPriority(760)]
        public void UpdateMessagesTest()
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var expandedReceiptMetadataList = AzQueueUnitTestHelper
                .GenerateExpandedReceiptMetadataList(receiptsMetadata);

            // Act
            var _updateMessagesResponseAct = AzQueueUnitTestHelper
                .UpdateMessages(expandedReceiptMetadataList, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_updateMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(762)]
        public void UpdateMessagesTest2()// BinaryData
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var expandedReceiptMetadataBinaryDataList = AzQueueUnitTestHelper
                .GenerateExpandedReceiptMetadataList(receiptsMetadata, binaryData: true);

            // Act
            var _updateMessagesResponseAct = AzQueueUnitTestHelper
                .UpdateMessages(expandedReceiptMetadataBinaryDataList, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_updateMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Update messages async

        [Fact, TestPriority(780)]
        public void UpdateMessagesAsyncTest()
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var expandedReceiptMetadataList = AzQueueUnitTestHelper
                .GenerateExpandedReceiptMetadataList(receiptsMetadata);

            // Act
            var _updateMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessagesAsync(expandedReceiptMetadataList, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_updateMessagesAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(782)]
        public void UpdateMessagesAsyncTest2()// BinaryData
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var expandedReceiptMetadataBinaryDataList = AzQueueUnitTestHelper
                .GenerateExpandedReceiptMetadataList(receiptsMetadata, binaryData: true);

            // Act
            var _updateMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessagesAsync(expandedReceiptMetadataBinaryDataList, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_updateMessagesAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(784)]
        public void UpdateMessagesAsyncTest3()// Failed: both Message text & BinaryData filled
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = "Updated -> 1";
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryDataList = new List<TestExpandedReceiptMetadata>(receiptsMetadata.Count)
            {
                new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, BinaryData.FromString(updatedText), visibilityTimeout)
            };

            // Act
            var _updateMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessagesAsync(expandedReceiptMetadataBinaryDataList, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedGenResponse(_updateMessagesAsyncResponseAct.First(), 
                ErrorTextProvider.Invalid_operation_message_defined_twice);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(786)]
        public void UpdateMessagesAsyncTest4()// Failed: Message text & BinaryData neither defined
        {
            // Arrange
            int maxMessages = 1;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var firstReceipt = receiptsMetadata.First();
            string updatedText = default;
            var visibilityTimeout = new TimeSpan(0, 0, 5);

            var expandedReceiptMetadataBinaryDataList = new List<TestExpandedReceiptMetadata>(receiptsMetadata.Count)
            {
                new TestExpandedReceiptMetadata(firstReceipt.MessageId,
                firstReceipt.PopReceipt, updatedText, default, visibilityTimeout)
            };

            // Act
            var _updateMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .UpdateMessagesAsync(expandedReceiptMetadataBinaryDataList, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedGenResponse(_updateMessagesAsyncResponseAct.First(), ErrorTextProvider.Invalid_operation_message_not_defined);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
