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

namespace AzStorage.Test.Helpers
{
    internal class AzQueueUnitTestHelper : AzStorageUnitTestHelper
    {
        #region Miscellaneous methods

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

            return new AzQueueRepository(StorageConnectionString, optionCreateIfNotExist, _azQueueRetryOptions);
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
        
        public static string SendAssertMessageRandomQueueName(SampleQueueEntity messageToSerialize)
        {
            string queueName = GetRandomQueueNameFromDefault();

            var _sendMessageResponseAct = SendMessage(messageToSerialize, JsonConvert.SerializeObject, queueName);
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

        public static SampleQueueEntity GenerateDefaultSampleQueueEntity()
        {
            return new SampleQueueEntity { Prop1 = "Queue_Prop1", Prop2 = 2 };
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

        #region Put

        public static AzStorageResponse<SendReceipt> SendMessageJsonSerializer(
            SampleQueueEntity sampleQueueEntity,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).SendMessageJsonSerializer(sampleQueueEntity,
                queueName);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessage(
            SampleQueueEntity sampleQueueEntity,
            Func<SampleQueueEntity, string> serializer,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).SendMessage(sampleQueueEntity,
                serializer, queueName);
        }
        
        public static AzStorageResponse<SendReceipt> SendMessage(
            string messageContent,
            string queueName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).SendMessage(messageContent, queueName);
        }

        #endregion

        #region Get

        public static AzStorageResponse<SampleQueueEntity> ReceiveMessageJsonDeserializer(
            string queueName,
            TimeSpan? visibilityTimeout = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .ReceiveMessageJsonDeserializer<SampleQueueEntity>(queueName, visibilityTimeout);
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

        #endregion

        #region Peek

        public static AzStorageResponse<SampleQueueEntity> PeekMessageJsonDeserializer(
            string queueName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist)
                .PeekMessageJsonDeserializer<SampleQueueEntity>(queueName);
        }
        
        public static AzStorageResponse<PeekedMessage> PeekRawMessage(
            string queueName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).PeekRawMessage(queueName);
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
