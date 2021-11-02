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
using CoreTools.Extensions;
using System.Text;
using System.Linq;
using AzCoreTools.Texting;

namespace AzStorage.Repositories
{
    public class AzQueueRepository : AzStorageRepository<AzQueueRetryOptions>
    {
        protected AzQueueRepository() { }

        public AzQueueRepository(string connectionString,
            CreateResourcePolicy createTableResource = CreateResourcePolicy.OnlyFirstTime,
            AzQueueClientOptions queueClientOptions = null,
            AzQueueRetryOptions retryOptions = null) : base(createTableResource, retryOptions)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(connectionString, nameof(connectionString));

            ConnectionString = connectionString;
            AzQueueClientOptions = queueClientOptions;
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

        protected virtual AzQueueClientOptions AzQueueClientOptions { get; set; }

        protected virtual QueueServiceClient CreateQueueServiceClient()
        {
            ThrowIfInvalidConnectionString();

            return new QueueServiceClient(ConnectionString, CreateClientOptions(AzQueueClientOptions));
        }

        protected virtual QueueMessageEncoding QueueMessageEncoding
        {
            get
            {
                if (AzQueueClientOptions != null)
                    return AzQueueClientOptions.MessageEncoding;

                return QueueMessageEncoding.None;
            }
        }

        #endregion

        #region Protected methods

        protected virtual string GetMessageContent<GenTIn>(
            GenTIn rawMessage,
            Func<GenTIn, BinaryData> getBodyFunc,
            bool decodeCaseMessageEncoding)
        {
            var strMessageContent = GetMessageContent(rawMessage, getBodyFunc);

            if (decodeCaseMessageEncoding)
                strMessageContent = DecodeIfCase(strMessageContent);

            return strMessageContent;
        }

        protected virtual string GetMessageContent<GenTIn>(
            GenTIn rawMessage,
            Func<GenTIn, BinaryData> getBodyFunc)
        {
            if (rawMessage == null)
                return default;

            return getBodyFunc(rawMessage).ToString();
        }

        protected virtual string EncodeIfCase(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
                return messageText;

            switch (QueueMessageEncoding)
            {
                case QueueMessageEncoding.Base64:
                    return Encoding.UTF8.EncodeToBase64String(messageText);
                case QueueMessageEncoding.None:
                    return messageText;
                default:
                    ExThrower.ST_ThrowNotImplementedException(QueueMessageEncoding);
                    return default;
            }
        }

        protected virtual string DecodeIfCase(string messageText)
        {
            switch (QueueMessageEncoding)
            {
                case QueueMessageEncoding.Base64:
                    return Encoding.UTF8.DecodeFromBase64String(messageText);
                case QueueMessageEncoding.None:
                    return messageText;
                default:
                    ExThrower.ST_ThrowNotImplementedException(QueueMessageEncoding);
                    return default;
            }
        }

        #endregion

        #region Response management

        protected virtual AzStorageResponse<GenTOut> InduceResponseWithDefaultValue<GenTIn, GenTOut>(
            AzStorageResponse<GenTIn> azStorageResponse)
            where GenTIn : class
        {
            if (!azStorageResponse.Succeeded || azStorageResponse.Value == default(GenTIn))
                return azStorageResponse.InduceResponse<GenTOut>(default);

            return null;
        }

        protected virtual AzStorageResponse<List<string>> InduceResponse<GenTIn>(
            AzStorageResponse<GenTIn[]> azStorageResponse,
            Func<GenTIn, BinaryData> getBodyFunc,
            bool decodeCaseMessageEncoding)
            where GenTIn : class
        {
            var withDefaultValueResponse = InduceResponseWithDefaultValue<GenTIn[], List<string>>(azStorageResponse);
            if (withDefaultValueResponse != null)
                return withDefaultValueResponse;

            var messageContents = new List<string>(azStorageResponse.Value.Length);
            foreach (var rawMessage in azStorageResponse.Value)
                messageContents.Add(GetMessageContent(rawMessage, getBodyFunc, decodeCaseMessageEncoding));

            return azStorageResponse.InduceResponse(messageContents);
        }

        protected virtual AzStorageResponse<string> InduceResponse<GenTIn>(
            AzStorageResponse<GenTIn> azStorageResponse,
            Func<GenTIn, BinaryData> getBodyFunc,
            bool decodeCaseMessageEncoding)
            where GenTIn : class
        {
            var withDefaultValueResponse = InduceResponseWithDefaultValue<GenTIn, string>(azStorageResponse);
            if (withDefaultValueResponse != null)
                return withDefaultValueResponse;

            return azStorageResponse.InduceResponse(
                GetMessageContent(azStorageResponse.Value, getBodyFunc, decodeCaseMessageEncoding));
        }

        protected virtual AzStorageResponse<List<GenTOut>> InduceResponse<GenTOut>(
            AzStorageResponse<List<string>> azStorageResponse,
            Func<string, GenTOut> deserializer)
        {
            var withDefaultValueResponse = InduceResponseWithDefaultValue<List<string>, List<GenTOut>>(azStorageResponse);
            if (withDefaultValueResponse != null)
                return withDefaultValueResponse;

            var messageContents = DeserializeObjects(azStorageResponse.Value, deserializer);

            return azStorageResponse.InduceResponse(messageContents);
        }

        protected virtual AzStorageResponse<GenTOut> InduceResponse<GenTOut>(
            AzStorageResponse<string> azStorageResponse,
            Func<string, GenTOut> deserializer)
        {
            var withDefaultValueResponse = InduceResponseWithDefaultValue<string, GenTOut>(azStorageResponse);
            if (withDefaultValueResponse != null)
                return withDefaultValueResponse;

            GenTOut entity;
            try
            {
                entity = DeserializeObject(azStorageResponse.Value, deserializer);
            }
            catch (Exception e)
            {
                return AzStorageResponse<GenTOut>.Create(e, $"'{nameof(deserializer)}' throws exception " +
                    $"for message:{azStorageResponse.Value}. {AzTextingResources.Exception_message}");
            }

            return azStorageResponse.InduceResponse(entity);
        }

        protected virtual List<GenTOut> DeserializeObjects<GenTOut>(
            List<string> strMessages,
            Func<string, GenTOut> deserializer)
        {
            if (strMessages == default)
                return default;

            var resulting = new List<GenTOut>(strMessages.Count);
            foreach (var msg in strMessages)
                resulting.Add(DeserializeObject(msg, deserializer));

            return resulting;
        }

