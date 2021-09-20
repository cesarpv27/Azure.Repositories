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
using Newtonsoft.Json;

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

        protected virtual string GetRawMessageContent<GenTIn>(AzStorageResponse<GenTIn> azStorageResponse,
            Func<GenTIn, BinaryData> getBody)
        {
            if (azStorageResponse.Value == null)
                return default;
            
            return getBody(azStorageResponse.Value).ToString();
        }

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
            return CreateQueueFromQueueServiceClient(@params[0], @params[1]);
        }

        private Response<QueueClient> CreateQueueFromQueueServiceClient(string queueName, CancellationToken cancellationToken)
        {
            return QueueServiceClient.CreateQueue(queueName, cancellationToken: cancellationToken);
        }

        protected virtual QueueClient CreateQueueClient(string queueName)
        {
            ThrowIfConnectionStringIsInvalid();

            return new QueueClient(ConnectionString, queueName, CreateClientOptions<AzQueueClientOptions>());
        }

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
            if (_strAzStorageResponse.Value == default)
                return _strAzStorageResponse.InduceResponse(default(T));

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
            Func<GenTIn, BinaryData> getBodyFunc)
            where GenTIn : class
        {
            if (!azStorageResponse.Succeeded)
                return azStorageResponse.InduceResponse(default(string));

            var strContent = GetRawMessageContent(azStorageResponse, getBodyFunc);

            return azStorageResponse.InduceResponse(strContent);
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

        #region AccountName

        /// <summary>
        /// Gets the Storage account name corresponding to the queue client.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <returns>The <see cref="AzStorageResponse{int}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<string> GetAccountName(string queueName)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            try
            {
                return AzStorageResponse<string>.Create(CreateQueueClient(queueName).AccountName, true);
            }
            catch (Exception e)
            {
                return AzStorageResponse<string>.Create(e);
            }
        }

        #endregion
        
        #region MaxPeekableMessages

        /// <summary>
        /// Indicates the maximum number of messages you can retrieve with each call to peek message methods.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <returns>The <see cref="AzStorageResponse{int}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<int> GetMaxPeekableMessages(string queueName)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            try
            {
                return AzStorageResponse<int>.Create(CreateQueueClient(queueName).MaxPeekableMessages, true);
            }
            catch (Exception e)
            {
                return AzStorageResponse<int>.Create(e);
            }
        }

        #endregion

        #region MessageMaxBytes

        /// <summary>
        /// Gets the maximum number of bytes allowed for a message's UTF-8 text.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <returns>The <see cref="AzStorageResponse{int}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<int> GetMessageMaxBytes(string queueName)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            try
            {
                return AzStorageResponse<int>.Create(CreateQueueClient(queueName).MessageMaxBytes, true);
            }
            catch (Exception e)
            {
                return AzStorageResponse<int>.Create(e);
            }
        }

        #endregion

        #region Put

        public virtual AzStorageResponse<SendReceipt> SendMessageJsonSerializer<T>(
            T messageToSerialize,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default)
            where T : class
        {
            return SendMessage(messageToSerialize, JsonConvert.SerializeObject, queueName,
                visibilityTimeout, timeToLive, cancellationToken);
        }
        
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

        public virtual AzStorageResponse<T> ReceiveMessageJsonDeserializer<T>(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            return ReceiveMessage(JsonConvert.DeserializeObject<T>, queueName, visibilityTimeout, cancellationToken);
        }
        
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

            return InduceVerifiedQueueResponse(_msgAzStorageResponse, queueMsg => queueMsg.Body);
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
        
        public virtual AzStorageResponse<T> PeekMessageJsonDeserializer<T>(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            return PeekMessage(JsonConvert.DeserializeObject<T>, queueName, cancellationToken);
        }
        
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
            var _msgAzStorageResponse = PeekRawMessage(queueName, cancellationToken);

            return InduceVerifiedQueueResponse(_msgAzStorageResponse, peekedMsg => peekedMsg.Body);
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

        #region Delete message

        /// <summary>
        /// Permanently removes the specified message from its queue.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteMessage(
            string messageId, 
            string popReceipt,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(messageId, nameof(messageId), nameof(messageId));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(popReceipt, nameof(popReceipt), nameof(popReceipt));

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<string, string, CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).DeleteMessage, messageId, popReceipt, cancellationToken);
        }

        #endregion

        #region Delete message async

        /// <summary>
        /// Permanently removes the specified message from its queue.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteMessageAsync(
            string messageId,
            string popReceipt,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(messageId, nameof(messageId), nameof(messageId));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(popReceipt, nameof(popReceipt), nameof(popReceipt));

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<string, string, CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).DeleteMessageAsync, messageId, popReceipt, cancellationToken);
        }

        #endregion

        #region Clear messages

        /// <summary>
        /// Deletes all messages from a queue.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse ClearMessages(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).ClearMessages, cancellationToken);
        }

        #endregion

        #region Clear messages async

        /// <summary>
        /// Deletes all messages from a queue.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> ClearMessagesAsync(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).ClearMessagesAsync, cancellationToken);
        }

        #endregion

        #region Create queue if not exists

        /// <summary>
        /// Creates a new queue under the specified account. If the queue already exists, it is not changed.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="metadata">Optional custom metadata to set for this queue.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse CreateQueueIfNotExists(
            string queueName,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return FuncHelper.Execute< IDictionary<string, string>, CancellationToken, Response, AzStorageResponse>(
                CreateQueueClient(queueName).CreateIfNotExists, metadata, cancellationToken);
        }

        #endregion

        #region Create queue async if not exists

        /// <summary>
        /// Creates a new queue under the specified account. If the queue already exists, it is not changed.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="metadata">Optional custom metadata to set for this queue.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> CreateQueueIfNotExistsAsync(
            string queueName,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return await FuncHelper.ExecuteAsync< IDictionary<string, string>, CancellationToken, Response, AzStorageResponse>(
                CreateQueueClient(queueName).CreateIfNotExistsAsync, metadata, cancellationToken);
        }

        #endregion

        #region Exists queue

        /// <summary>
        /// Verify if a queue with specified <paramref name="queueName"/> exists on the storage account 
        /// in the storage service.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation
        /// with value of property <c>Succeeded</c> true if the queue exists.</returns>
        public virtual AzStorageResponse Exists(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return FuncHelper.Execute<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                CreateQueueClient(queueName).Exists, cancellationToken).InduceGenericLessResponse();
        }

        #endregion

        #region Exists queue async

        /// <summary>
        /// Verify if a queue with specified <paramref name="queueName"/> exists on the storage account 
        /// in the storage service.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation
        /// with value of property <c>Succeeded</c> true if the queue exists, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> ExistsAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return (await FuncHelper.ExecuteAsync<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                CreateQueueClient(queueName).ExistsAsync, cancellationToken)).InduceGenericLessResponse();
        }

        #endregion

        #region Delete queue if exists

        /// <summary>
        /// Deletes the specified queue if it exists.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteQueueIfExists(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return FuncHelper.Execute<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                CreateQueueClient(queueName).DeleteIfExists, cancellationToken).InduceGenericLessResponse();
        }

        #endregion

        #region Delete queue async if exists

        /// <summary>
        /// Deletes the specified queue if it exists.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteQueueIfExistsAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            return (await FuncHelper.ExecuteAsync<CancellationToken, Response<bool>, AzStorageResponse<bool>, bool>(
                CreateQueueClient(queueName).DeleteIfExistsAsync, cancellationToken)).InduceGenericLessResponse();
        }

        #endregion
    }
}
