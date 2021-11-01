using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AzStorage.Core.Queues;
using AzStorage.Core.Texting;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_DeleteMessage_DeleteMessages
    {
        #region DeleteMessage

        [Fact, TestPriority(310)]
        public void DeleteMessageTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            // Act
            var _deleteMessageResponseAct = AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(312)]
        public void DeleteMessageTest2()// Failed: Message does not exist
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Act
            var _deleteMessageResponseAct = AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(_deleteMessageResponseAct, "The specified message does not exist");

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(314)]
        public void DeleteMessageTest3()// Failed: messageId is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = default;
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("messageId",
                () => AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName));
        }
        
        [Fact, TestPriority(316)]
        public void DeleteMessageTest4()// Failed: popReceipt is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = default;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("popReceipt",
                () => AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName));
        }

        [Fact, TestPriority(318)]
        public void DeleteMessageTest5()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            // Act
            var _deleteMessageResponseAct = AzQueueUnitTestHelper.DeleteMessage(
                new ReceiptMetadata(messageId, popReceipt), queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(320)]
        public void DeleteMessageTest6()// Failed: SendReceiptMetadata null
        {
            // Arrange
            var queueName = string.Empty;
            ReceiptMetadata receiptMetadata = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("receiptMetadata",
                () => AzQueueUnitTestHelper.DeleteMessage(receiptMetadata, queueName));
        }

        #endregion

        #region DeleteMessageAsync

        [Fact, TestPriority(350)]
        public void DeleteMessageAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            // Act
            var _deleteMessageAsyncResponseAct = AzQueueUnitTestHelper
                .DeleteMessageAsync(messageId, popReceipt, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteMessageAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(352)]
        public void DeleteMessageAsyncTest2()// Failed: Message does not exist
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Act
            var _deleteMessageAsyncResponseAct = AzQueueUnitTestHelper.
                DeleteMessageAsync(messageId, popReceipt, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(_deleteMessageAsyncResponseAct, "The specified message does not exist");

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(354)]
        public void DeleteMessageAsyncTest3()// Failed: messageId is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = default;
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("messageId",
                () => AzQueueUnitTestHelper.DeleteMessageAsync(messageId, popReceipt, queueName).WaitAndUnwrapException());
        }

        [Fact, TestPriority(356)]
        public void DeleteMessageAsyncTest4()// Failed: popReceipt is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = default;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("popReceipt",
                () => AzQueueUnitTestHelper.DeleteMessageAsync(messageId, popReceipt, queueName).WaitAndUnwrapException());
        }

        [Fact, TestPriority(358)]
        public void DeleteMessageAsyncTest5()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessageRandomQueueName(
                out string messageId, out string popReceipt, false, false);

            // Act
            var _deleteMessageResponseAct = AzQueueUnitTestHelper.DeleteMessageAsync(
                new ReceiptMetadata(messageId, popReceipt), queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(360)]
        public void DeleteMessageAsyncTest6()// Failed: SendReceiptMetadata null
        {
            // Arrange
            var queueName = string.Empty;
            ReceiptMetadata receiptMetadata = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("receiptMetadata",
                () => AzQueueUnitTestHelper.DeleteMessageAsync(receiptMetadata, queueName).WaitAndUnwrapException());
        }

        #endregion

        #region Delete messages

        [Fact, TestPriority(380)]
        public void DeleteMessagesTest()
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            // Act
            var _deleteMessagesResponseAct = AzQueueUnitTestHelper.DeleteMessages(receiptsMetadata, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(380)]
        public void DeleteMessagesTest2()
        {
            // Arrange
            var queueName = string.Empty;
            List<ReceiptMetadata> receiptsMetadata = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("receiptsMetadata",
                () => AzQueueUnitTestHelper.DeleteMessages(receiptsMetadata, queueName));
        }

        [Fact, TestPriority(382)]
        public void DeleteMessagesWithCancellationTokenTest3()
        {
            // Arrange
            int maxMessages = 100;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var cTokenSource = new CancellationTokenSource();
            CancellationToken token = cTokenSource.Token;

            // Act
            cTokenSource.CancelAfter(5000);
            var _deleteMessagesResponseAct = AzQueueUnitTestHelper.DeleteMessages(receiptsMetadata, queueName, token);

            // Assert
            UnitTestHelper.AssertExpectedCancelledResponses(_deleteMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Delete messages async

        [Fact, TestPriority(388)]
        public void DeleteMessagesAsyncTest()
        {
            // Arrange
            int maxMessages = 10;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            // Act
            var _deleteMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .DeleteMessagesAsync(receiptsMetadata, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteMessagesAsyncResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(389)]
        public void DeleteMessagesAsyncWithCancellationTokenTest1()
        {
            // Arrange
            int maxMessages = 100;
            var queueName = AzQueueUnitTestHelper.GenerateSendAssertMessagesRandomQueueName(
                maxMessages, out List<ReceiptMetadata> receiptsMetadata, false, false);

            var cTokenSource = new CancellationTokenSource();
            CancellationToken token = cTokenSource.Token;

            // Act
            cTokenSource.CancelAfter(5000);
            var _deleteMessagesResponseAct = AzQueueUnitTestHelper.DeleteMessagesAsync(receiptsMetadata, queueName, token)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedCancelledResponses(_deleteMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        [Fact, TestPriority(390)]
        public void DeleteMessagesAsyncTest2()
        {
            // Arrange
            var queueName = string.Empty;
            List<ReceiptMetadata> receiptsMetadata = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>("receiptsMetadata",
                () => AzQueueUnitTestHelper.DeleteMessagesAsync(receiptsMetadata, queueName).WaitAndUnwrapException());
        }
        
        [Fact, TestPriority(392)]
        public void DeleteMessagesAsyncTest3()// Failed: sendReceiptMetadata_is_null
        {
            // Arrange
            var queueName = string.Empty;
            var receiptsMetadata = new List<ReceiptMetadata>(1) { null };

            // Act
            var _deleteMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .DeleteMessagesAsync(receiptsMetadata, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(
                _deleteMessagesAsyncResponseAct.First(), 
                ErrorTextProvider.receiptMetadata_is_null());
        }
        
        [Fact, TestPriority(393)]
        public void DeleteMessagesAsyncTest4()// Failed: sendReceiptMetadata_MessageId_is_null_or_empty
        {
            // Arrange
            var queueName = string.Empty;
            var receiptsMetadata = new List<ReceiptMetadata>(1) 
            { 
                new TestSendReceiptMetadata(default, "Test value") 
            };

            // Act
            var _deleteMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .DeleteMessagesAsync(receiptsMetadata, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(
                _deleteMessagesAsyncResponseAct.First(), 
                ErrorTextProvider.receiptMetadata_MessageId_is_null_or_empty());
        }
        
        [Fact, TestPriority(394)]
        public void DeleteMessagesAsyncTest5()// Failed: sendReceiptMetadata_MessageId_is_null_or_whitespace
        {
            // Arrange
            var queueName = string.Empty;
            var receiptsMetadata = new List<ReceiptMetadata>(1) 
            { 
                new TestSendReceiptMetadata(" ", "Test value") 
            };

            // Act
            var _deleteMessagesAsyncResponseAct = AzQueueUnitTestHelper
                .DeleteMessagesAsync(receiptsMetadata, queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(
                _deleteMessagesAsyncResponseAct.First(), 
                ErrorTextProvider.receiptMetadata_MessageId_is_null_or_whitespace());
        }

        [Fact, TestPriority(396)]
        public void DeleteMessagesTest3()
        {
            // Arrange
            var queueName = string.Empty;
            var receiptsMetadata = new List<ReceiptMetadata>(1) 
            { 
                new TestSendReceiptMetadata("Test value", default)
            };

            // Act
            var _deleteMessagesResponseAct = AzQueueUnitTestHelper
                .DeleteMessages(receiptsMetadata, queueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(
                _deleteMessagesResponseAct.First(),
                ErrorTextProvider.receiptMetadata_PopReceipt_is_null_or_empty());
        }

        [Fact, TestPriority(398)]
        public void DeleteMessagesTest4()
        {
            // Arrange
            var queueName = string.Empty;
            var receiptsMetadata = new List<ReceiptMetadata>(1)
            {
                new TestSendReceiptMetadata("Test value", " ")
            };

            // Act
            var _deleteMessagesResponseAct = AzQueueUnitTestHelper
                .DeleteMessages(receiptsMetadata, queueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(
                _deleteMessagesResponseAct.First(),
                ErrorTextProvider.receiptMetadata_PopReceipt_is_null_or_whitespace());
        }

        #endregion
    }
}
