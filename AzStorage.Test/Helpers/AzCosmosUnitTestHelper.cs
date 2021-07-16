using AzCoreTools.Core;
using AzCoreTools.Utilities;
using AzStorage.Core.Cosmos;
using AzStorage.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CoreTools.Extensions;
using System.Threading;

namespace AzStorage.Test.Helpers
{
    internal class AzCosmosUnitTestHelper : UnitTestHelper
    {
        #region Environment variables

        private static string GetCosmosEndpointUri()
        {
            string cosmosEndpointUri;
            TestEnvironment.TryGetCosmosEndpointUri(out cosmosEndpointUri);

            return cosmosEndpointUri;
        }

        private static string _cosmosEndpointUri;
        protected static string AccountEndpointUri
        {
            get
            {
                if (string.IsNullOrEmpty(_cosmosEndpointUri))
                    _cosmosEndpointUri = GetCosmosEndpointUri();

                return _cosmosEndpointUri;
            }
        }

        private static string GetCosmosPrimaryKey()
        {
            string cosmosCosmosPrimaryKey;
            TestEnvironment.TryGetCosmosPrimaryKey(out cosmosCosmosPrimaryKey);

            return cosmosCosmosPrimaryKey;
        }

        private static string _cosmosPrimaryKey;
        protected static string AuthKeyOrResourceToken
        {
            get
            {
                if (string.IsNullOrEmpty(_cosmosPrimaryKey))
                    _cosmosPrimaryKey = GetCosmosPrimaryKey();

                return _cosmosPrimaryKey;
            }
        }

        #endregion

        #region Miscellaneous properties

        private static string DefaultDatabaseId
        {
            get
            {
                return "Database1";
            }
        }

        private static string DefaultContainerId
        {
            get
            {
                return "Container1";
            }
        }

        private static string DefaultPartitionKeyPropName
        {
            get
            {
                return "PartitionKey";
            }
        }

        #endregion

        #region Miscellaneous methods

        private static AzCosmosDBRepository _AzCosmosDBRepository;
        public static AzCosmosDBRepository GetOrCreateAzCosmosDBRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            if (_AzCosmosDBRepository == null)
                _AzCosmosDBRepository = CreateAzCosmosDBRepository(optionCreateIfNotExist);

            return _AzCosmosDBRepository;
        }

        private static AzCosmosDBRepository CreateAzCosmosDBRepository(CreateResourcePolicy optionCreateIfNotExist,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            var _azCosmosRetryOptions = new AzCosmosRetryOptions
            {
                Mode = Azure.Core.RetryMode.Fixed,
                Delay = new TimeSpan(0, 0, 1),
                MaxDelay = new TimeSpan(0, 1, 0),
                NetworkTimeout = new TimeSpan(0, 3, 0),
                MaxRetries = 5,
            };

            if (string.IsNullOrEmpty(databaseId))
                databaseId = DefaultDatabaseId;
            if (string.IsNullOrEmpty(containerId))
                containerId = DefaultContainerId;
            if (string.IsNullOrEmpty(partitionKeyPropName))
                partitionKeyPropName = DefaultPartitionKeyPropName;

            return new AzCosmosDBRepository(AccountEndpointUri, AuthKeyOrResourceToken, databaseId, containerId, partitionKeyPropName,
                optionCreateIfNotExist, _azCosmosRetryOptions);
        }

        public static CustomCosmosEntity CreateSomeEntity(string partitionKey = null, string id = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                partitionKey = Guid.Empty.ToString();
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();

            return new CustomCosmosEntity(partitionKey, id);
        }

        public static IEnumerable<CustomCosmosEntity> CreateSomeEntities(int amount, string commonPartitionKey)
        {
            if (string.IsNullOrEmpty(commonPartitionKey))
                return CreateSomeEntities(amount);

            return CreateSomeEntities(amount, new List<string> { commonPartitionKey });
        }

        public static IEnumerable<CustomCosmosEntity> CreateSomeEntities(int amount, List<string> partitionKeys = null)
        {
            var _r = new Random();
            var entities = new List<CustomCosmosEntity>(amount);
            for (int i = 0; i < amount; i++)
                if (partitionKeys == null)
                    entities.Add(CreateSomeEntity());
                else
                    entities.Add(CreateSomeEntity(partitionKey: partitionKeys[_r.Next(0, partitionKeys.Count)]));

            return entities;
        }

