using System;
using System.Collections.Generic;
using AzStorage.Repositories.Core;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzStorage.Core.Queues;
using AzCoreTools.Core;
using Azure.Storage.Queues;
using AzCoreTools.Core.Validators;
using AzStorage.Core.Texting;
using Azure;
using System.Threading;
using AzCoreTools.Helpers;
using Azure.Storage.Queues.Models;
using System.Threading.Tasks;

namespace AzStorage.Repositories
{
    public class AzQueueRepository : AzStorageRepository<AzQueueRetryOptions>
    {
        protected AzQueueRepository() { }

        public AzQueueRepository(string connectionString,
            CreateResourcePolicy createTableResource = CreateResourcePolicy.OnlyFirstTime,
            AzQueueRetryOptions retryOptions = null) : base(createTableResource, retryOptions)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(connectionString, nameof(connectionString));

            ConnectionString = connectionString;
        }

        #region Properties

        protected QueueServiceClient _QueueServiceClient;
        protected virtual QueueServiceClient QueueServiceClient
        {
            get
            {
                if (_QueueServiceClient == null)
                    _QueueServiceClient = CreateQueueServiceClient();

                return _QueueServiceClient;
            }
            private set
            {
                ExThrower.ST_ThrowIfArgumentIsNull(value, nameof(Azure.Storage.Queues.QueueServiceClient));

                _QueueServiceClient = value;
            }
        }

        #endregion

        #region Protected methods

        protected virtual QueueServiceClient CreateQueueServiceClient()
        {
            ThrowIfConnectionStringIsInvalid();

            return new QueueServiceClient(ConnectionString, CreateClientOptions<AzQueueClientOptions>());
        }

        protected QueueClient _QueueClient;
        //protected virtual QueueClient GetQueueClient<T>(string queueName = default) where T : class, new()
        //{
        //    return GetQueueClient(GetQueueName<T>(queueName));
        //}

        protected virtual QueueClient GetQueueClient(string queueName)
        {
            var response = CreateOrLoadQueueClient(queueName);
            if (response != null && !ResponseValidator.ResponseSucceeded<Response>(response.GetRawResponse()))
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Can_not_load_create_queue);

