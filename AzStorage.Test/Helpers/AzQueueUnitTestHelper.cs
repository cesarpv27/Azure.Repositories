using AzCoreTools.Core;
using AzStorage.Repositories;
using Azure;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Linq;
using AzCoreTools.Core.Validators;
using System.Threading.Tasks;
using CoreTools.Extensions;
using AzStorage.Core.Queues;
using AzStorage.Test.Utilities;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using Azure.Storage.Queues;
using System.Threading;

namespace AzStorage.Test.Helpers
{
    internal class AzQueueUnitTestHelper : AzStorageUnitTestHelper
    {
        #region Miscellaneous methods

        public static AzQueueRepository GetOrCreateAzQueueRepository(
            bool base64Encoding,
            CreateResourcePolicy optionCreateIfNotExist)
        {
            AzQueueRepository _azQueueRepository;
            if (base64Encoding)
                _azQueueRepository = GetOrCreateAzQueueRepositoryBase64(optionCreateIfNotExist);
            else
                _azQueueRepository = GetOrCreateAzQueueRepository(optionCreateIfNotExist);

            return _azQueueRepository;
        }

        private static AzQueueRepository _AzQueueRepository;
        public static AzQueueRepository GetOrCreateAzQueueRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            if (_AzQueueRepository == null)
                _AzQueueRepository = CreateAzQueueRepository(optionCreateIfNotExist);

            return _AzQueueRepository;
        }

        private static AzQueueRepository CreateAzQueueRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            var _azQueueRetryOptions = new AzQueueRetryOptions
            {
                Mode = Azure.Core.RetryMode.Fixed,
                Delay = new TimeSpan(0, 0, 1),
                MaxDelay = new TimeSpan(0, 1, 0),
                NetworkTimeout = new TimeSpan(0, 3, 0),
                MaxRetries = 5,
            };