        public static string GenerateProp(int number, string prefix = "")
        {
            return $"{prefix}Prop{number}";
        }
        
        public static string GenerateUpdatedProp(int number)
        {
            return GenerateProp(number, "Updated");
        }

        #endregion

        #region Assert response

        public static void AssertExpectedSuccessfulGenResponse<T>(T sourceEntity, AzCosmosResponse<T> response)
            where T : BaseCosmosEntity
        {
            Assert.NotNull(sourceEntity);
            AssertExpectedSuccessfulGenResponse(response);

            var resultingEntity = response.Value;
            Assert.Equal(sourceEntity.PartitionKey, resultingEntity.PartitionKey);
            Assert.Equal(sourceEntity.Id, resultingEntity.Id);
        }

        #endregion

        #region Assert by GetEntities

        public static CustomCosmosEntity AssertByGetEntity(CustomCosmosEntity entity)
        {
            return AssertWithGetEntity(entity, true);
        }

        private static CustomCosmosEntity AssertWithGetEntity(
            CustomCosmosEntity entity,
            bool succeededResponse,
            string failedErrorMessage = null)
        {
            var _getEntityResponseAct = GetEntityByIdAsync<CustomCosmosEntity>(entity.PartitionKey, entity.Id)
                .WaitAndUnwrapException();
            if (succeededResponse)
            {
                AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
                return _getEntityResponseAct.Value;
            }

            AssertExpectedFailedGenResponse(_getEntityResponseAct, failedErrorMessage);

            return default;
        }

        #endregion

        #region Assert UpdateOrUpsert

        public static void AssertUpdateOrUpsertExistingEntity(
            bool addEntity,
            Func<CustomCosmosEntity, CreateResourcePolicy, Task<AzCosmosResponse<CustomCosmosEntity>>> funcUpdateOrUpsert,
            Action<CustomCosmosEntity> actionAssertRecoveredEntity)
        {
            // Arrange
            var entity = CreateSomeEntity();

            if (addEntity)
            {
                entity.Prop1 = GenerateProp(1);
                entity.Prop2 = default;
                var _addEntityResponseArr = AddEntityAsync(entity).WaitAndUnwrapException();
                AssertExpectedSuccessfulResponse(_addEntityResponseArr);
            }

            entity.Prop1 = default;
            entity.Prop2 = GenerateUpdatedProp(2);

            var _createResourcePolicy = CreateResourcePolicy.OnlyFirstTime;

            // Act
            var _updateReplaceEntityResponseAct = funcUpdateOrUpsert(entity, _createResourcePolicy).WaitAndUnwrapException();

            // Assert
            AssertExpectedSuccessfulResponse(_updateReplaceEntityResponseAct);

            var resultingEntity = AssertByGetEntity(entity);
            actionAssertRecoveredEntity(resultingEntity);
        }

        #endregion

        #region Add entities

        public static async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntityAsync(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Get entities

        public static async Task<AzCosmosResponse<T>> GetEntityByIdAsync<T>(string partitionKey,
            string id,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntityByIdAsync<T>(partitionKey,
                id, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Update entities

        public static async Task<AzCosmosResponse<TIn>> UpdateEntityAsync<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntityAsync(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static async Task<AzCosmosResponse<TIn>> UpdateEntityAsync2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntityAsync(entity,
                entity.Id, entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Upsert entities

        public static async Task<AzCosmosResponse<TIn>> UpsertEntityAsync<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntityAsync(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }

        public static async Task<AzCosmosResponse<TIn>> UpsertEntityAsync2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntityAsync(entity,
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion
    }

    public class CustomCosmosEntity : BaseCosmosEntity
    {
        public CustomCosmosEntity(string partitionKey, string id)
        {
            PartitionKey = partitionKey;
            Id = id;
        }

        public string Prop1 { get; set; }
        public string Prop2 { get; set; }
    }
}
