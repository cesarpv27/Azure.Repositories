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

        public static string GetDefaultQueueName
        {
            get
            {
                return "samplequeue";
            }
        }

        public static void SendAssertMessage(
            string messageContent,
            string queueName)
        {
            var _sendMessageResponseAct = SendMessage(messageContent, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        }

        public static void SendAssertMessage(
            SampleQueueEntity messageToSerialize,
            string queueName)
        {
            var _sendMessageResponseAct = SendMessage(messageToSerialize, JsonConvert.SerializeObject, queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        }

        public static string SendAssertMessageRandomQueueName(string messageContent, bool appendId = true)
        {
            var rdm = new Random();
            var id = rdm.Next(1, int.MaxValue);
            string queueName = GetRandomQueueNameFromDefault(id);

            if (appendId)
                messageContent += $" - { id}";

            var _sendMessageResponseAct = SendMessage(messageContent, queueName);
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

                var _sendMessageResponseAct = SendMessage(tmpSampleQueueEntity, JsonConvert.SerializeObject,
                    queueName, base64Encoding);
                UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
            }

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

            var _sendMessageResponseAct = SendMessage(messageToSerialize, JsonConvert.SerializeObject, queueName, base64Encoding);
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
            int id = randomValue;
            if (randomValue < 0)
            {
                var rdm = new Random();
                id = rdm.Next(1, int.MaxValue);
            }
            return GetDefaultQueueName + id;
        }

        #endregion

        #region AzQueueRepository methods

        #region Put (sync & async)

        public static AzStorageResponse<SendReceipt> SendMessage(
            SampleQueueEntity sampleQueueEntity,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessage(sampleQueueEntity, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessage(
            SampleQueueEntity sampleQueueEntity,
            Func<SampleQueueEntity, string> serializer,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist).SendMessage(sampleQueueEntity,
                serializer, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessage(
            string messageContent,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).SendMessage(messageContent, queueName);
        }

        public static async Task<AzStorageResponse<SendReceipt>> SendMessageAsync(
            SampleQueueEntity sampleQueueEntity,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessageAsync(sampleQueueEntity, queueName, encodeCaseMessageEncoding: base64Encoding);
        }

        public static List<AzStorageResponse<SendReceipt>> SendMessages(
            IEnumerable<SampleQueueEntity> messages,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessages(messages, queueName, encodeCaseMessageEncoding: base64Encoding);
        }
        
        public static List<AzStorageResponse<SendReceipt>> SendMessages(
            IEnumerable<string> messages,
            string queueName,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .SendMessages(messages, queueName, encodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Get (sync & async)

        public static AzStorageResponse<SampleQueueEntity> ReceiveMessage(
            string queueName,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessage<SampleQueueEntity>(queueName, visibilityTimeout, 
                decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static AzStorageResponse<SampleQueueEntity> ReceiveMessage(
            Func<string, SampleQueueEntity> deserializer,
            string queueName,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).ReceiveMessage(deserializer, 
                queueName, visibilityTimeout);
        }

        public static AzStorageResponse<QueueMessage> ReceiveRawMessage(
            string queueName,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).ReceiveRawMessage(queueName, visibilityTimeout);
        }

        public static async Task<AzStorageResponse<SampleQueueEntity>> ReceiveMessageAsync(
            string queueName,
            bool base64Encoding = false,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .ReceiveMessageAsync<SampleQueueEntity>(queueName, visibilityTimeout,
                decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Peek (sync & async)

        public static AzStorageResponse<PeekedMessage> PeekRawMessage(
            string queueName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).PeekRawMessage(queueName);
        }

        public static AzStorageResponse<SampleQueueEntity> PeekMessage(
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessage<SampleQueueEntity>(queueName, decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<SampleQueueEntity>> PeekMessageAsync(
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessageAsync<SampleQueueEntity>(queueName, decodeCaseMessageEncoding: base64Encoding);
        }

        #endregion

        #region Peek messages (sync & async)

        public static AzStorageResponse<List<SampleQueueEntity>> PeekMessages(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessages<SampleQueueEntity>(maxMessages, queueName, decodeCaseMessageEncoding: base64Encoding);
        }
        
        public static async Task<AzStorageResponse<List<SampleQueueEntity>>> PeekMessagesAsync(
            int maxMessages,
            string queueName = null,
            bool base64Encoding = false,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzQueueRepository(base64Encoding, optionCreateIfNotExist)
                .PeekMessagesAsync<SampleQueueEntity>(maxMessages, queueName, decodeCaseMessageEncoding: base64Encoding);
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