            return new AzQueueRepository(StorageConnectionString, optionCreateIfNotExist, retryOptions: _azQueueRetryOptions);
        }

        private static AzQueueRepository _AzQueueRepositoryBase64;
        public static AzQueueRepository GetOrCreateAzQueueRepositoryBase64(CreateResourcePolicy optionCreateIfNotExist)
        {
            if (_AzQueueRepositoryBase64 == null)
                _AzQueueRepositoryBase64 = CreateAzQueueRepositoryBase64(optionCreateIfNotExist);

            return _AzQueueRepositoryBase64;
        }

        private static AzQueueRepository CreateAzQueueRepositoryBase64(CreateResourcePolicy optionCreateIfNotExist)
        {
            var _azQueueRetryOptions = new AzQueueRetryOptions
            {
                Mode = Azure.Core.RetryMode.Fixed,
                Delay = new TimeSpan(0, 0, 1),
                MaxDelay = new TimeSpan(0, 1, 0),
                NetworkTimeout = new TimeSpan(0, 3, 0),
                MaxRetries = 5,
            };

            var _azQueueClientOptions = new AzQueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };

            return new AzQueueRepository(StorageConnectionString, optionCreateIfNotExist, _azQueueClientOptions, _azQueueRetryOptions);
        }

        public static SampleQueueEntity CreateSomeEntity()
        {
            return new SampleQueueEntity();
        }

        public static string GetDefaultQueueName => "samplequeue";

        public static void SendAssertMessage(
            string messageText,
            string queueName)
        {
            var _sendMessageResponseAct = SendMessage(messageText, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        }

        public static void SendAssertMessage(
            SampleQueueEntity messageToSerialize,
            string queueName)
        {
            var _sendMessageResponseAct = SendMessageEntity(messageToSerialize, JsonConvert.SerializeObject, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        }

        public static string SendAssertMessageRandomQueueName(string messageText, bool appendId = true)
        {
            var rdm = new Random();
            var id = rdm.Next(1, int.MaxValue);
            string queueName = GetRandomQueueNameFromDefault(id);

            if (appendId)
                messageText += $" - { id}";

            var _sendMessageResponseAct = SendMessage(messageText, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);

            return queueName;
        }

        public static string GenerateSendAssertMessageRandomQueueName(
            out SampleQueueEntity messageToSerialize,
            bool base64Encoding = false)
        {
            messageToSerialize = GenerateDefaultSampleQueueEntity();
            return SendAssertMessageRandomQueueName(messageToSerialize, base64Encoding);
        }

        public static string GenerateSendAssertMessagesRandomQueueName(
            int maxMessages,
            out List<SampleQueueEntity> messagesToSerialize,
            bool base64Encoding = false,
            bool randomEntity = true)
        {
            string queueName = GetRandomQueueNameFromDefault();

            SampleQueueEntity tmpSampleQueueEntity;
            messagesToSerialize = new List<SampleQueueEntity>(maxMessages);
            for (int i = 0; i < maxMessages; i++)
            {
                if (randomEntity)
                    tmpSampleQueueEntity = GenerateRandomSampleQueueEntity();
                else
                    tmpSampleQueueEntity = GenerateDefaultSampleQueueEntity();

                messagesToSerialize.Add(tmpSampleQueueEntity);

                var _sendMessageResponseAct = SendMessageEntity(tmpSampleQueueEntity, JsonConvert.SerializeObject,
                    queueName, base64Encoding);
                UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);
            }

            return queueName;
        }

        public static string GenerateSendAssertMessagesRandomQueueName(
            int maxMessages,
            out List<ReceiptMetadata> messagesToSerialize,
            bool base64Encoding = false,
            bool randomEntity = true)
        {
            string queueName = GetRandomQueueNameFromDefault();

            SampleQueueEntity tmpSampleQueueEntity;
            messagesToSerialize = new List<ReceiptMetadata>(maxMessages);
            for (int i = 0; i < maxMessages; i++)
            {
                if (randomEntity)
                    tmpSampleQueueEntity = GenerateRandomSampleQueueEntity();
                else
                    tmpSampleQueueEntity = GenerateDefaultSampleQueueEntity();

                var _sendMessageResponseAct = SendMessageEntity(tmpSampleQueueEntity, JsonConvert.SerializeObject,
                    queueName, base64Encoding);
                UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);

                messagesToSerialize.Add(new ReceiptMetadata(_sendMessageResponseAct));
            }

            return queueName;
        }

        public static string GenerateSendAssertMessageRandomQueueName(
            out string messageId,
            out string popReceipt,
            bool base64Encoding = false,
            bool randomEntity = true)
        {
            string queueName = GetRandomQueueNameFromDefault();

            SampleQueueEntity tmpSampleQueueEntity;
            if (randomEntity)
                tmpSampleQueueEntity = GenerateRandomSampleQueueEntity();
            else
                tmpSampleQueueEntity = GenerateDefaultSampleQueueEntity();

            var _sendMessageResponseAct = SendMessageEntity(tmpSampleQueueEntity, JsonConvert.SerializeObject,
                queueName, base64Encoding);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_sendMessageResponseAct);

            messageId = _sendMessageResponseAct.Value.MessageId;
            popReceipt = _sendMessageResponseAct.Value.PopReceipt;

            return queueName;
        }

        public static List<SampleQueueEntity> GenerateMessagesRandom(
            int maxMessages,
            bool randomEntity = true)
        {
            SampleQueueEntity tmpSampleQueueEntity;
            var messagesToSerialize = new List<SampleQueueEntity>(maxMessages);
            for (int i = 0; i < maxMessages; i++)
            {
                if (randomEntity)
                    tmpSampleQueueEntity = GenerateRandomSampleQueueEntity();
                else
                    tmpSampleQueueEntity = GenerateDefaultSampleQueueEntity();

                messagesToSerialize.Add(tmpSampleQueueEntity);
            }

            return messagesToSerialize;
        }

        public static string SendAssertMessageRandomQueueName(
            SampleQueueEntity messageToSerialize,
            bool base64Encoding = false)
        {
            string queueName = GetRandomQueueNameFromDefault();

            var _sendMessageResponseAct = SendMessageEntity(messageToSerialize, JsonConvert.SerializeObject, queueName, base64Encoding);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);

            return queueName;
        }

        public static void AssertSampleQueueEntityFromResponse(
            AzStorageResponse<SampleQueueEntity> azStorageResponse,
            SampleQueueEntity sampleQueueEntity)
        {
            Assert.NotNull(azStorageResponse.Value);

            var responseEntity = azStorageResponse.Value;
            Assert.Equal(sampleQueueEntity.Prop1, responseEntity.Prop1);
            Assert.Equal(sampleQueueEntity.Prop2, responseEntity.Prop2);
        }

        public static void AssertSamplesQueueEntityFromResponse(
            AzStorageResponse<List<SampleQueueEntity>> azStorageResponse,
            List<SampleQueueEntity> samplesQueueEntity)
        {
            Assert.NotNull(azStorageResponse.Value);

            var responseEntities = azStorageResponse.Value;
            foreach (var ent in responseEntities)
                Assert.True(samplesQueueEntity.Where(sqe => sqe.Prop1 == ent.Prop1 && sqe.Prop2 == ent.Prop2).Any());
        }

        public static SampleQueueEntity GenerateDefaultSampleQueueEntity()
        {
            return new SampleQueueEntity { Prop1 = "Queue_Prop1", Prop2 = 2 };
        }
        
        public static SampleQueueEntity GenerateRandomSampleQueueEntity()
        {
            return new SampleQueueEntity { Prop1 = Guid.NewGuid().ToString(), Prop2 = new Random().Next(1, int.MaxValue) };
        }

        public static string GetRandomQueueNameFromDefault(int randomValue = -1)
        {
            return BuildRandomName(GetDefaultQueueName, randomValue);
        }

        public static List<ExpandedReceiptMetadata> GenerateExpandedReceiptMetadataList(
            List<ReceiptMetadata> receiptsMetadata,
            TimeSpan commonVisibilityTimeout = default,
            bool binaryData = false)
        {
            var result = new List<ExpandedReceiptMetadata>(receiptsMetadata.Count);
            string commonUpdatedText = "Updated", currentMessageText;
            int count = 0;
            if (commonVisibilityTimeout == default)
                commonVisibilityTimeout = new TimeSpan(0, 0, 5);
            foreach (var receipt in receiptsMetadata)
            {
                currentMessageText = $"{commonUpdatedText} -> {++count}";
                result.Add(
                    binaryData ?
                    new ExpandedReceiptMetadata(receipt.MessageId, receipt.PopReceipt,
                    BinaryData.FromString(currentMessageText), commonVisibilityTimeout)
                    :
                    new ExpandedReceiptMetadata(receipt.MessageId, receipt.PopReceipt,
                    currentMessageText, commonVisibilityTimeout));
            }

            return result;
        }

        #endregion

        #region AzQueueRepository methods

        #region Put (sync & async)

        public static AzStorageResponse<SendReceipt> SendMessageEntity(
            SampleQueueEntity sampleQueueEntity,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessageEntity(sampleQueueEntity, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessageEntity(
            SampleQueueEntity sampleQueueEntity,
            Func<SampleQueueEntity, string> serializer,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist).SendMessageEntity(sampleQueueEntity,
                serializer, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessage(
            string messageText,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).SendMessage(messageText, queueName);
        }

        public static AzStorageResponse<SendReceipt> SendMessage(
            BinaryData message,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .SendMessage(message, queueName);
        }

        public static async Task<AzStorageResponse<SendReceipt>> SendMessageEntityAsync(
            SampleQueueEntity sampleQueueEntity,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessageEntityAsync(sampleQueueEntity, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<SendReceipt>> SendMessageAsync(
            BinaryData message,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .SendMessageAsync(message, queueName);
        }

        #endregion

        #region Put messages (sync & async)

        public static List<AzStorageResponse<SendReceipt>> SendMessageEntities(
            IEnumerable<SampleQueueEntity> messages,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessageEntities(messages, queueName, encodeCaseMessageEncoding: base64Encoding);
        }

        public static List<AzStorageResponse<SendReceipt>> SendMessages(
            IEnumerable<string> messages,
            string queueName,
            bool base64Encoding = false,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessages(messages, queueName, cancellationToken: cancellationToken, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<List<AzStorageResponse<SendReceipt>>> SendMessageEntitiesAsync(
            IEnumerable<SampleQueueEntity> messages,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessageEntitiesAsync(messages, queueName, encodeCaseMessageEncoding: base64Encoding);
        }

        public static async Task<List<AzStorageResponse<SendReceipt>>> SendMessagesAsync(
            IEnumerable<string> messages,
            string queueName,
            bool base64Encoding = false,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessagesAsync(messages, queueName, cancellationToken: cancellationToken, encodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Get (sync & async)

        public static AzStorageResponse<SampleQueueEntity> ReceiveMessageEntity(
            string queueName,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessageEntity<SampleQueueEntity>(queueName, visibilityTimeout, 
                decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SampleQueueEntity> ReceiveMessageEntity(
            Func<string, SampleQueueEntity> deserializer,
            string queueName,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).ReceiveMessageEntity(deserializer, 
                queueName, visibilityTimeout);
        }

        public static AzStorageResponse<QueueMessage> ReceiveRawMessage(
            string queueName,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).ReceiveRawMessage(queueName, visibilityTimeout);
        }

        public static async Task<AzStorageResponse<SampleQueueEntity>> ReceiveMessageEntityAsync(
            string queueName,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessageEntityAsync<SampleQueueEntity>(queueName, visibilityTimeout,
                decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Get messages (sync & async)

        public static AzStorageResponse<List<SampleQueueEntity>> ReceiveMessageEntities(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessageEntities<SampleQueueEntity>(maxMessages, queueName,
                visibilityTimeout, decodeCaseMessageEncoding: base64Encoding);
        }

        public static async Task<AzStorageResponse<List<SampleQueueEntity>>> ReceiveMessageEntitiesAsync(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessageEntitiesAsync<SampleQueueEntity>(maxMessages, queueName,
                visibilityTimeout, decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Peek (sync & async)

        public static AzStorageResponse<PeekedMessage> PeekRawMessage(
            string queueName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).PeekRawMessage(queueName);
        }

        public static AzStorageResponse<SampleQueueEntity> PeekMessageEntity(
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessageEntity<SampleQueueEntity>(queueName, decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<SampleQueueEntity>> PeekMessageEntityAsync(
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessageEntityAsync<SampleQueueEntity>(queueName, decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Peek messages (sync & async)

        public static AzStorageResponse<List<SampleQueueEntity>> PeekMessageEntities(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessageEntities<SampleQueueEntity>(maxMessages, queueName, decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<List<SampleQueueEntity>>> PeekMessageEntitiesAsync(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessageEntitiesAsync<SampleQueueEntity>(maxMessages, queueName, decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Update message (sync & async)

        public static AzStorageResponse<UpdateReceipt> UpdateMessage(
            ReceiptMetadata receiptMetadata,
            string queueName,
            string messageText = null,
            TimeSpan visibilityTimeout = default,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .UpdateMessage(receiptMetadata, messageText, queueName, visibilityTimeout,
                encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ReceiptMetadata receiptMetadata,
            string queueName,
            string messageText = null,
            TimeSpan visibilityTimeout = default,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .UpdateMessageAsync(receiptMetadata, messageText, queueName, visibilityTimeout,
                encodeCaseMessageEncoding: base64Encoding);
        }

        public static AzStorageResponse<UpdateReceipt> UpdateMessage(
            ExpandedReceiptMetadata expandedReceiptMetadata,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessage(expandedReceiptMetadata, queueName);
        }

        public static async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ExpandedReceiptMetadata expandedReceiptMetadata,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessageAsync(expandedReceiptMetadata, queueName);
        }

        public static AzStorageResponse<UpdateReceipt> UpdateMessage(
            ReceiptMetadata receiptMetadata,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessage(receiptMetadata, message, queueName, visibilityTimeout);
        }
        
        public static async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ReceiptMetadata receiptMetadata,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessageAsync(receiptMetadata, message, queueName, visibilityTimeout);
        }

        #endregion

        #region Update messages (sync & async)

        public static List<AzStorageResponse<UpdateReceipt>> UpdateMessages(
           IEnumerable<ExpandedReceiptMetadata> expReceiptsMetadata,
            string queueName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessages(expReceiptsMetadata, queueName, cancellationToken: cancellationToken);
        }
        
        public static async Task<List<AzStorageResponse<UpdateReceipt>>> UpdateMessagesAsync(
           IEnumerable<ExpandedReceiptMetadata> expReceiptsMetadata,
            string queueName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .UpdateMessagesAsync(expReceiptsMetadata, queueName, cancellationToken: cancellationToken);
        }

        #endregion

        #region Delete message (sync & async)

        public static AzStorageResponse DeleteMessage(
            string messageId,
            string popReceipt,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessage(messageId, popReceipt, queueName);
        }
        
        public static async Task<AzStorageResponse> DeleteMessageAsync(
            string messageId,
            string popReceipt,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessageAsync(messageId, popReceipt, queueName);
        }

        public static AzStorageResponse DeleteMessage(
            ReceiptMetadata receiptMetadata,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessage(receiptMetadata, queueName);
        }

        public static async Task<AzStorageResponse> DeleteMessageAsync(
            ReceiptMetadata receiptMetadata,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessageAsync(receiptMetadata, queueName);
        }

        #endregion

        #region Delete messages (sync & async)

        public static List<AzStorageResponse> DeleteMessages(
            IEnumerable<ReceiptMetadata> receiptsMetadata,
            string queueName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessages(receiptsMetadata, queueName, cancellationToken: cancellationToken);
        }

        public static async Task<List<AzStorageResponse>> DeleteMessagesAsync(
            IEnumerable<ReceiptMetadata> receiptsMetadata,
            string queueName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .DeleteMessagesAsync(receiptsMetadata, queueName, cancellationToken: cancellationToken);
        }

        #endregion

        #region Clear messages (sync & async)

        public static AzStorageResponse ClearMessages(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).ClearMessages(queueName);
        }

        public static async Task<AzStorageResponse> ClearMessagesAsync(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist).ClearMessagesAsync(queueName);
        }

        #endregion

        #region GetAccountName

        public static AzStorageResponse<string> GetAccountName(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).GetAccountName(queueName);
        }

        #endregion
        
        #region GetMaxPeekableMessages

        public static AzStorageResponse<int> GetMaxPeekableMessages(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).GetMaxPeekableMessages(queueName);
        }

        #endregion

        #region GetMessageMaxBytes

        public static AzStorageResponse<int> GetMessageMaxBytes(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).GetMessageMaxBytes(queueName);
        }

        #endregion

        #region GetProperties (sync & async)

        public static AzStorageResponse<QueueProperties> GetProperties(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).GetProperties(queueName);
        }
        
        public static async Task<AzStorageResponse<QueueProperties>> GetPropertiesAsync(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist).GetPropertiesAsync(queueName);
        }

        #endregion
        
        #region CreateQueueIfNotExists

        public static AzStorageResponse CreateQueueIfNotExists(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).CreateQueueIfNotExists(queueName);
        }

        #endregion

        #region Exists queue (sync & async)

        public static AzStorageResponse Exists(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).Exists(queueName);
        }
        
        public static async Task<AzStorageResponse> ExistsAsync(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(optionCreateIfNotExist).ExistsAsync(queueName);
        }

        #endregion

        #region DeleteQueueIfExists

        public static AzStorageResponse DeleteQueueIfExists(
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).DeleteQueueIfExists(queueName);
        }

        #endregion

        #endregion
    }
}
