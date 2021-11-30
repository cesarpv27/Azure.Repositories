using AzCoreTools.Core;
using Azure.Storage.Queues.Models;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Queues
{
    public class ReceiptMetadata
    {
        protected ReceiptMetadata() { }

        public ReceiptMetadata(AzStorageResponse<SendReceipt> azStorageResponse)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(azStorageResponse, nameof(azStorageResponse), nameof(azStorageResponse));
            if (azStorageResponse.Value == null)
                ExThrower.ST_ThrowInvalidOperationException($"{nameof(azStorageResponse)}.{nameof(azStorageResponse.Value)} is null");

            Initialize(azStorageResponse.Value.MessageId, azStorageResponse.Value.PopReceipt);
        }

        public ReceiptMetadata(string messageId, string popReceipt)
        {
            Initialize(messageId, popReceipt);
        }

        protected virtual void Initialize(string messageId, string popReceipt)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(messageId, nameof(messageId), nameof(messageId));
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(popReceipt, nameof(popReceipt), nameof(popReceipt));

            MessageId = messageId;
            PopReceipt = popReceipt;
        }

        /// <summary>
        /// The Id of the Message.
        /// </summary>
        public virtual string MessageId { get; protected set; }

        /// <summary>
        /// This value is required to delete the Message. If deletion fails using this popreceipt
        /// then the message has been dequeued by another client.
        /// </summary>
        public virtual string PopReceipt { get; protected set; }
    }
}
