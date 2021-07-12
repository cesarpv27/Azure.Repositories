using AzStorage.Repositories.Core;
using System;
using System.Collections.Generic;
using System.Text;
using AzCoreTools.Core;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using Microsoft.Azure.Cosmos;
using AzStorage.Core.Cosmos;
using System.Threading.Tasks;
using AzCoreTools.Core.Validators;
using CoreTools.Extensions;
using AzStorage.Core.Utilities;

namespace AzStorage.Repositories
{
    public class AzCosmosDBRepository : AzRepository<AzCosmosRetryOptions>
    {
        protected AzCosmosDBRepository() : base() { }

        public AzCosmosDBRepository(string accountEndpointUri,
            string authKeyOrResourceToken,
            CreateResourcePolicy optionCreateTableResource = CreateResourcePolicy.OnlyFirstTime,
            AzCosmosRetryOptions retryOptions = null) : base(optionCreateTableResource, retryOptions)
        {
            Initialize(accountEndpointUri, authKeyOrResourceToken);
        }

        #region Properties

        public virtual string AccountEndpointUri { get; set; }
        public virtual string AuthKeyOrResourceToken { get; set; }

        public virtual string DatabaseId { get; set; }
        public virtual string ContainerId { get; set; }
        public virtual string PartitionKeyPath { get; protected set; }

        public virtual CosmosClient CosmosClient { get; protected set; }
        public virtual Database Database { get; protected set; }
        public virtual Container Container { get; protected set; }

        protected virtual bool IsFirstTimeDatabaseCreation { get; set; } = true;
        protected virtual bool IsFirstTimeContainerCreation { get; set; } = true;

        #endregion

        #region Throws

        protected virtual void ThrowIfInvalid_AccountEndpointUri_AuthKeyOrResourceToken(
            string accountEndpointUri,
            string authKeyOrResourceToken)
        {
            ThrowIfInvalidAccountEndpointUri(accountEndpointUri);
            ThrowIfInvalidAuthKeyOrResourceToken(authKeyOrResourceToken);
        }

        protected virtual void ThrowIfInvalidAccountEndpointUri(string accountEndpointUri)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(accountEndpointUri, nameof(accountEndpointUri));
        }

