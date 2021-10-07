using System;
using System.Collections.Generic;
using System.Text;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample4_ClearMessages
    {
        [Fact, TestPriority(300)]
        public void ClearMessagesTest()
        {
            // Arrange
            string messageText = ConstProvider.Sent_message_text;

            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageText, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);

            // Act
            var _clearMessagesResponseAct = AzQueueUnitTestHelper.ClearMessages(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_clearMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(300)]
        public void ClearMessagesAsyncTest()
        {
            // Arrange
            string messageText = ConstProvider.Sent_message_text;

            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageText, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);

            // Act
            var _clearMessagesResponseAct = AzQueueUnitTestHelper.ClearMessagesAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_clearMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
    }
}
