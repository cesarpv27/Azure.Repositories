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
using Microsoft.Azure.Cosmos;
using System.Linq;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Core.Validators;

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

        public static List<CustomCosmosEntity> CreateSomeEntities(int amount, string commonPartitionKey)
        {
            if (string.IsNullOrEmpty(commonPartitionKey))
                return CreateSomeEntities(amount);

            return CreateSomeEntities(amount, new List<string> { commonPartitionKey });
        }

        public static List<CustomCosmosEntity> CreateSomeEntities(int amount, List<string> partitionKeys = null)
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

        public static List<CustomCosmosEntity> CreateSomeEntities(int amount, bool scatterPartitionKeys, bool fillProps)
        {
            var entities = CreateSomeEntities(amount, scatterPartitionKeys);
            if (fillProps)
                entities.ForEach(entt =>
                {
                    entt.Prop1 = GenerateProp(1, "Cosmos_");
                    entt.Prop2 = GenerateProp(2, "Cosmos_");
                });

            return entities;
        }

        public static List<CustomCosmosEntity> CreateSomeEntities(int amount, bool scatterPartitionKeys)
        {
            if (!scatterPartitionKeys)
                return CreateSomeEntities(amount);

            var pKeysAmount = (amount / 100 / 2) + 1;
            var partitionKeys = new List<string>(pKeysAmount);
            for (int i = 0; i < pKeysAmount; i++)
                partitionKeys.Add(Guid.NewGuid().ToString());

            return CreateSomeEntities(amount, partitionKeys);
        }

        public static List<CustomCosmosEntity> CreateAddAssertSomeEntities(int amount, bool scatterPartitionKeys, bool fillProps)
        {
            var entities = CreateSomeEntities(amount, scatterPartitionKeys, fillProps);

            AddAssertSomeEntities(entities);

            return entities;
        }

        public static IEnumerable<CustomCosmosEntity> CreateAddAssertSomeEntities(
            bool scatterPartitionKeys,
            int randomMinValue = Utilities.ConstProvider.RandomMinValue,
            int randomMaxValue = Utilities.ConstProvider.RandomMaxValue)
        {
            return CreateAddAssertSomeEntities(
                scatterPartitionKeys,
                default,
                randomMinValue,
                randomMaxValue);
        }

        public static IEnumerable<CustomCosmosEntity> CreateAddAssertSomeEntities(
            string commonPartitionKey = default,
            int randomMinValue = Utilities.ConstProvider.RandomMinValue,
            int randomMaxValue = Utilities.ConstProvider.RandomMaxValue)
        {
            return CreateAddAssertSomeEntities(
                false,
                commonPartitionKey,
                randomMinValue,
                randomMaxValue);
        }

        private static IEnumerable<CustomCosmosEntity> CreateAddAssertSomeEntities(
            bool scatterPartitionKeys,
            string commonPartitionKey,
            int randomMinValue,
            int randomMaxValue)
        {
            var amount = new Random().Next(randomMinValue, randomMaxValue);
            IEnumerable<CustomCosmosEntity> entities;

            if (!string.IsNullOrEmpty(commonPartitionKey))
                entities = CreateSomeEntities(amount, commonPartitionKey);
            else
                entities = CreateSomeEntities(amount, scatterPartitionKeys);

            return AddAssertSomeEntities(entities);
        }

        public static IEnumerable<CustomCosmosEntity> AddAssertSomeEntities(IEnumerable<CustomCosmosEntity> entities)
        {
            var _addEntitiesTransactionallyResponseArr = AddEntitiesTransactionally(entities);
            AssertSucceededResponses(_addEntitiesTransactionallyResponseArr);

            return entities;
        }

        public static IEnumerable<CustomCosmosEntity> CreateSetPropsAddAssertSomeEntities(
            string prop1QueryValue1,
            string prop1QueryValue2,
            string prop1QueryValue3,
            string prop2QueryValue1,
            string prop2QueryValue2,
            string prop2QueryValue3,
            int randomMinValue = Utilities.ConstProvider.Hundreds_RandomMinValue,
            int randomMaxValue = Utilities.ConstProvider.Hundreds_RandomMaxValue)
        {
            var r = new Random();

            var amount = r.Next(randomMinValue, randomMaxValue);
            var entities = CreateSomeEntities(amount, true);

            var rangeAmount = entities.Count() / 3;
            for (int i = 0; i < rangeAmount; i++)
            {
                entities[i].Prop1 = prop1QueryValue1;
                entities[i].Prop2 = prop2QueryValue1;
            }
            for (int i = rangeAmount, length = rangeAmount * 2; i < length; i++)
            {
                entities[i].Prop1 = prop1QueryValue2;
                entities[i].Prop2 = prop2QueryValue2;
            }
            for (int i = rangeAmount * 2; i < entities.Count; i++)
            {
                entities[i].Prop1 = prop1QueryValue3;
                entities[i].Prop2 = prop2QueryValue3;
            }

            AddAssertSomeEntities(entities);

            return entities;
        }
        
        public static List<CustomCosmosEntity> CreateSetPropsAddAssertSetPropsSomeEntities(int amount, bool scatterPartitionKeys)
        {
            var entities = CreateSomeEntities(amount, scatterPartitionKeys);
            entities.ForEach(entt =>
            {
                entt.Prop1 = GenerateProp(1, "Cosmos_");
                entt.Prop2 = null;
            });

            var _addEntitiesTransactionallyAsyncResponseAct = AddEntitiesTransactionally(entities);
            AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);

            entities.ForEach(entt =>
            {
                entt.Prop1 = null;
                entt.Prop2 = GenerateProp(2, "Cosmos_");
            });

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

        #region Miscellaneous Asserts

        public static void AssertEnumerableBContainsEnumerableAEntities<T>(IEnumerable<T> enumerableA,
            IEnumerable<T> enumerableB) where T : CustomCosmosEntity
        {
            foreach (var entity in enumerableA)
                Assert.True(enumerableB.Where(entt => entt.Id.Equals(entity.Id)
                && entt.PartitionKey.Equals(entity.PartitionKey)).Count() == 1);
        }
        
        public static void AssertEnumerableBNotContainsAnyEnumerableAEntities<T>(IEnumerable<T> enumerableA,
            IEnumerable<T> enumerableB) where T : CustomCosmosEntity
        {
            foreach (var entity in enumerableA)
                Assert.True(enumerableB.Where(entt => entt.Id.Equals(entity.Id)
                && entt.PartitionKey.Equals(entity.PartitionKey)).Count() == 0);
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

        #region Assert responses

        public static void AssertSucceededResponses<T>(
            List<AzCosmosResponse<List<T>>> azCosmosResponse)
        {
            foreach (var _response in azCosmosResponse)
                AssertSucceededResponses(_response);
        }

        public static void AssertSucceededResponses<T>(
            AzCosmosResponse<List<T>> azCosmosResponse)
        {
            AssertResponses(azCosmosResponse, true);
        }

        private static void AssertResponses<T>(
            AzCosmosResponse<List<T>> azCosmosResponse,
            bool succeeded,
            string errorMessage = null)
        {
            if (succeeded)
                Assert.True(azCosmosResponse.Succeeded);
            else
                AssertExpectedFailedGenResponseWithException(azCosmosResponse, errorMessage);

            if (azCosmosResponse.Value != default)
                foreach (var val in azCosmosResponse.Value)
                {
                    if (val is string _str)
                    {
                        if (succeeded)
                            Assert.True(!string.IsNullOrEmpty(_str));
                        else
                            Assert.False(string.IsNullOrEmpty(_str));
                    }
                    else
                    {
                        if (succeeded)
                            Assert.NotNull(val);
                        else
                            Assert.Null(val);
                    }
                }
        }

        public static void AssertSucceededResponses(
            List<AzCosmosResponse<TransactionalBatchResponse>> azCosmosResponse)
        {
            foreach (var _response in azCosmosResponse)
                AssertSucceededResponses(_response);
        }

        public static void AssertSucceededResponses(
            AzCosmosResponse<TransactionalBatchResponse> azCosmosResponse)
        {
            AssertResponses(azCosmosResponse, true);
        }

        private static void AssertResponses(
            AzCosmosResponse<TransactionalBatchResponse> azCosmosResponse,
            bool succeeded,
            string errorMessage = null)
        {
            if (succeeded)
                Assert.True(azCosmosResponse.Succeeded);
            else
                AssertExpectedFailedGenResponseWithException(azCosmosResponse, errorMessage);

            if (azCosmosResponse.Value != default)
                foreach (var _response in azCosmosResponse.Value)
                {
                    if (succeeded)
                        Assert.True(ResponseValidator.StatusSucceeded(_response.StatusCode));
                    else
                        Assert.False(ResponseValidator.StatusSucceeded(_response.StatusCode));
                }
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

            AssertExpectedFailedGenResponseWithException(_getEntityResponseAct, failedErrorMessage);

            return default;
        }

        #endregion

        #region Assert UpdateOrUpsert

        public static void AssertUpdateOrUpsertExistingEntity(
            bool addEntity,
            Func<CustomCosmosEntity, CreateResourcePolicy, AzCosmosResponse<CustomCosmosEntity>> funcUpdateOrUpsert,
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
            var _updateReplaceEntityResponseAct = funcUpdateOrUpsert(entity, _createResourcePolicy);

            // Assert
            AssertExpectedSuccessfulGenResponse(_updateReplaceEntityResponseAct);

            var resultingEntity = AssertByGetEntity(entity);
            actionAssertRecoveredEntity(resultingEntity);
        }

        #endregion
        
        #region Assert UpdateOrUpsert async

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
            AssertExpectedSuccessfulGenResponse(_updateReplaceEntityResponseAct);

            var resultingEntity = AssertByGetEntity(entity);
            actionAssertRecoveredEntity(resultingEntity);
        }

        #endregion

        #region Samples_AzCosmosDBRepository tests

        public static void CommonQueryAllTest<T, TOutGen>(Func<CreateResourcePolicy, AzCosmosResponse<TOutGen>> func)
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            var entities = CreateAddAssertSomeEntities(true,
                Utilities.ConstProvider.Hundreds_RandomMinValue, Utilities.ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _queryAllResponseAct = func(CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct);

            var storedEntities = _queryAllResponseAct.Value.ToList();
            AssertEnumerableBContainsEnumerableAEntities(entities, storedEntities);
        }

        public static void CommonQueryByPartitionKeyTest<T, TOutGen>(Func<string, string, string, CreateResourcePolicy, AzCosmosResponse<TOutGen>> func)
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            var entities = CreateAddAssertSomeEntities(true,
                Utilities.ConstProvider.Hundreds_RandomMinValue, Utilities.ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            // Act
            var _queryByPartitionKeyResponseAct = func(partitionKey, null, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponseAct);

            var responseEntities = _queryByPartitionKeyResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count);
            AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        public static void CommonQueryByFilterTest<T, TOutGen>(Func<string, string, string, CreateResourcePolicy, AzCosmosResponse<TOutGen>> func)
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            var entities = CreateAddAssertSomeEntities(true,
                Utilities.ConstProvider.Hundreds_RandomMinValue, Utilities.ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string filter = $"select * from Container c1 where c1.PartitionKey = '{partitionKey}'";

            // Act
            var _queryByFilterResponseAct = func(filter, null, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByFilterResponseAct);

            var responseEntities = _queryByFilterResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count);
            AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        public static void CommonQueryByQueryDefinitionTest<T, TOutGen>(Func<QueryDefinition, CreateResourcePolicy, AzCosmosResponse<TOutGen>> func)
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            var entities = CreateAddAssertSomeEntities(true,
                Utilities.ConstProvider.Hundreds_RandomMinValue, Utilities.ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string queryText = $"select * from Container c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _queryByQueryDefinitionResponseAct = func(queryDefinition, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByQueryDefinitionResponseAct);

            var responseEntities = _queryByQueryDefinitionResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        public static void CommonQueryWithAndOrTest<T, TOutGen>(Func<dynamic, string, string,
            CreateResourcePolicy, AzCosmosResponse<TOutGen>> func, CoreTools.Utilities.BooleanOperator boolOperator) 
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            var r = new Random();

            var amount = r.Next(Utilities.ConstProvider.Hundreds_RandomMinValue, Utilities.ConstProvider.Hundreds_RandomMaxValue);
            var entities = CreateSomeEntities(amount, true);

            foreach (var item in entities)
            {
                item.Prop1 = r.Next(1, 4).ToString();
                item.Prop2 = r.Next(1, 4).ToString();
            }

            AddAssertSomeEntities(entities);

            var prop1QueryValue = "1";
            var prop2QueryValue = "2";

            // Act
            var _queryWithAndOrResponseAct = func(
                new
                {
                    Prop1 = prop1QueryValue,
                    Prop2 = prop2QueryValue
                }, null, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryWithAndOrResponseAct);

            IEnumerable<CustomCosmosEntity> locallyQueriedEntities = null;
            switch (boolOperator)
            {
                case CoreTools.Utilities.BooleanOperator.and:
                    locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue) && entt.Prop2.Equals(prop2QueryValue));
                    break;
                case CoreTools.Utilities.BooleanOperator.or:
                    locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue) || entt.Prop2.Equals(prop2QueryValue));
                    break;
                default:
                    ExThrower.ST_ThrowNotImplementedException(boolOperator);
                    break;
            }
            var responseEntities = _queryWithAndOrResponseAct.Value.ToList();

            Assert.True(responseEntities.Count() >= locallyQueriedEntities.Count());
            AssertEnumerableBContainsEnumerableAEntities(locallyQueriedEntities, responseEntities);
        }

        public static void CommonQueryWithAndOrTest2<T, TOutGen>(Func<IEnumerable<KeyValuePair<string, string>>, string, string, 
            CreateResourcePolicy, AzCosmosResponse<TOutGen>> func, CoreTools.Utilities.BooleanOperator boolOperator) 
            where T : CustomCosmosEntity where TOutGen : IEnumerable<T>
        {
            // Arrange
            string prop1QueryValue1 = "4";
            string prop1QueryValue2 = "5";
            string prop1QueryValue3 = "6";

            string prop2QueryValue1 = "1";
            string prop2QueryValue2 = "2";
            string prop2QueryValue3 = "3";

            var entities = CreateSetPropsAddAssertSomeEntities(
                prop1QueryValue1, prop1QueryValue2, prop1QueryValue3,
                prop2QueryValue1, prop2QueryValue2, prop2QueryValue3);

            List<KeyValuePair<string, string>> param = null;
            switch (boolOperator)
            {
                case CoreTools.Utilities.BooleanOperator.and:
                    param = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Prop1", prop1QueryValue1),
                        new KeyValuePair<string, string>("Prop2", prop2QueryValue1),
                    };
                    break;
                case CoreTools.Utilities.BooleanOperator.or:
                    param = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Prop1", prop1QueryValue3),
                        new KeyValuePair<string, string>("Prop2", prop2QueryValue1),
                        new KeyValuePair<string, string>("Prop2", prop2QueryValue2),
                    };
                    break;
                default:
                    ExThrower.ST_ThrowNotImplementedException(boolOperator);
                    break;
            }

            // Act
            var _queryWithAndOrResponseAct = func(param, null, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryWithAndOrResponseAct);

            IEnumerable<CustomCosmosEntity> locallyQueriedEntities = null;
            switch (boolOperator)
            {
                case CoreTools.Utilities.BooleanOperator.and:
                    locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue1) && entt.Prop2.Equals(prop2QueryValue1));
                    break;
                case CoreTools.Utilities.BooleanOperator.or:
                    locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue3) || entt.Prop2.Equals(prop2QueryValue1) 
                        || entt.Prop2.Equals(prop2QueryValue2));
                    break;
                default:
                    ExThrower.ST_ThrowNotImplementedException(boolOperator);
                    break;
            }

            var responseEntities = _queryWithAndOrResponseAct.Value.ToList();

            Assert.True(responseEntities.Count() >= locallyQueriedEntities.Count());
            AssertEnumerableBContainsEnumerableAEntities(locallyQueriedEntities, responseEntities);
        }

        #endregion

        #region Add entities

        public static AzCosmosResponse<TIn> AddEntity<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntity(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static AzCosmosResponse<TIn> AddEntity2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntity(entity,
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion
        
        #region Add async entities

        public static async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntityAsync(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static async Task<AzCosmosResponse<TIn>> AddEntityAsync2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntityAsync(entity,
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        public static async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            string databaseId,
            string containerId,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntityAsync(entity,
                cancellationToken: default, databaseId: databaseId, containerId: containerId);
        }

        #endregion

        #region Add transactionally entities (sync & async)

        public static List<AzCosmosResponse<TransactionalBatchResponse>> AddEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntitiesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<TransactionalBatchResponse>>> AddEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).AddEntitiesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        #endregion

        #region Get entities

        public static AzCosmosResponse<T> GetEntityById<T>(string partitionKey,
            string id,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntityById<T>(partitionKey,
                id, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Get async entities

        public static async Task<AzCosmosResponse<T>> GetEntityByIdAsync<T>(string partitionKey,
            string id,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntityByIdAsync<T>(partitionKey,
                id, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Get transactionally entities (sync & async)

        public static List<AzCosmosResponse<List<T>>> GetEntitiesTransactionally<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntitiesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<List<T>>>> GetEntitiesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntitiesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        public static List<AzCosmosResponse<List<string>>> GetEntitiesAsStringTransactionally<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntitiesAsStringTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<List<string>>>> GetEntitiesAsStringTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntitiesAsStringTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static List<AzCosmosResponse<TransactionalBatchResponse>> GetEntitiesResponsesTransactionally<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntitiesResponsesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<TransactionalBatchResponse>>> GetEntityResponsesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).GetEntityResponsesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        #endregion

        #region Query entities

        #region QueryAll

        public static AzCosmosResponse<List<T>> QueryAll<T>(
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryAll<T>();
        }
        
        public static AzCosmosResponse<List<T>> QueryAll<T>(
            int take,
            string continuationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist)
                .QueryAll<T>(take: take, continuationToken: continuationToken);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryAll<T>(
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryAll<T>();
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryAllAsync<T>(
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryAllAsync<T>();
        }

        #endregion

        #region QueryByFilter

        public static AzCosmosResponse<List<T>> QueryByFilter<T>(string filter,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByFilter<T>(filter,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryByFilter<T>(string filter,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryByFilter<T>(filter,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryByFilterAsync<T>(string filter,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByFilterAsync<T>(filter,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        #endregion

        #region QueryByQueryDefinition

        public static AzCosmosResponse<List<T>> QueryByQueryDefinition<T>(QueryDefinition queryDefinition,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByQueryDefinition<T>(queryDefinition);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryByQueryDefinition<T>(QueryDefinition queryDefinition,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryByQueryDefinition<T>(queryDefinition);
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryByQueryDefinitionAsync<T>(QueryDefinition queryDefinition,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByQueryDefinitionAsync<T>(queryDefinition);
        }

        #endregion

        #region QueryByPartitionKey

        public static AzCosmosResponse<List<T>> QueryByPartitionKey<T>(string partitionKey,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByPartitionKey<T>(partitionKey,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryByPartitionKey<T>(string partitionKey,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryByPartitionKey<T>(partitionKey,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryByPartitionKeyAsync<T>(string partitionKey,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryByPartitionKeyAsync<T>(partitionKey,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        #endregion

        #region QueryWithOr

        public static AzCosmosResponse<List<T>> QueryWithOr<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithOr<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<List<T>> QueryWithOr<T>(IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithOr<T>(nameValueProperties,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryWithOr<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryWithOr<T>(nameValueProperties,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryWithOrAsync<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithOrAsync<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        #endregion

        #region LazyQueryWithOr

        public static AzCosmosResponse<List<T>> QueryWithAnd<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithAnd<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<List<T>> QueryWithAnd<T>(IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithAnd<T>(nameValueProperties,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryWithAnd<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).LazyQueryWithAnd<T>(nameValueProperties,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        public static async Task<AzCosmosResponse<List<T>>> QueryWithAndAsync<T>(dynamic operationTerms,
            string containerId = null,
            string partitionKeyPropName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).QueryWithAndAsync<T>(operationTerms,
                containerId: containerId, partitionKeyPropName: partitionKeyPropName);
        }

        #endregion

        #endregion

        #region Update entities

        public static AzCosmosResponse<TIn> UpdateEntity<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntity(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static AzCosmosResponse<TIn> UpdateEntity2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntity(entity,
                entity.Id, entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion
        
        #region Update async entities

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

        #region Update transactionally entities sync & async

        public static List<AzCosmosResponse<TransactionalBatchResponse>> UpdateEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntitiesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpdateEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpdateEntitiesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        #endregion

        #region Upsert entities

        public static AzCosmosResponse<TIn> UpsertEntity<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntity(entity,
                cancellationToken: default, databaseId: default, containerId: default);
        }

        public static AzCosmosResponse<TIn> UpsertEntity2<TIn>(TIn entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntity(entity,
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion
        
        #region Upsert entities async

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

        #region Upsert transactionally entities sync & async

        public static List<AzCosmosResponse<TransactionalBatchResponse>> UpsertEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntitiesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }
        
        public static Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpsertEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).UpsertEntitiesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        #endregion

        #region Delete entities

        public static AzCosmosResponse<T> DeleteEntity<T>(T entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntity(entity, 
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static AzCosmosResponse<T> DeleteEntity2<T>(T entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntity<T>(entity.Id, 
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Delete entities async

        public static async Task<AzCosmosResponse<T>> DeleteEntityAsync<T>(T entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntityAsync(entity, 
                cancellationToken: default, databaseId: default, containerId: default);
        }
        
        public static async Task<AzCosmosResponse<T>> DeleteEntityAsync2<T>(T entity,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : BaseCosmosEntity
        {
            return await GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntityAsync<T>(entity.Id, 
                entity.PartitionKey, cancellationToken: default, databaseId: default, containerId: default);
        }

        #endregion

        #region Delete transactionally entities sync & async

        public static List<AzCosmosResponse<TransactionalBatchResponse>> DeleteEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntitiesTransactionally(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
        }

        public static Task<List<AzCosmosResponse<TransactionalBatchResponse>>> DeleteEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : BaseCosmosEntity
        {
            return GetOrCreateAzCosmosDBRepository(optionCreateIfNotExist).DeleteEntitiesTransactionallyAsync(entities,
                cancellationToken: default, databaseId: default, containerId: default, partitionKeyPropName: default);
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
