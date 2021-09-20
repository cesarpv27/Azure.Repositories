using System;
using System.Collections.Generic;
using System.Text;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_DeleteMessage
    {
        #region DeleteMessage

        [Fact, TestPriority(300)]
        public void DeleteMessageTest()
        {
            // Arrange
            string messageContent = "create queue test";

            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageContent, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
            string messageId = _sendMessageResponseAct.Value.MessageId;
            string popReceipt = _sendMessageResponseAct.Value.PopReceipt;

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_receiveRawMessageResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(300)]
        public void DeleteMessageTest2()// Failed: Message does not exist
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Arr
            var _receiveRawMessageResponseAct = AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(_receiveRawMessageResponseAct, "The specified message does not exist");

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(300)]
        public void DeleteMessageTest3()// Failed: messageId is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = default;
            string popReceipt = "AgAAAAMAAAAAAAAAFoHaalyu1wE=";

            // Arr
            // Assert
            Assert.Throws<ArgumentNullException>("messageId",
                () => AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName));
        }
        
        [Fact, TestPriority(300)]
        public void DeleteMessageTest4()// Failed: popReceipt is null
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            string messageId = Guid.NewGuid().ToString();
            string popReceipt = default;

            // Arr
            // Assert
            Assert.Throws<ArgumentNullException>("popReceipt",
                () => AzQueueUnitTestHelper.DeleteMessage(messageId, popReceipt, queueName));
        }

        #endregion
    }
}