        protected virtual GenTOut DeserializeObject<GenTOut>(
            string message,
            Func<string, GenTOut> deserializer)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(message, nameof(message), nameof(message));

            GenTOut entity = default;
            try
            {
                entity = deserializer(message);
            }
            catch (Exception e)
            {
                ExThrower.ST_ThrowInvalidOperationException($"'{nameof(deserializer)}' has thrown exception while trying to " +
                    $"deserialize the message:{message}. Exception message:{e.GetDepthMessages()}");
            }

            if (entity == null)
                ExThrower.ST_ThrowInvalidOperationException($"'{nameof(deserializer)}' returned null for message:{message}");

            return entity;
        }

        protected virtual AzStorageResponse<string> SerializeObject<T>(
            T messageToSerialize,
            Func<T, string> serializer)
        {
            string messageText;
            try
            {
                messageText = serializer(messageToSerialize);
            }
            catch (Exception e)
            {
                return AzStorageResponse<string>.Create(e, $"'{nameof(serializer)}' throws exception " +
                    $"serializing object. {AzTextingResources.Exception_message}");
            }

            if (messageText == null)
                ExThrower.ST_ThrowInvalidOperationException($"'{nameof(serializer)}' returned invalid value by " +
                    $"serializing the object.");

            return AzStorageResponse<string>.Create(messageText, true);
        }

        #endregion

        #region QueueClient creator

        protected QueueClient _QueueClient;

        protected virtual QueueClient GetQueueClient(string queueName)
        {
            var response = CreateOrLoadQueueClient(queueName);
            if (response != null && !ResponseValidator.ResponseSucceeded<Response>(response.GetRawResponse()))
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Can_not_load_create_queue);

