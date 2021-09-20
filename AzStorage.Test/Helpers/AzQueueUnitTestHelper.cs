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

        public static SampleQueueEntity GenerateDefaultSampleQueueEntity()
        {
            return new SampleQueueEntity { Prop1 = "Queue_Prop1", Prop2 = 2 };
        }

        #endregion

        #region AzQueueRepository methods

        #region Put

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

        public static AzStorageResponse<string> PeekMessage(
            string queueName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzQueueRepository(optionCreateIfNotExist).PeekMessage(queueName);
        }

        #endregion

        #region Delete queue

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