            return _QueueClient;
        }

        //protected virtual string GetQueueName<T>(string queueName = default)
        //{
        //    if (string.IsNullOrEmpty(queueName))
        //        queueName = typeof(T).Name.ToLower();

        //    return queueName;
        //}

        #endregion

        #region QueueClient creator

        //protected virtual Response<QueueClient> CreateOrLoadQueueClient<T>(string queueName = default) where T : class
        //{
        //    return CreateOrLoadQueueClient(GetQueueName<T>(queueName));
        //}

        protected virtual Response<QueueClient> CreateOrLoadQueueClient(string queueName)
        {
            ThrowIfInvalidQueueName(queueName);

            return CreateOrLoadQueueClient(queueName, CreateQueueIfNotExists);
        }

        protected virtual Response<QueueClient> CreateOrLoadQueueClient(string queueName, Func<dynamic[], Response<QueueClient>> func)
        {
            if (_QueueClient == null || !queueName.Equals(_QueueClient.Name))
                SetTrueToIsFirstTime();

            bool _isFirstTime = IsFirstTimeResourceCreation;

            Response<QueueClient> response;
            var result = TryCreateResource(func, new dynamic[] { queueName, default(CancellationToken) }, ref _isFirstTime, out response);

            IsFirstTimeResourceCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceResponseSucceeded<Response<QueueClient>, QueueClient>(response))
                _QueueClient = response.Value;// CreateQueueClient(queueName);

            return response;
        }

        private Response<QueueClient> CreateQueueIfNotExists(dynamic[] @params)
        {
            return CreateQueueIfNotExists(@params[0], @params[1]);
        }

        private Response<QueueClient> CreateQueueIfNotExists(string queueName, CancellationToken cancellationToken)
        {
            return QueueServiceClient.CreateQueue(queueName, cancellationToken: cancellationToken);
        }

        //protected virtual QueueClient CreateQueueClient(string queueName)
        //{
        //    ThrowIfConnectionStringIsInvalid();

        //    return new QueueClient(ConnectionString, queueName, CreateClientOptions<AzQueueClientOptions>());
        //}

        protected virtual AzStorageResponse<string> SerializeObject<T>(
            T messageToSerialize,
            Func<T, string> serializer)
        {
            string messageContent;
            try
            {
                messageContent = serializer(messageToSerialize);
            }
            catch (Exception e)
            {
                return new AzStorageResponse<string>
                {
                    Succeeded = false,
                    Exception = e,
                    Message = $"'{nameof(serializer)}' throw exception serializing object."
                };
            }

            if (messageContent == null)
                return new AzStorageResponse<string>
                {
                    Succeeded = false,
                    Message = $"'{nameof(serializer)}' returned null by serializing the object."
                };

            return AzStorageResponse<string>.Create(messageContent, true);
        }
        
        protected virtual AzStorageResponse<T> DeserializeObject<T>(
            AzStorageResponse<string> _strAzStorageResponse,
            Func<string, T> deserializer)
        {
            T entity;
            try
            {
                entity = deserializer(_strAzStorageResponse.Value);
            }
            catch (Exception e)
            {
                return new AzStorageResponse<T>
                {
                    Succeeded = false,
                    Exception = e,
                    Message = $"'{nameof(deserializer)}' throw exception for message:{_strAzStorageResponse.Value}"
                };
            }

            if (entity == null)
                return new AzStorageResponse<T>
                {
                    Succeeded = false,
                    Message = $"'{nameof(deserializer)}' return null for message:{_strAzStorageResponse.Value}"
                };

            return _strAzStorageResponse.InduceResponse(entity);
        }

        #endregion

        #region Throws

        protected virtual string GetValidQueueNameOrThrowIfInvalid(string queueName)
        {
            if (queueName == null)
            {
                ThrowIfInvalidQueueName(DefaultQueueName, nameof(DefaultQueueName), 
                    $"'{nameof(queueName)}' and '{nameof(DefaultQueueName)}' have invalid values");

                return DefaultQueueName;
            }

            ThrowIfInvalidQueueName(queueName, nameof(queueName), $"'{nameof(queueName)}' has invalid value");

            return queueName;
        }

        protected virtual void ThrowIfInvalidQueueName(string queueName, string paramName = null, string message = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(queueName, paramName, message);
        }

        protected virtual void ThrowIfInvalidMessageContent(string messageContent)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(messageContent, nameof(messageContent));
        }

        protected virtual void ThrowIfInvalidSerializer<T>(Func<T, string> serializer)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(serializer, nameof(serializer));
        }
        
        protected virtual void ThrowIfInvalidDeserializer<T>(Func<string, T> deserializer)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(deserializer, nameof(deserializer));
        }

        protected virtual AzStorageResponse<string> InduceVerifiedQueueResponse<GenTIn>(
            AzStorageResponse<GenTIn> azStorageResponse,
            Func<AzStorageResponse<GenTIn>, string> getValueFunc)
            where GenTIn : class
        {
            if (azStorageResponse.Succeeded)
                azStorageResponse.Succeeded = azStorageResponse.Value != default;

            if (!azStorageResponse.Succeeded)
                return azStorageResponse.InduceResponse(default(string));

            return azStorageResponse.InduceResponse(getValueFunc(azStorageResponse));
        }

        #endregion

        #region Default queue name

        protected string _defaultQueueName;
        public virtual string DefaultQueueName
        {
            get 
            {
                return _defaultQueueName;
            }
            set
            {
                ThrowIfInvalidQueueName(value);
                _defaultQueueName = value;
            }
        }

        #endregion

        #region Put

        public virtual AzStorageResponse<SendReceipt> SendMessage<T>(
            T messageToSerialize,
            Func<T, string> serializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidSerializer(serializer);

            var _strAzStorageResponse = SerializeObject(messageToSerialize, serializer);
            if (!_strAzStorageResponse.Succeeded)
                return _strAzStorageResponse.InduceResponse<SendReceipt>(default);

            return SendMessage(_strAzStorageResponse.Value, queueName,
                visibilityTimeout, timeToLive, cancellationToken);
        }

        public virtual AzStorageResponse<SendReceipt> SendMessage(
            string messageContent,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidMessageContent(messageContent);
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<string, TimeSpan?, TimeSpan?, CancellationToken, Response<SendReceipt>, AzStorageResponse<SendReceipt>, SendReceipt>(
                GetQueueClient(queueName).SendMessage, messageContent, visibilityTimeout, timeToLive, cancellationToken);
        }

        #endregion

        #region Get

        public virtual AzStorageResponse<T> ReceiveMessage<T>(
            Func<string, T> deserializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = ReceiveMessage(queueName, visibilityTimeout, cancellationToken);
            if (!_strAzStorageResponse.Succeeded)
                return _strAzStorageResponse.InduceResponse<T>(default);

            return DeserializeObject(_strAzStorageResponse, deserializer);
        }

        public virtual AzStorageResponse<string> ReceiveMessage(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            var _msgAzStorageResponse = ReceiveRawMessage(queueName, visibilityTimeout, cancellationToken);

            return InduceVerifiedQueueResponse(_msgAzStorageResponse,
                azStorageResponse => azStorageResponse.Value.Body.ToString());
        }

        public virtual AzStorageResponse<QueueMessage> ReceiveRawMessage(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<TimeSpan?, CancellationToken, Response<QueueMessage>, AzStorageResponse<QueueMessage>, QueueMessage>(
                GetQueueClient(queueName).ReceiveMessage, visibilityTimeout, cancellationToken);
        }
        
        #endregion

        #region Peek

        public virtual AzStorageResponse<T> PeekMessage<T>(
            Func<string, T> deserializer,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = PeekMessage(queueName, cancellationToken);
            if (!_strAzStorageResponse.Succeeded)
                return _strAzStorageResponse.InduceResponse<T>(default);

            return DeserializeObject(_strAzStorageResponse, deserializer);
        }

        public virtual AzStorageResponse<string> PeekMessage(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            var _msgAzStorageResponse = PeekRawMessage(queueName, cancellationToken);

            return InduceVerifiedQueueResponse(_msgAzStorageResponse,
                azStorageResponse => azStorageResponse.Value.Body.ToString());
        }
        
        public virtual AzStorageResponse<PeekedMessage> PeekRawMessage(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<CancellationToken, Response<PeekedMessage>, AzStorageResponse<PeekedMessage>, PeekedMessage>(
                GetQueueClient(queueName).PeekMessage, cancellationToken);
        }

        #endregion

        #region Delete queue

        public virtual AzStorageResponse DeleteQueueIfExists(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return FuncHelper.Execute<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                GetQueueClient(queueName).DeleteIfExists, cancellationToken).InduceGenericLessResponse();
        }

        #endregion

        #region Delete queue async

        public virtual async Task<AzStorageResponse> DeleteQueueAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return (await FuncHelper.ExecuteAsync<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                GetQueueClient(queueName).DeleteIfExistsAsync, cancellationToken)).InduceGenericLessResponse();
        }

        #endregion
    }
}