            return _QueueClient;
        }

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
            ThrowIfInvalidConnectionString();

            return new QueueClient(ConnectionString, queueName, CreateClientOptions(AzQueueClientOptions));
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

        protected virtual void ThrowIfInvalidMessageText(string messageText)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(messageText, nameof(messageText));
        }
        
        protected virtual void ThrowIfInvalidMessage(BinaryData message)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(message, nameof(message), nameof(message));
        }

        protected virtual void ThrowIfInvalidSerializer<T>(Func<T, string> serializer)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(serializer, nameof(serializer));
        }

        protected virtual void ThrowIfInvalidDeserializer<T>(Func<string, T> deserializer)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(deserializer, nameof(deserializer));
        }
        
        protected virtual void ThrowIfInvalidMessageIdPopReceipt(string messageId, string popReceipt)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(messageId, nameof(messageId), nameof(messageId));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(popReceipt, nameof(popReceipt), nameof(popReceipt));
        }

        protected virtual void ThrowIfInvalidMaxMessagesValue(int? maxMessages)
        {
            if (maxMessages != null && (maxMessages < 1 || maxMessages > 32))
                ExThrower.ST_ThrowArgumentOutOfRangeException(nameof(maxMessages), 
                    $"'{nameof(maxMessages)}' parameter is out of valid range: 1-32");
        }

        protected virtual void ThrowIfInvalidReceiptMetadata(ReceiptMetadata receiptMetadata, string paramName = default)
        {
            if (string.IsNullOrEmpty(paramName))
                paramName = nameof(receiptMetadata);

            ExThrower.ST_ThrowIfArgumentIsNull(receiptMetadata, paramName);

            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(receiptMetadata.MessageId, nameof(receiptMetadata.MessageId), nameof(receiptMetadata.MessageId));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(receiptMetadata.PopReceipt, nameof(receiptMetadata.PopReceipt), nameof(receiptMetadata.PopReceipt));
        }
        
        protected virtual void ThrowIfInvalidExpandedReceiptMetadata(ExpandedReceiptMetadata expReceiptMetadata)
        {
            ThrowIfInvalidReceiptMetadata(expReceiptMetadata, nameof(expReceiptMetadata));

            if (expReceiptMetadata.MessageText != default && expReceiptMetadata.Message != default)
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Invalid_operation_message_defined_twice);

            if (expReceiptMetadata.MessageText == default && expReceiptMetadata.Message == default)
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Invalid_operation_message_not_defined);
        }

        protected virtual bool ValidateReceiptMetadata(
            ReceiptMetadata receiptMetadata, 
            out AzStorageResponse azStorageResponse,
            string receiptMetadataParamName = default)
        {
            if (string.IsNullOrEmpty(receiptMetadataParamName))
                receiptMetadataParamName = nameof(receiptMetadata);

            if (receiptMetadata == default)
            {
                azStorageResponse = AzStorageResponse.Create(
                    ErrorTextProvider.receiptMetadata_is_null($"'{receiptMetadataParamName}'"));
                return false;
            }

            #region MessageId validations

            if (string.IsNullOrEmpty(receiptMetadata.MessageId))
            {
                azStorageResponse = AzStorageResponse.Create(
                    ErrorTextProvider.receiptMetadata_MessageId_is_null_or_empty($"'{receiptMetadataParamName}.MessageId'"));
                return false;
            }

            if (string.IsNullOrWhiteSpace(receiptMetadata.MessageId))
            {
                azStorageResponse = AzStorageResponse.Create(
                    ErrorTextProvider.receiptMetadata_MessageId_is_null_or_whitespace($"'{receiptMetadataParamName}.MessageId'"));
                return false;
            }

            #endregion

            #region PopReceipt validations

            if (string.IsNullOrEmpty(receiptMetadata.PopReceipt))
            {
                azStorageResponse = AzStorageResponse.Create(
                    ErrorTextProvider.receiptMetadata_PopReceipt_is_null_or_empty($"'{receiptMetadataParamName}.PopReceipt'"));
                return false;
            }

            if (string.IsNullOrWhiteSpace(receiptMetadata.PopReceipt))
            {
                azStorageResponse = AzStorageResponse.Create(
                    ErrorTextProvider.receiptMetadata_PopReceipt_is_null_or_whitespace($"'{receiptMetadataParamName}.PopReceipt'"));
                return false;
            }

            #endregion

            azStorageResponse = AzStorageResponse.Create<AzStorageResponse>(true);
            return true;
        }

        protected virtual bool ValidateExpandedReceiptMetadata<GenTOut>(
            ExpandedReceiptMetadata expReceiptMetadata,
            out AzStorageResponse<GenTOut> genAzStorageResponse)
        {
            var validationResult = ValidateReceiptMetadata(expReceiptMetadata, out AzStorageResponse azStorageResponse, nameof(expReceiptMetadata));
            genAzStorageResponse = azStorageResponse.InduceResponse<GenTOut, AzStorageResponse<GenTOut>>();
            if (!validationResult)
                return false;

            if (expReceiptMetadata.MessageText != default && expReceiptMetadata.Message != default)
            {
                genAzStorageResponse.Succeeded = false;
                genAzStorageResponse.Message = ErrorTextProvider.Invalid_operation_message_defined_twice;

                return false;
            }

            if (expReceiptMetadata.MessageText == default && expReceiptMetadata.Message == default)
            {
                genAzStorageResponse.Succeeded = false;
                genAzStorageResponse.Message = ErrorTextProvider.Invalid_operation_message_not_defined;

                return false;
            }

            return validationResult;
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

        #region GetProperties

        /// <summary>
        /// Retrieves queue properties and user-defined metadata and properties on the specified queue. 
        /// Metadata is associated with the queue as name-values pairs.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueProperties}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<QueueProperties> GetProperties(string queueName)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            try
            {
                return AzStorageResponse<QueueProperties>.Create(CreateQueueClient(queueName).GetProperties());
            }
            catch (Exception e)
            {
                return AzStorageResponse<QueueProperties>.Create(e);
            }
        }

        #endregion

        #region GetPropertiesAsync

        /// <summary>
        /// Retrieves queue properties and user-defined metadata and properties on the specified queue. 
        /// Metadata is associated with the queue as name-values pairs.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueProperties}"/> indicating the result of the operation,
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<QueueProperties>> GetPropertiesAsync(string queueName)
        {
            ThrowIfInvalidQueueName(queueName, nameof(queueName), nameof(queueName));

            try
            {
                return AzStorageResponse<QueueProperties>
                    .Create(await CreateQueueClient(queueName).GetPropertiesAsync());
            }
            catch (Exception e)
            {
                return AzStorageResponse<QueueProperties>.Create(e);
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Serialize <paramref name="messageEntity"/> and add it as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <c>SerializeObject</c> method of <see cref="JsonConvert"/> class will be used in serialization.
        /// The <c>SerializeObject</c> response must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the <paramref name="messageEntity"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntity">The entity to serialize and add as a new message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value 
        /// for the <paramref name="messageEntity"/>.</exception>
        public virtual AzStorageResponse<SendReceipt> SendMessageEntity<T>(
            T messageEntity,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
            where T : class
        {
            return SendMessageEntity(messageEntity, JsonConvert.SerializeObject, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Serialize <paramref name="messageEntity"/> and add it as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <paramref name="serializer"/> response must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="serializer"/> return invalid value for 
        /// the <paramref name="messageEntity"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntity">The entity to serialize and add as a new message.</param>
        /// <param name="serializer">Used to serialize <paramref name="messageEntity"/></param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="serializer"/> return invalid value for the <paramref name="messageEntity"/>.</exception>
        public virtual AzStorageResponse<SendReceipt> SendMessageEntity<T>(
            T messageEntity,
            Func<T, string> serializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidSerializer(serializer);

            var _strAzStorageResponse = SerializeObject(messageEntity, serializer);
            if (!_strAzStorageResponse.Succeeded)
                return _strAzStorageResponse.InduceResponse<SendReceipt>(default);

            return SendMessage(_strAzStorageResponse.Value, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Adds a new message to the back of a queue. The visibility timeout specifies how long the message 
        /// should be invisible to Receive and Peek operations.
        /// A <paramref name="messageText"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageText">Message to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<SendReceipt> SendMessage(
            string messageText,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidMessageText(messageText);
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            if (encodeCaseMessageEncoding)
                messageText = EncodeIfCase(messageText);

            return FuncHelper.Execute<string, TimeSpan?, TimeSpan?, CancellationToken, Response<SendReceipt>, AzStorageResponse<SendReceipt>, SendReceipt>(
                GetQueueClient(queueName).SendMessage, messageText, visibilityTimeout, timeToLive, cancellationToken);
        }

        /// <summary>
        /// Adds a new message to the back of a queue. The visibility timeout specifies how long the message 
        /// should be invisible to Receive and Peek operations.
        /// A <paramref name="message"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="message">Message to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<SendReceipt> SendMessage(
            BinaryData message,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidMessage(message);
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<BinaryData, TimeSpan?, TimeSpan?, CancellationToken, Response<SendReceipt>, AzStorageResponse<SendReceipt>, SendReceipt>(
                GetQueueClient(queueName).SendMessage, message, visibilityTimeout, timeToLive, cancellationToken);
        }

        #endregion

        #region Put async

        /// <summary>
        /// Serialize <paramref name="messageEntity"/> and add it as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <c>SerializeObject</c> method of <see cref="JsonConvert"/> class will be used in serialization.
        /// The <c>SerializeObject</c> response must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the <paramref name="messageEntity"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntity">The entity to serialize and add as a new message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value 
        /// for the <paramref name="messageEntity"/>.</exception>
        public virtual async Task<AzStorageResponse<SendReceipt>> SendMessageEntityAsync<T>(
            T messageEntity,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
            where T : class
        {
            return await SendMessageEntityAsync(messageEntity, JsonConvert.SerializeObject, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Serialize <paramref name="messageEntity"/> and add it as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <paramref name="serializer"/> response must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="serializer"/> return invalid value for 
        /// the <paramref name="messageEntity"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntity">The entity to serialize and add as a new message.</param>
        /// <param name="serializer">Used to serialize <paramref name="messageEntity"/></param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="serializer"/> return invalid value for the <paramref name="messageEntity"/>.</exception>
        public virtual async Task<AzStorageResponse<SendReceipt>> SendMessageEntityAsync<T>(
            T messageEntity,
            Func<T, string> serializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidSerializer(serializer);

            var _strAzStorageResponse = SerializeObject(messageEntity, serializer);
            if (!_strAzStorageResponse.Succeeded)
                return _strAzStorageResponse.InduceResponse<SendReceipt>(default);

            return await SendMessageAsync(_strAzStorageResponse.Value, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Adds a new message to the back of a queue. The visibility timeout specifies how long the message 
        /// should be invisible to Receive and Peek operations.
        /// A <paramref name="messageText"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageText">Message to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<SendReceipt>> SendMessageAsync(
            string messageText,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidMessageText(messageText);
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            if (encodeCaseMessageEncoding)
                messageText = EncodeIfCase(messageText);

            return await FuncHelper.ExecuteAsync<string, TimeSpan?, TimeSpan?, CancellationToken, Response<SendReceipt>, AzStorageResponse<SendReceipt>, SendReceipt>(
                GetQueueClient(queueName).SendMessageAsync, messageText, visibilityTimeout, timeToLive, cancellationToken);
        }
        
        /// <summary>
        /// Adds a new message to the back of a queue. The visibility timeout specifies how long the message 
        /// should be invisible to Receive and Peek operations.
        /// A <paramref name="message"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="message">Message to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<SendReceipt>> SendMessageAsync(
            BinaryData message,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidMessage(message);
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<BinaryData, TimeSpan?, TimeSpan?, CancellationToken, Response<SendReceipt>, AzStorageResponse<SendReceipt>, SendReceipt>(
                GetQueueClient(queueName).SendMessageAsync, message, visibilityTimeout, timeToLive, cancellationToken);
        }

        #endregion

        #region Put messages

        /// <summary>
        /// Serialize each message in <paramref name="messageEntities"/> and add each one as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <c>SerializeObject</c> method of <see cref="JsonConvert"/> class will be used in serialization.
        /// All <c>SerializeObject</c> responses must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message in <paramref name="messageEntities"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntities">The messages to serialize and add each one as a new message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value 
        /// for any message in <paramref name="messageEntities"/>.</exception>
        public virtual List<AzStorageResponse<SendReceipt>> SendMessageEntities<T>(
            IEnumerable<T> messageEntities,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
            where T : class
        {
            return SendMessageEntities(messageEntities, JsonConvert.SerializeObject, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Serialize each message in <paramref name="messageEntities"/> and add each one as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// All <paramref name="serializer"/> responses must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="serializer"/> return invalid value 
        /// for any message in <paramref name="messageEntities"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntities">The messages to serialize and add each one as a new message.</param>
        /// <param name="serializer">Used to serialize <paramref name="messageEntities"/></param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="serializer"/> return invalid value 
        /// for any message in <paramref name="messageEntities"/>.</exception>
        public virtual List<AzStorageResponse<SendReceipt>> SendMessageEntities<T>(
            IEnumerable<T> messageEntities,
            Func<T, string> serializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(messageEntities), nameof(messageEntities));

            var results = new List<AzStorageResponse<SendReceipt>>();


            AzStorageResponse<SendReceipt> _response;
            foreach (var entityMsg in messageEntities)
            {
                try
                {
                    _response = SendMessageEntity(entityMsg, serializer, queueName, visibilityTimeout,
                        timeToLive, cancellationToken, encodeCaseMessageEncoding);
                }
                catch (Exception e)
                {
                    _response = AzStorageResponse<SendReceipt>
                        .CreateWithException<Exception, AzStorageResponse<SendReceipt>>(e);
                }

                results.Add(_response);
            }

            return results;
        }

        /// <summary>
        /// Adds each message in <paramref name="messages"/> to the back of a queue. The visibility timeout specifies 
        /// how long the message should be invisible to Receive and Peek operations.
        /// Each message in <paramref name="messages"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messages">Messages to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation.</returns>
        public virtual List<AzStorageResponse<SendReceipt>> SendMessages(
           IEnumerable<string> messages,
           string queueName = null,
           TimeSpan? visibilityTimeout = default,
           TimeSpan? timeToLive = default,
           CancellationToken cancellationToken = default,
           bool encodeCaseMessageEncoding = true)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(messages), nameof(messages));

            var results = new List<AzStorageResponse<SendReceipt>>();
            foreach (var msg in messages)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<SendReceipt>());
                else
                    results.Add(SendMessage(msg, queueName, visibilityTimeout,
                    timeToLive, cancellationToken, encodeCaseMessageEncoding));

            return results;
        }

        #endregion

        #region Put messages async

        /// <summary>
        /// Serialize each message in <paramref name="messageEntities"/> and add each one as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// The <c>SerializeObject</c> method of <see cref="JsonConvert"/> class will be used in serialization.
        /// All <c>SerializeObject</c> responses must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message in <paramref name="messageEntities"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntities">The messages to serialize and add each one as a new message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value 
        /// for any message in <paramref name="messageEntities"/>.</exception>
        public virtual async Task<List<AzStorageResponse<SendReceipt>>> SendMessageEntitiesAsync<T>(
            IEnumerable<T> messageEntities,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
            where T : class
        {
            return await SendMessageEntitiesAsync(messageEntities, JsonConvert.SerializeObject, queueName,
                visibilityTimeout, timeToLive, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Serialize each message in <paramref name="messageEntities"/> and add each one as a new message to the back of a queue. 
        /// The visibility timeout specifies how long the message should be invisible to Receive and Peek operations.
        /// All <paramref name="serializer"/> responses must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="serializer"/> return invalid value 
        /// for any message in <paramref name="messageEntities"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="messageEntities">The messages to serialize and add each one as a new message.</param>
        /// <param name="serializer">Used to serialize <paramref name="messageEntities"/></param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="serializer"/> return invalid value 
        /// for any message in <paramref name="messageEntities"/>.</exception>
        public virtual async Task<List<AzStorageResponse<SendReceipt>>> SendMessageEntitiesAsync<T>(
            IEnumerable<T> messageEntities,
            Func<T, string> serializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(messageEntities), nameof(messageEntities));

            var results = new List<AzStorageResponse<SendReceipt>>();
            AzStorageResponse<SendReceipt> _response;
            foreach (var entityMsg in messageEntities)
            {
                try
                {
                    _response = await SendMessageEntityAsync(entityMsg, serializer, queueName, visibilityTimeout,
                        timeToLive, cancellationToken, encodeCaseMessageEncoding);
                }
                catch (Exception e)
                {
                    _response = AzStorageResponse<SendReceipt>
                        .CreateWithException<Exception, AzStorageResponse<SendReceipt>>(e);
                }

                results.Add(_response);
            }

            return results;
        }

        /// <summary>
        /// Adds each message in <paramref name="messages"/> to the back of a queue. The visibility timeout specifies 
        /// how long the message should be invisible to Receive and Peek operations.
        /// Each message in <paramref name="messages"/> must be in a format that can be included in an XML request with UTF-8 encoding.
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding option can
        /// be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle non compliant
        /// messages. The encoded message can be up to 64 KiB in size for versions 2011-08-18
        /// and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messages">Messages to add.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Queues.Models.SendReceipt}"/> 
        /// indicating the result of each add operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<SendReceipt>>> SendMessagesAsync(
           IEnumerable<string> messages,
           string queueName = null,
           TimeSpan? visibilityTimeout = default,
           TimeSpan? timeToLive = default,
           CancellationToken cancellationToken = default,
           bool encodeCaseMessageEncoding = true)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(messages), nameof(messages));

            var results = new List<AzStorageResponse<SendReceipt>>();
            foreach (var msg in messages)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<SendReceipt>());
                else
                    results.Add(await SendMessageAsync(msg, queueName, visibilityTimeout,
                        timeToLive, cancellationToken, encodeCaseMessageEncoding));

            return results;
        }

        #endregion

        #region Get

        /// <summary>
        /// Receives one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for the message.</exception>
        public virtual AzStorageResponse<T> ReceiveMessageEntity<T>(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return ReceiveMessageEntity(JsonConvert.DeserializeObject<T>, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for the message.</exception>
        public virtual AzStorageResponse<T> ReceiveMessageEntity<T>(
            Func<string, T> deserializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = ReceiveMessage(queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Receives one message from the front of the queue.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{string}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<string> ReceiveMessage(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = ReceiveRawMessage(queueName, visibilityTimeout, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, queueMsg => queueMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one message from the front of the queue as <see cref="QueueMessage"/> instance.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueMessage}"/> indicating the result of the operation.</returns>
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

        #region Get async

        /// <summary>
        /// Receives one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for the message.</exception>
        public virtual async Task<AzStorageResponse<T>> ReceiveMessageEntityAsync<T>(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return await ReceiveMessageEntityAsync(JsonConvert.DeserializeObject<T>, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for the message.</exception>
        public virtual async Task<AzStorageResponse<T>> ReceiveMessageEntityAsync<T>(
            Func<string, T> deserializer,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = await ReceiveMessageAsync(queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Receives one message from the front of the queue.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{string}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<string>> ReceiveMessageAsync(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = await ReceiveRawMessageAsync(queueName, visibilityTimeout, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, queueMsg => queueMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one message from the front of the queue into <see cref="QueueMessage"/> instance.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueMessage}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<QueueMessage>> ReceiveRawMessageAsync(
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<TimeSpan?, CancellationToken, Response<QueueMessage>, AzStorageResponse<QueueMessage>, QueueMessage>(
                GetQueueClient(queueName).ReceiveMessageAsync, visibilityTimeout, cancellationToken);
        }

        #endregion

        #region Get messages

        /// <summary>
        /// Receives one or more messages from the front of the queue and deserialize each message to the specified type <typeparamref name="T"/>.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for any message.</exception>
        public virtual AzStorageResponse<List<T>> ReceiveMessageEntities<T>(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return ReceiveMessageEntities(JsonConvert.DeserializeObject<T>, maxMessages, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue and deserialize each message to the specified type <typeparamref name="T"/>.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for any message.</exception>
        public virtual AzStorageResponse<List<T>> ReceiveMessageEntities<T>(
            Func<string, T> deserializer,
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = ReceiveMessages(maxMessages, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{string}}"/> indicating the result of the operation
        /// containing a collection of messages.</returns>
        public virtual AzStorageResponse<List<string>> ReceiveMessages(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = ReceiveRawMessages(maxMessages, queueName,
                visibilityTimeout, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, queueMsg => queueMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue as <see cref="QueueMessage"/> instances.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueMessage[]}"/> indicating the result of the operation
        /// containing a collection of messages.</returns>
        public virtual AzStorageResponse<QueueMessage[]> ReceiveRawMessages(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            ThrowIfInvalidMaxMessagesValue(maxMessages);

            return FuncHelper.Execute<int?, TimeSpan?, CancellationToken, Response<QueueMessage[]>, AzStorageResponse<QueueMessage[]>, QueueMessage[]>(
                GetQueueClient(queueName).ReceiveMessages, maxMessages, visibilityTimeout, cancellationToken);
        }

        #endregion

        #region Get messages async

        /// <summary>
        /// Receives one or more messages from the front of the queue and deserialize each message to the specified type <typeparamref name="T"/>.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for any message.</exception>
        public virtual async Task<AzStorageResponse<List<T>>> ReceiveMessageEntitiesAsync<T>(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return await ReceiveMessageEntitiesAsync(JsonConvert.DeserializeObject<T>, maxMessages, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue and deserialize each message to the specified type <typeparamref name="T"/>.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for any message.</exception>
        public virtual async Task<AzStorageResponse<List<T>>> ReceiveMessageEntitiesAsync<T>(
            Func<string, T> deserializer,
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = await ReceiveMessagesAsync(maxMessages, queueName, visibilityTimeout,
                cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{string}}"/> indicating the result of the operation
        /// containing a collection of messages, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<string>>> ReceiveMessagesAsync(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = await ReceiveRawMessagesAsync(maxMessages, queueName, 
                visibilityTimeout, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, queueMsg => queueMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Receives one or more messages from the front of the queue as <see cref="QueueMessage"/> instances.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to retrieve
        ///  from the queue, up to a maximum of 32. If fewer are visible, the visible messages
        ///  are returned. By default, a single message is retrieved from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. 
        /// Cannot be larger than 7 days.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{QueueMessage[]}"/> indicating the result of the operation
        /// containing a collection of messages, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<QueueMessage[]>> ReceiveRawMessagesAsync(
            int? maxMessages = null,
            string queueName = null,
            TimeSpan? visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            ThrowIfInvalidMaxMessagesValue(maxMessages);

            return await FuncHelper.ExecuteAsync<int?, TimeSpan?, CancellationToken, Response<QueueMessage[]>, AzStorageResponse<QueueMessage[]>, QueueMessage[]>(
                GetQueueClient(queueName).ReceiveMessagesAsync, maxMessages, visibilityTimeout, cancellationToken);
        }

        #endregion

        #region Peek

        /// <summary>
        /// Retrieves one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>,
        /// but does not alter the visibility of the message.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for the message.</exception>
        public virtual AzStorageResponse<T> PeekMessageEntity<T>(
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return PeekMessageEntity(JsonConvert.DeserializeObject<T>, queueName, cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>,
        /// but does not alter the visibility of the message.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for the message.</exception>
        public virtual AzStorageResponse<T> PeekMessageEntity<T>(
            Func<string, T> deserializer,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = PeekMessage(queueName, cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue but does not alter the visibility of the message.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{string}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<string> PeekMessage(
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = PeekRawMessage(queueName, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, peekedMsg => peekedMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue as <see cref="PeekedMessage"/> instance, 
        /// but does not alter the visibility of the message.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{PeekedMessage}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<PeekedMessage> PeekRawMessage(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<CancellationToken, Response<PeekedMessage>, AzStorageResponse<PeekedMessage>, PeekedMessage>(
                GetQueueClient(queueName).PeekMessage, cancellationToken);
        }

        #endregion

        #region Peek async

        /// <summary>
        /// Retrieves one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>,
        /// but does not alter the visibility of the message.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for the message.</exception>
        public virtual async Task<AzStorageResponse<T>> PeekMessageEntityAsync<T>(
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return await PeekMessageEntityAsync(JsonConvert.DeserializeObject<T>, queueName, cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue and deserialize it to the specified type <typeparamref name="T"/>,
        /// but does not alter the visibility of the message.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for the message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for the message.</exception>
        public virtual async Task<AzStorageResponse<T>> PeekMessageEntityAsync<T>(
            Func<string, T> deserializer,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = await PeekMessageAsync(queueName, cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue but does not alter the visibility of the message.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{string}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<string>> PeekMessageAsync(
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = await PeekRawMessageAsync(queueName, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, peekedMsg => peekedMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one message from the front of the queue as <see cref="PeekedMessage"/> instance, 
        /// but does not alter the visibility of the message.
        /// </summary>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{PeekedMessage}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<PeekedMessage>> PeekRawMessageAsync(
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<CancellationToken, Response<PeekedMessage>, AzStorageResponse<PeekedMessage>, PeekedMessage>(
                GetQueueClient(queueName).PeekMessageAsync, cancellationToken);
        }

        #endregion

        #region Peek messages

        /// <summary>
        /// Retrieves one or more messages from the front of the queue and deserialize it to the 
        /// specified type <typeparamref name="T"/>, but does not alter the visibility of the message.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation,
        /// containing a collection of entities.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for any message.</exception>
        public virtual AzStorageResponse<List<T>> PeekMessageEntities<T>(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return PeekMessageEntities(JsonConvert.DeserializeObject<T>, maxMessages, queueName,
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue but does not alter the visibility of the message.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the messages.</param>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation,
        /// containing a collection of entities.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for any message.</exception>
        public virtual AzStorageResponse<List<T>> PeekMessageEntities<T>(
            Func<string, T> deserializer,
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = PeekMessages(maxMessages, queueName,
                cancellationToken, decodeCaseMessageEncoding);

            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue but does not alter the visibility of the message.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{string}}"/> indicating the result of the operation,
        /// containing a collection of messages.</returns>
        public virtual AzStorageResponse<List<string>> PeekMessages(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = PeekRawMessages(maxMessages, queueName, cancellationToken);

            return InduceResponse(
                _msgAzStorageResponse,
                peekedMsg => peekedMsg.Body,
                decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue as <see cref="PeekedMessage"/> instances, 
        /// but does not alter the visibility of the message.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{PeekedMessage}"/> indicating the result of the operation,
        /// containing a collection of <see cref="PeekedMessage"/>.</returns>
        public virtual AzStorageResponse<PeekedMessage[]> PeekRawMessages(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            ThrowIfInvalidMaxMessagesValue(maxMessages);

            return FuncHelper.Execute<int?, CancellationToken, Response<PeekedMessage[]>, AzStorageResponse<PeekedMessage[]>, PeekedMessage[]>(
                GetQueueClient(queueName).PeekMessages, maxMessages, cancellationToken);
        }

        #endregion

        #region Peek messages async

        /// <summary>
        /// Retrieves one or more messages from the front of the queue and deserialize it to the 
        /// specified type <typeparamref name="T"/>, but does not alter the visibility of the message.
        /// The <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <c>DeserializeObject<T></c> method 
        /// of <see cref="JsonConvert"/> class return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <c>DeserializeObject<T></c> method of <see cref="JsonConvert"/> class return invalid value for any message.</exception>
        public virtual async Task<AzStorageResponse<List<T>>> PeekMessageEntitiesAsync<T>(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            return await PeekMessageEntitiesAsync(JsonConvert.DeserializeObject<T>, maxMessages, queueName, 
                cancellationToken, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue but does not alter the visibility of the message.
        /// The <paramref name="deserializer"/> will be used in deserialization.
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="deserializer"/> return invalid value for any message.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="deserializer">Used to deserialize the messages.</param>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{T}}"/> indicating the result of the operation
        /// containing a collection of entities, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Throws <see cref="InvalidOperationException"/> 
        /// if <paramref name="deserializer"/> return invalid value for any message.</exception>
        public virtual async Task<AzStorageResponse<List<T>>> PeekMessageEntitiesAsync<T>(
            Func<string, T> deserializer,
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidDeserializer(deserializer);

            var _strAzStorageResponse = await PeekMessagesAsync(maxMessages, queueName, 
                cancellationToken, decodeCaseMessageEncoding);
            
            return InduceResponse(_strAzStorageResponse, deserializer);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue but does not alter the visibility of the message.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="decodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then decode the message according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{List{string}}"/> indicating the result of the operation
        /// containing a collection of messages,  
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<string>>> PeekMessagesAsync(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default,
            bool decodeCaseMessageEncoding = true)
        {
            var _msgAzStorageResponse = await PeekRawMessagesAsync(maxMessages, queueName, cancellationToken);

            return InduceResponse(_msgAzStorageResponse, peekedMsg => peekedMsg.Body, decodeCaseMessageEncoding);
        }

        /// <summary>
        /// Retrieves one or more messages from the front of the queue as <see cref="PeekedMessage"/> instances, 
        /// but does not alter the visibility of the message.
        /// </summary>
        /// <param name="maxMessages">A nonzero integer value that specifies the number of messages to peek
        /// from the queue, up to a maximum of 32. By default, a single message is peeked
        /// from the queue with this operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Queues.Models.PeekedMessage[]}"/> indicating the result of the operation
        /// containing a collection of messages, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<PeekedMessage[]>> PeekRawMessagesAsync(
            int? maxMessages = null,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            ThrowIfInvalidMaxMessagesValue(maxMessages);

            return await FuncHelper.ExecuteAsync<int?, CancellationToken, Response<PeekedMessage[]>, AzStorageResponse<PeekedMessage[]>, PeekedMessage[]>(
                GetQueueClient(queueName).PeekMessagesAsync, maxMessages, cancellationToken);
        }

        #endregion

        #region Update

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="expReceiptMetadata">Must contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation. Optionally can contains messageText and visibilityTimeout.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<UpdateReceipt> UpdateMessage(
            ExpandedReceiptMetadata expReceiptMetadata,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidExpandedReceiptMetadata(expReceiptMetadata);

            if (!string.IsNullOrEmpty(expReceiptMetadata.MessageText))
                return UpdateMessage(expReceiptMetadata.MessageId, expReceiptMetadata.PopReceipt,
                    expReceiptMetadata.MessageText, queueName, expReceiptMetadata.VisibilityTimeout, cancellationToken);

            return UpdateMessage(expReceiptMetadata.MessageId, expReceiptMetadata.PopReceipt,
                expReceiptMetadata.Message, queueName, expReceiptMetadata.VisibilityTimeout, cancellationToken);
        }

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="receiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="message">Updated message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<UpdateReceipt> UpdateMessage(
            ReceiptMetadata receiptMetadata,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidReceiptMetadata(receiptMetadata);

            return UpdateMessage(receiptMetadata.MessageId, receiptMetadata.PopReceipt, message,
                queueName, visibilityTimeout, cancellationToken);
        }
        
        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="message">Updated message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<UpdateReceipt> UpdateMessage(
            string messageId,
            string popReceipt,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<string, string, BinaryData, TimeSpan, CancellationToken, Response<UpdateReceipt>, AzStorageResponse<UpdateReceipt>, UpdateReceipt>(
                GetQueueClient(queueName).UpdateMessage, messageId, popReceipt, message, visibilityTimeout, cancellationToken);
        }

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="receiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="messageText">Updated message text.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="message"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<UpdateReceipt> UpdateMessage(
            ReceiptMetadata receiptMetadata,
            string messageText = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidReceiptMetadata(receiptMetadata);

            return UpdateMessage(receiptMetadata.MessageId, receiptMetadata.PopReceipt, messageText,
                queueName, visibilityTimeout, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="messageText">Updated message text.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<UpdateReceipt> UpdateMessage(
            string messageId,
            string popReceipt,
            string messageText = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            if (encodeCaseMessageEncoding)
                messageText = EncodeIfCase(messageText);

            return FuncHelper.Execute<string, string, string, TimeSpan, CancellationToken, Response<UpdateReceipt>, AzStorageResponse<UpdateReceipt>, UpdateReceipt>(
                GetQueueClient(queueName).UpdateMessage, messageId, popReceipt, messageText, visibilityTimeout, cancellationToken);
        }

        #endregion

        #region Update async

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="expReceiptMetadata">Must contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation. Optionally can contains messageText and visibilityTimeout.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ExpandedReceiptMetadata expReceiptMetadata,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidExpandedReceiptMetadata(expReceiptMetadata);

            if (!string.IsNullOrEmpty(expReceiptMetadata.MessageText))
                return await UpdateMessageAsync(expReceiptMetadata.MessageId, expReceiptMetadata.PopReceipt,
                    expReceiptMetadata.MessageText, queueName, expReceiptMetadata.VisibilityTimeout, cancellationToken);

            return await UpdateMessageAsync(expReceiptMetadata.MessageId, expReceiptMetadata.PopReceipt,
                expReceiptMetadata.Message, queueName, expReceiptMetadata.VisibilityTimeout, cancellationToken);
        }
        
        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="receiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="message">Updated message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ReceiptMetadata receiptMetadata,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidReceiptMetadata(receiptMetadata);

            return await UpdateMessageAsync(receiptMetadata.MessageId, receiptMetadata.PopReceipt, message,
                queueName, visibilityTimeout, cancellationToken);
        }
        
        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="message">Updated message.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            string messageId,
            string popReceipt,
            BinaryData message = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<string, string, BinaryData, TimeSpan, CancellationToken, Response<UpdateReceipt>, AzStorageResponse<UpdateReceipt>, UpdateReceipt>(
                GetQueueClient(queueName).UpdateMessageAsync, messageId, popReceipt, message, visibilityTimeout, cancellationToken);
        }

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="receiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="messageText">Updated message text.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="message"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            ReceiptMetadata receiptMetadata,
            string messageText = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidReceiptMetadata(receiptMetadata);

            return await UpdateMessageAsync(receiptMetadata.MessageId, receiptMetadata.PopReceipt, messageText,
                queueName, visibilityTimeout, cancellationToken, encodeCaseMessageEncoding);
        }

        /// <summary>
        /// Changes a message's visibility timeout and contents. A message must be in a format
        /// that can be included in an XML request with UTF-8 encoding. 
        /// Otherwise AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding
        /// option can be set to Azure.Storage.Queues.QueueMessageEncoding.Base64 to handle
        /// non compliant messages. The encoded message can be up to 64 KiB in size for versions
        /// 2011-08-18 and newer, or 8 KiB in size for previous versions.
        /// </summary>
        /// <param name="messageId">ID of the message to delete.</param>
        /// <param name="popReceipt">A valid pop receipt value returned from an earlier call to the 
        /// Get Messages or Update Message operation.</param>
        /// <param name="messageText">Updated message text.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The new value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has expired.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="encodeCaseMessageEncoding">If AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding is defined
        /// then encode <paramref name="messageText"/> according to the value specified in AzStorage.Core.Queues.AzQueueClientOptions.MessageEncoding.</param>
        /// <returns>The <see cref="AzStorageResponse{UpdateReceipt}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<UpdateReceipt>> UpdateMessageAsync(
            string messageId,
            string popReceipt,
            string messageText = null,
            string queueName = null,
            TimeSpan visibilityTimeout = default,
            CancellationToken cancellationToken = default,
            bool encodeCaseMessageEncoding = true)
        {
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            if (encodeCaseMessageEncoding)
                messageText = EncodeIfCase(messageText);

            return await FuncHelper.ExecuteAsync<string, string, string, TimeSpan, CancellationToken, Response<UpdateReceipt>, AzStorageResponse<UpdateReceipt>, UpdateReceipt>(
                GetQueueClient(queueName).UpdateMessageAsync, messageId, popReceipt, messageText, visibilityTimeout, cancellationToken);
        }

        #endregion

        #region Update messages

        /// <summary>
        /// Permanently removes each message in <paramref name="messages"/> from the queue.
        /// </summary>
        /// <param name="expReceiptsMetadata">Contains IDs of the messages to delete and 
        /// valid pop receipt values returned from an earlier calls to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{UpdateReceipt}"/> 
        /// indicating the result of each remove operation.</returns>
        public virtual List<AzStorageResponse<UpdateReceipt>> UpdateMessages(
           IEnumerable<ExpandedReceiptMetadata> expReceiptsMetadata,
           string queueName = null,
           CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(expReceiptsMetadata, nameof(expReceiptsMetadata), nameof(expReceiptsMetadata));

            var results = new List<AzStorageResponse<UpdateReceipt>>();
            foreach (var _receipt in expReceiptsMetadata)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<UpdateReceipt>());
                else
                {
                    if (!ValidateExpandedReceiptMetadata(_receipt, out AzStorageResponse<UpdateReceipt> tmpAzStorageResponse))
                        results.Add(tmpAzStorageResponse);
                    else
                        results.Add(UpdateMessage(_receipt, queueName, cancellationToken));
                }

            return results;
        }

        #endregion

        #region Update messages async

        /// <summary>
        /// Permanently removes each message in <paramref name="messages"/> from the queue.
        /// </summary>
        /// <param name="expReceiptsMetadata">Contains IDs of the messages to delete and 
        /// valid pop receipt values returned from an earlier calls to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{UpdateReceipt}"/> 
        /// indicating the result of each remove operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<UpdateReceipt>>> UpdateMessagesAsync(
           IEnumerable<ExpandedReceiptMetadata> expReceiptsMetadata,
           string queueName = null,
           CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(expReceiptsMetadata, nameof(expReceiptsMetadata), nameof(expReceiptsMetadata));

            var results = new List<AzStorageResponse<UpdateReceipt>>();
            foreach (var _receipt in expReceiptsMetadata)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<UpdateReceipt>());
                else
                {
                    if (!ValidateExpandedReceiptMetadata(_receipt, out AzStorageResponse<UpdateReceipt> tmpAzStorageResponse))
                        results.Add(tmpAzStorageResponse);
                    else
                        results.Add(await UpdateMessageAsync(_receipt, queueName, cancellationToken));
                }

            return results;
        }

        #endregion

        #region Delete message

        /// <summary>
        /// Permanently removes the specified message from its queue.
        /// </summary>
        /// <param name="sendReceiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteMessage(
            ReceiptMetadata sendReceiptMetadata,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidReceiptMetadata(sendReceiptMetadata);

            return DeleteMessage(sendReceiptMetadata.MessageId, sendReceiptMetadata.PopReceipt, queueName, cancellationToken);
        }

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
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return FuncHelper.Execute<string, string, CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).DeleteMessage, messageId, popReceipt, cancellationToken);
        }

        #endregion

        #region Delete message async

        /// <summary>
        /// Permanently removes the specified message from its queue.
        /// </summary>
        /// <param name="receiptMetadata">Contains ID of the message to delete and 
        /// valid pop receipt value returned from an earlier call to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteMessageAsync(
            ReceiptMetadata receiptMetadata,
            string queueName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidReceiptMetadata(receiptMetadata);

            return await DeleteMessageAsync(receiptMetadata.MessageId, receiptMetadata.PopReceipt, queueName, cancellationToken);
        }
        
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
            ThrowIfInvalidMessageIdPopReceipt(messageId, popReceipt);

            queueName = GetValidQueueNameOrThrowIfInvalid(queueName);

            return await FuncHelper.ExecuteAsync<string, string, CancellationToken, Response, AzStorageResponse>(
                GetQueueClient(queueName).DeleteMessageAsync, messageId, popReceipt, cancellationToken);
        }

        #endregion

        #region Delete messages

        /// <summary>
        /// Permanently removes each message in <paramref name="messages"/> from the queue.
        /// </summary>
        /// <param name="receiptsMetadata">Contains IDs of the messages to delete and 
        /// valid pop receipt values returned from an earlier calls to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> 
        /// indicating the result of each remove operation.</returns>
        public virtual List<AzStorageResponse> DeleteMessages(
           IEnumerable<ReceiptMetadata> receiptsMetadata,
           string queueName = null,
           CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(receiptsMetadata, nameof(receiptsMetadata), nameof(receiptsMetadata));

            var results = new List<AzStorageResponse>();
            foreach (var _receipt in receiptsMetadata)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage());
                else
                {
                    if (!ValidateReceiptMetadata(_receipt, out AzStorageResponse tmpAzStorageResponse))
                        results.Add(tmpAzStorageResponse);
                    else
                        results.Add(DeleteMessage(_receipt, queueName, cancellationToken));
                }

            return results;
        }

        #endregion

        #region Delete messages async

        /// <summary>
        /// Permanently removes each message in <paramref name="messages"/> from the queue.
        /// </summary>
        /// <param name="receiptsMetadata">Contains IDs of the messages to delete and 
        /// valid pop receipt values returned from an earlier calls to the 
        /// Get Message or Update Message operation.</param>
        /// <param name="queueName">The queue name to execute the operation. If <paramref name="queueName"/> is null, 
        /// the value of property <c>DefaultQueueName</c> will be taken as queue name.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> 
        /// indicating the result of each remove operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse>> DeleteMessagesAsync(
           IEnumerable<ReceiptMetadata> receiptsMetadata,
           string queueName = null,
           CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(receiptsMetadata, nameof(receiptsMetadata), nameof(receiptsMetadata));

            var results = new List<AzStorageResponse>();
            foreach (var _receipt in receiptsMetadata)
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage());
                else
                {
                    if (!ValidateReceiptMetadata(_receipt, out AzStorageResponse tmpAzStorageResponse))
                        results.Add(tmpAzStorageResponse);
                    else
                        results.Add(await DeleteMessageAsync(_receipt, queueName, cancellationToken));
                }

            return results;
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
