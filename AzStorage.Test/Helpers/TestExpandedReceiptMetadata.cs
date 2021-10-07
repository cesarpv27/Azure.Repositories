using AzStorage.Core.Queues;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Test.Helpers
{
    public class TestExpandedReceiptMetadata : ExpandedReceiptMetadata
    {
        public TestExpandedReceiptMetadata(
            string messageId,
            string popReceipt,
            string messageText = default,
            BinaryData message = default,
            TimeSpan visibilityTimeout = default)
        {
            MessageText = messageText;
         
            Initialize(messageId, popReceipt);
            InitializeExpandedReceiptMetadata(message, visibilityTimeout);
        }
    }
}
