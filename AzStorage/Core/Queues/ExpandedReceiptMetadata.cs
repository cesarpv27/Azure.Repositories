using System;
using ExThrower = CoreTools.Throws.ExceptionThrower;


namespace AzStorage.Core.Queues
{
    public class ExpandedReceiptMetadata : ReceiptMetadata
    {
        protected ExpandedReceiptMetadata() { }

        public ExpandedReceiptMetadata(
            string messageId, 
            string popReceipt, 
            string messageText = default,
            TimeSpan visibilityTimeout = default) : base(messageId, popReceipt)
        {
            InitializeExpandedReceiptMetadata(messageText, visibilityTimeout);
        }
        
        public ExpandedReceiptMetadata(
            string messageId, 
            string popReceipt,
            BinaryData message = default,
            TimeSpan visibilityTimeout = default) : base(messageId, popReceipt)
        {
            InitializeExpandedReceiptMetadata(message, visibilityTimeout);
        }

        protected virtual void InitializeExpandedReceiptMetadata(string messageText, TimeSpan visibilityTimeout)
        {
            MessageText = messageText;
            VisibilityTimeout = visibilityTimeout;
        }
        
        protected virtual void InitializeExpandedReceiptMetadata(BinaryData message, TimeSpan visibilityTimeout)
        {
            Message = message;
            VisibilityTimeout = visibilityTimeout;
        }

        /// <summary>
        /// Message text
        /// </summary>
        public virtual string MessageText { get; protected set; }
        
        /// <summary>
        /// Message
        /// </summary>
        public virtual BinaryData Message { get; protected set; }

        /// <summary>Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time.
        /// </summary>
        public virtual TimeSpan VisibilityTimeout { get; protected set; }
    }
}