        protected virtual void ThrowIfInvalidAuthKeyOrResourceToken(string authKeyOrResourceToken)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(authKeyOrResourceToken, nameof(authKeyOrResourceToken));
        }

        protected virtual void ThrowIfInvalidDatabaseId(string databaseId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(databaseId, nameof(databaseId));
        }

        protected virtual void ThrowIfInvalidContainerId(string containerId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(containerId, nameof(containerId));
        }

        protected virtual void ThrowIfInvalidPartitionKeyPath(string partitionKeyPath)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(partitionKeyPath, nameof(partitionKeyPath), nameof(partitionKeyPath));
            if (!partitionKeyPath.StartsWith("/"))
                ExThrower.ST_ThrowArgumentException(nameof(partitionKeyPath), $"{nameof(partitionKeyPath)} does not have the expected format");
        }

        protected virtual void ThrowIfInvalidCosmosClient()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(CosmosClient, nameof(CosmosClient), nameof(CosmosClient));
        }

        protected virtual void ThrowIfInvalidDatabase()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(Database, nameof(Database), nameof(Database));
        }

        //protected virtual void ThrowIfInvalidContainer()
        //{
        //    ExThrower.ST_ThrowIfArgumentIsNull(Container, nameof(Container), nameof(Container));
        //}

        //protected virtual void ThrowIfInvalidCreateContainerResponse(ContainerResponse response)
        //{
        //    if (!ResponseValidator.StatusSucceeded(response.StatusCode))
        //        ExThrower.ST_ThrowApplicationException($"Can not create container. StatusCode:{response.StatusCode}");
        //}

        #endregion

        #region Protected methods

        protected virtual void Initialize(string accountEndpointUri, string authKeyOrResourceToken)
        {
            ThrowIfInvalid_AccountEndpointUri_AuthKeyOrResourceToken(accountEndpointUri, authKeyOrResourceToken);

            AccountEndpointUri = accountEndpointUri;
            AuthKeyOrResourceToken = authKeyOrResourceToken;

            LoadCosmosClient(accountEndpointUri, authKeyOrResourceToken);
        }

        #endregion

        #region CosmosClient creator

        /// <summary>
        /// Creates a new CosmosClient with the account endpoint URI string and account key.
        /// </summary>
        /// <param name="accountEndpointUri">The cosmos service endpoint to use.</param>
        /// <param name="authKeyOrResourceToken">The cosmos account key or resource token to use to create the client.</param>
        protected virtual void LoadCosmosClient(string accountEndpointUri, string authKeyOrResourceToken)
        {
            CosmosClient = new CosmosClient(accountEndpointUri, authKeyOrResourceToken);
        }

        #endregion

        #region Database creator

        /// <summary>
        /// Check if a database exists and create database if does not exists.
        /// The database id is used to verify if there is an existing database.
        /// </summary>
        /// <param name="databaseId">The database id.</param>
        protected virtual void CreateDatabase(string databaseId)
        {
            ThrowIfInvalidDatabaseId(databaseId);

            ThrowIfInvalidCosmosClient();

            CreateDatabase<string, DatabaseResponse>(databaseId, CreateDatabaseIfNotExists);
        }

        private void CreateDatabase<FTIn1, FTOut>(string databaseId, Func<FTIn1, FTOut> func)
        {
            if (Database == null || !databaseId.Equals(DatabaseId))
                IsFirstTimeDatabaseCreation = true;

            bool _isFirstTime = IsFirstTimeDatabaseCreation;

            DatabaseResponse response;
            var result = TryCreateResource(func, new dynamic[] { databaseId }, ref _isFirstTime, out response);

            IsFirstTimeDatabaseCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceCosmosResponseSucceeded<Response<DatabaseProperties>, DatabaseProperties>(response))
            {
                Database = response;
                DatabaseId = databaseId;
            }
        }

        private DatabaseResponse CreateDatabaseIfNotExists(string databaseId)
        {
            return CosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).WaitAndUnwrapException();
        }

        #endregion

        #region Container creator

        /// <summary>
        /// Check if a container exists and create container if does not exists.
        /// This will make a read operation, and if the container is not found it will do a create operation.
        /// </summary>
        /// <param name="containerId">The Cosmos container id</param>
        /// <param name="partitionKeyPath">The path to the partition key. Example: /PartitionKey</param>
        protected virtual void CreateContainer(string containerId, string partitionKeyPath)
        {
            ThrowIfInvalidContainerId(containerId);
            ThrowIfInvalidPartitionKeyPath(partitionKeyPath);

            ThrowIfInvalidDatabase();

            CreateContainer<string, string, ContainerResponse>(containerId, partitionKeyPath, CreateContainerIfNotExists);
        }

        private void CreateContainer<FTIn1, FTIn2, FTOut>(
            string containerId, 
            string partitionKeyPath, 
            Func<FTIn1, FTIn2, FTOut> func)
        {
            if (Container == null || !containerId.Equals(ContainerId) || !partitionKeyPath.Equals(PartitionKeyPath))
                IsFirstTimeDatabaseCreation = true;

            bool _isFirstTime = IsFirstTimeDatabaseCreation;

            ContainerResponse response;
            var result = TryCreateResource(func, new dynamic[] { containerId, partitionKeyPath }, 
                ref _isFirstTime, out response);

            IsFirstTimeDatabaseCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceCosmosResponseSucceeded<Response<ContainerProperties>, ContainerProperties>(response))
            {
                Container = response;
                ContainerId = containerId;
                PartitionKeyPath = partitionKeyPath;
            }
        }

        private ContainerResponse CreateContainerIfNotExists(string containerId, string partitionKeyPath)
        {
            return Database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath).WaitAndUnwrapException();
        }

        #endregion
    }
}
