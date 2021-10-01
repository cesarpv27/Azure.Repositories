using AzStorage.Core.Queues;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Test.Helpers
{
    public class TestSendReceiptMetadata : ReceiptMetadata
    {
        public TestSendReceiptMetadata(string messageId, string popReceipt)
        {
            MessageId = messageId;
            PopReceipt = popReceipt;
        }
    }
}
