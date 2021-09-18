using AzCoreTools.Core;
using AzCoreTools.Utilities;
using AzStorage.Repositories;
using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Linq;
using AzCoreTools.Core.Validators;
using AzStorage.Core.Tables;
using System.Threading.Tasks;
using CoreTools.Extensions;

namespace AzStorage.Test.Helpers
{
    internal class AzTableUnitTestHelper : AzStorageUnitTestHelper
    {
        #region Miscellaneous methods

        private static AzTableRepository _AzTableRepository;
        public static AzTableRepository GetOrCreateAzTableRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            if (_AzTableRepository == null)
                _AzTableRepository = CreateAzTableRepository(optionCreateIfNotExist);

            return _AzTableRepository;
        }

        private static AzTableRepository CreateAzTableRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            var _azTableRetryOptions = new AzTableRetryOptions
            {
                Mode = Azure.Core.RetryMode.Fixed,
                Delay = new TimeSpan(0, 0, 1),
                MaxDelay = new TimeSpan(0, 1, 0),
                NetworkTimeout = new TimeSpan(0, 3, 0),
                MaxRetries = 5,
            };

            return new AzTableRepository(StorageConnectionString, optionCreateIfNotExist, _azTableRetryOptions);
        }

        public static TableClient GetExistingTableClient(string tableName)
        {
            return new TableClient(StorageConnectionString, tableName);
        }

        public static TableEntity CreateSomeEntity(string partitionKey = null, string rowKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                partitionKey = Guid.Empty.ToString();
            if (string.IsNullOrEmpty(rowKey))
                rowKey = Guid.NewGuid().ToString();

            return new TableEntity(partitionKey, rowKey);
        }

        public static IEnumerable<TableEntity> CreateSomeEntities(int amount, string commonPartitionKey)
        {
            if (string.IsNullOrEmpty(commonPartitionKey))
                return CreateSomeEntities(amount);

            return CreateSomeEntities(amount, new List<string> { commonPartitionKey });
        }

        public static IEnumerable<TableEntity> CreateSomeEntities(int amount, List<string> partitionKeys = null)
        {
            var _r = new Random();
            var entities = new List<TableEntity>(amount);
            for (int i = 0; i < amount; i++)
                if (partitionKeys == null)
                    entities.Add(CreateSomeEntity());
                else
                    entities.Add(CreateSomeEntity(partitionKey: partitionKeys[_r.Next(0, partitionKeys.Count)]));

            return entities;
        }

        public static IEnumerable<TableEntity> CreateSomeEntities(int amount, bool scatterPartitionKeys)
        {
            if (!scatterPartitionKeys)
                return CreateSomeEntities(amount);

            var pKeysAmount = (amount / 100 / 2) + 1;
            var partitionKeys = new List<string>(pKeysAmount);
            for (int i = 0; i < pKeysAmount; i++)
                partitionKeys.Add(Guid.NewGuid().ToString());

            return CreateSomeEntities(amount, partitionKeys);
        }

        public static TableEntity CreateAddAssertSomeEntity()
        {
            var entity = CreateSomeEntity();
            var _addEntityResponseArr = AddEntity(entity);
            AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            return entity;
        }

        public static IEnumerable<TableEntity> CreateAddAssertSomeEntities(
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

        public static IEnumerable<TableEntity> CreateAddAssertSomeEntities(
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

        private static IEnumerable<TableEntity> CreateAddAssertSomeEntities(
            bool scatterPartitionKeys,
            string commonPartitionKey,
            int randomMinValue,
            int randomMaxValue)
        {
            var amount = new Random().Next(randomMinValue, randomMaxValue);
            IEnumerable<TableEntity> entities;

            if (!string.IsNullOrEmpty(commonPartitionKey))
                entities = CreateSomeEntities(amount, commonPartitionKey);
            else
                entities = CreateSomeEntities(amount, scatterPartitionKeys);

            var _addEntitiesTransactionallyResponseArr = AddEntitiesTransactionally(entities);
            AssertSucceededResponses(_addEntitiesTransactionallyResponseArr);

            return entities;
        }

        static string testPropKey = "Test_{0}_PropKey";
        static string testPropValue = "Test_{0}_PropValue";

        public static void AddTestProp(TableEntity entity, int number)
        {
            entity.Add(GenerateTestPropKey(number), GenerateTestPropValue(number));
        }

        public static void RemoveTestProp(TableEntity entity, int number)
        {
            entity.Remove(GenerateTestPropKey(number));
        }

        public static string GenerateTestPropKey(int number)
        {
            return string.Format(testPropKey, number);
        }

        public static string GenerateTestPropValue(int number)
        {
            return string.Format(testPropValue, number);
        }

        public static string GetTableEntityName()
        {
            return typeof(TableEntity).Name;
        }

        #endregion

        #region Assert response

        public static void AssertExpectedSuccessfulGenResponse(TableEntity sourceEntity, AzStorageResponse<TableEntity> response)
        {
            Assert.NotNull(sourceEntity);
            AssertExpectedSuccessfulGenResponse(response);

            var resultingEntity = response.Value;
            Assert.Equal(sourceEntity.PartitionKey, resultingEntity.PartitionKey);
            Assert.Equal(sourceEntity.RowKey, resultingEntity.RowKey);
        }

        public static void AssertExpectedSuccessfulGenResponse<T>(AzStorageResponse<List<T>> response,
            IEnumerable<T> originalEntities, Func<List<T>, IEnumerable<T>, bool> funcToAssertTrue)
            where T : class, ITableEntity, new()
        {
            AssertExpectedSuccessfulGenResponse(response);

            Assert.NotNull(originalEntities);
            var responseValues = response.Value;
            Assert.True(funcToAssertTrue(responseValues, originalEntities));

            foreach (var item in originalEntities)
                Assert.Single(responseValues.Where(entt =>
                {
                    Assert.NotNull(entt);
                    return item.PartitionKey.Equals(entt.PartitionKey) &&
                        item.RowKey.Equals(entt.RowKey);
                }));
        }

        #endregion

        #region Assert by GetEntities

        public static TableEntity AssertByGetEntity(TableEntity entity)
        {
            return AssertWithGetEntity(entity, true);
        }

        public static List<TableEntity> AssertByGetEntity(IEnumerable<TableEntity> entities)
        {
            return AssertWithGetEntity(entities, true);
        }

        public static TableEntity AssertByExpectedFailedGetEntity(TableEntity entity,
            string errorMessage)
        {
            return AssertWithGetEntity(entity, false, errorMessage);
        }
        
        public static List<TableEntity> AssertByExpectedFailedGetEntity(IEnumerable<TableEntity> entities,
            string errorMessage)
        {
            return AssertWithGetEntity(entities, false, errorMessage);
        }

        private static List<TableEntity> AssertWithGetEntity(
            IEnumerable<TableEntity> entities,
            bool succeededResponse,
            string failedErrorMessage = null)
        {
            var recoveredEntities = new List<TableEntity>(entities.Count());
            TableEntity tmpEntity;
            foreach (var entt in entities)
            {
                tmpEntity = AssertWithGetEntity(entt, succeededResponse, failedErrorMessage);
                if (tmpEntity != default)
                    recoveredEntities.Add(tmpEntity);
            }

            return recoveredEntities;
        }

        private static TableEntity AssertWithGetEntity(
            TableEntity entity,
            bool succeededResponse,
            string failedErrorMessage = null)
        {
            var _getEntityResponseAct = GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey);
            if (succeededResponse)
            {
                AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
                return _getEntityResponseAct.Value;
            }
                
            AssertExpectedFailedGenResponse(_getEntityResponseAct, failedErrorMessage);

            return default;
        }

        #endregion

        #region Assert responses

        public static void AssertSucceededResponses(AzStorageResponse<IReadOnlyList<Response>> azStorageResponse)
        {
            AssertResponses(azStorageResponse, true);
        }

        public static void AssertExpectedFailedResponses(AzStorageResponse<IReadOnlyList<Response>> azStorageResponse,
            string errorMessage)
        {
            AssertResponses(azStorageResponse, false, errorMessage);
        }

        public static void AssertSucceededResponses(List<AzStorageResponse<IReadOnlyList<Response>>> azStorageResponses)
        {
            foreach (var _response in azStorageResponses)
                AssertSucceededResponses(_response);
        }

        public static void AssertExpectedFailedResponses(List<AzStorageResponse<IReadOnlyList<Response>>> azStorageResponses,
            string errorMessage)
        {
            foreach (var _response in azStorageResponses)
                AssertExpectedFailedResponses(_response, errorMessage);
        }

        private static void AssertResponses(
            AzStorageResponse<IReadOnlyList<Response>> azStorageResponse,
            bool succeeded,
            string errorMessage = null)
        {
            if (succeeded)
                Assert.True(azStorageResponse.Succeeded);
            else
                AssertExpectedFailedGenResponse(azStorageResponse, errorMessage);

            if (azStorageResponse.Value != default)
                foreach (var _response in azStorageResponse.Value)
                {
                    if (succeeded)
                        Assert.True(ResponseValidator.StatusSucceeded(_response.Status));
                    else
                        Assert.False(ResponseValidator.StatusSucceeded(_response.Status));
                }
        }

        #endregion

        #region Assert UpdateOrUpsert

        public static void AssertUpdateOrUpsertExistingEntity(Func<TableEntity, string, CreateResourcePolicy, AzStorageResponse> funcUpdateOrUpsert, Action<TableEntity> actionAssertRecoveredEntity)
        {
            // Arrange
            var entity = CreateSomeEntity();
            AddTestProp(entity, 1);
            var _addEntityResponseArr = AddEntity(entity);
            AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            RemoveTestProp(entity, 1);
            AddTestProp(entity, 2);

            // Act
            var _updateReplaceEntityResponseAct = funcUpdateOrUpsert(entity, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            AssertExpectedSuccessfulResponse(_updateReplaceEntityResponseAct);

            var resultingEntity = AssertByGetEntity(entity);
            actionAssertRecoveredEntity(resultingEntity);
        }

        public static void AssertUpdateOrUpsertExistingEntitiesTransactionally(
            Func<IEnumerable<TableEntity>, string, CreateResourcePolicy, List<AzStorageResponse<IReadOnlyList<Response>>>> funcUpdate, Action<TableEntity> actionAssertRecoveredEntity)
        {
            // Arrange
            var entities = CreateSomeEntities(GetOverOneHundredRandomValue());

            foreach (var entt in entities)
                AddTestProp(entt, 1);

            var _addEntitiesTransactionallyResponse = AddEntitiesTransactionally(entities);
            AssertSucceededResponses(_addEntitiesTransactionallyResponse);

            foreach (var entt in entities)
            {
                RemoveTestProp(entt, 1);
                AddTestProp(entt, 2);
            }

            // Act
            var _updateEntitiesTransactionallyResponse = funcUpdate(entities, null, CreateResourcePolicy.OnlyFirstTime);

            // Assert
            AssertSucceededResponses(_updateEntitiesTransactionallyResponse);

            //var recoveredEntities = AssertByGetEntity(entities);
            //foreach (var recEntt in recoveredEntities)
            //    actionAssertRecoveredEntity(recEntt);
        }

        #endregion

        #region AzTableRepository methods

        #region Add entities

        public static AzStorageResponse AddEntity<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).AddEntity(entity, tableName: tableName);
        }

        public static int AddEntitiesParallelForEach<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).AddEntitiesParallelForEach(entities, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> AddEntitiesTransactionally<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).AddEntitiesTransactionally(entities, tableName: tableName);
        }

        #endregion

        #region Add async

        public static async Task<AzStorageResponse> AddEntityAsync<TIn>(TIn entity)
            where TIn : class, ITableEntity, new()
        {
            return await GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime).AddEntityAsync(entity, default);
        }

        public static async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> AddEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities)
            where TIn : class, ITableEntity, new()
        {
            return await GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime)
                .AddEntitiesTransactionallyAsync(entities);
        }

        #endregion

        #region Get entity

        public static AzStorageResponse<T> GetEntity<T>(string partitionKey, string rowKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).GetEntity<T>(partitionKey, rowKey, tableName: tableName);
        }

        public static Task<AzStorageResponse<T>> GetEntityAsync<T>(string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime).GetEntityAsync<T>(
                partitionKey, rowKey, tableName: null);
        }

        #endregion

        #region Query entities

        public static AzStorageResponse<List<T>> QueryAll<T>(int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryAll<T>(take: take, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByPartitionKey<T>(
            string partitionKey,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKey<T>(
                partitionKey, take: take, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByPartitionKeyStartPattern<T>(
            string startPattern,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKeyStartPattern<T>(
                startPattern, take: take, tableName: tableName);
        }

        public static AzStorageResponse<T> QueryByPartitionKeyRowKey<T>(
            string partitionKey,
            string rowKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKeyRowKey<T>(
                partitionKey, rowKey, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByPartitionKeyRowKeyStartPattern<T>(
            string partitionKey,
            string rowKeyStartPattern,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKeyRowKeyStartPattern<T>(
                partitionKey, rowKeyStartPattern, take: take, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByPartitionKeyStartPatternRowKeyStartPattern<T>(
            string partitionKeyStartPattern,
            string rowKeyStartPattern,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKeyStartPatternRowKeyStartPattern<T>(
                partitionKeyStartPattern, rowKeyStartPattern, take: take, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByTimestamp<T>(
            DateTime timeStampFrom,
            DateTime timeStampTo,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByTimestamp<T>(
                timeStampFrom, timeStampTo, take: take, tableName: tableName);
        }

        public static AzStorageResponse<List<T>> QueryByPartitionKeyTimestamp<T>(
            string partitionKey,
            DateTime timeStampFrom,
            DateTime timeStampTo,
            int take = int.MaxValue,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).QueryByPartitionKeyTimestamp<T>(
                partitionKey, timeStampFrom, timeStampTo, take: take, tableName: tableName);
        }

        #endregion

        #region Update

        public static AzStorageResponse UpdateMergeEntity<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntity(entity, TableUpdateMode.Merge, tableName: tableName);
        }

        public static AzStorageResponse UpdateReplaceEntity<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntity(entity, TableUpdateMode.Replace, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpdateMergeEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntitiesTransactionally(entities, TableUpdateMode.Merge, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpdateReplaceEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntitiesTransactionally(entities, TableUpdateMode.Replace, tableName: tableName);
        }

        #endregion

        #region Update async

        public static AzStorageResponse UpdateMergeEntityAsync<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpdateEntityAsync(entity, TableUpdateMode.Merge, tableName: tableName).WaitAndUnwrapException();
        }

        public static AzStorageResponse UpdateReplaceEntityAsync<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpdateEntityAsync(entity, TableUpdateMode.Replace, tableName: tableName).WaitAndUnwrapException();
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpdateMergeEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntitiesTransactionallyAsync(
                entities, TableUpdateMode.Merge, tableName: tableName).WaitAndUnwrapException();
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpdateReplaceEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateEntitiesTransactionallyAsync(
                entities, TableUpdateMode.Replace, tableName: tableName).WaitAndUnwrapException();
        }

        #endregion

        #region Upsert

        public static AzStorageResponse UpsertMergeEntity<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpsertEntity(entity, TableUpdateMode.Merge, tableName: tableName);
        }

        public static AzStorageResponse UpsertReplaceEntity<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpsertEntity(entity, TableUpdateMode.Replace, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpsertMergeEntitiesTransactionally<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpsertEntitiesTransactionally(entities, TableUpdateMode.Merge, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpsertReplaceEntitiesTransactionally<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpsertEntitiesTransactionally(entities, TableUpdateMode.Replace, tableName: tableName);
        }

        #endregion

        #region Upsert async

        public static AzStorageResponse UpsertMergeEntityAsync<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpsertEntityAsync(entity, TableUpdateMode.Merge, tableName: tableName).WaitAndUnwrapException();
        }

        public static AzStorageResponse UpsertReplaceEntityAsync<TIn>(TIn entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpsertEntityAsync(entity, TableUpdateMode.Replace, tableName: tableName).WaitAndUnwrapException();
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpsertMergeEntitiesTransactionallyAsync<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpsertEntitiesTransactionallyAsync(entities, TableUpdateMode.Merge, tableName: tableName)
                .WaitAndUnwrapException();
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> UpsertReplaceEntitiesTransactionallyAsync<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist)
                .UpsertEntitiesTransactionallyAsync(entities, TableUpdateMode.Replace, tableName: tableName)
                .WaitAndUnwrapException();
        }

        #endregion

        #region Delete entities

        public static AzStorageResponse DeleteEntity<T>(T entity,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntity(entity, tableName: tableName);
        }
        
        public static AzStorageResponse DeleteEntity<T>(string partitionKey, string rowKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntity<T>(partitionKey, rowKey, tableName: tableName);
        }
        
        public static AzStorageResponse DeleteEntity(string partitionKey, string rowKey,
            string tableName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntity(partitionKey, rowKey, tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKey<T>(
            string partitionKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKey<T>(partitionKey, tableName: tableName);
        }
        
        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKey(
            string partitionKey,
            string tableName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKey(partitionKey, tableName);
        }
        
        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyStartPattern<T>(
            string partitionKeyStartPattern,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKeyStartPattern<T>(partitionKeyStartPattern, tableName: tableName);
        }
        
        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyStartPattern(
            string partitionKeyStartPattern,
            string tableName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKeyStartPattern(partitionKeyStartPattern, tableName);
        }
        
        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyRowKeyStartPattern<T>(
            string partitionKey,
            string rowKeyStartPattern,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKeyRowKeyStartPattern<T>(
                partitionKey, rowKeyStartPattern, tableName: tableName);
        }
        
        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyRowKeyStartPattern(
            string partitionKey,
            string rowKeyStartPattern,
            string tableName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesByPartitionKeyRowKeyStartPattern(
                partitionKey, rowKeyStartPattern, tableName);
        }

        public static int DeleteEntitiesParallelForEach<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesParallelForEach(entities, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesTransactionally<TIn>(IEnumerable<TIn> entities,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).DeleteEntitiesTransactionally(entities, tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable<T>(
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).ClearTable<T>(tableName: tableName);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable(
            string tableName,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).ClearTable(tableName);
        }

        #endregion

        #region Delete entities async

        public static async Task<AzStorageResponse> DeleteEntityAsync<T>(T entity)
            where T : class, ITableEntity, new()
        {
            return await GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime).DeleteEntityAsync(entity, tableName: default);
        }

        public static async Task<AzStorageResponse> DeleteEntityAsync<T>(string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            return await GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime).DeleteEntityAsync<T>(partitionKey, rowKey);
        }
        
        public static async Task<AzStorageResponse> DeleteEntityAsync(string partitionKey, string rowKey)
        {
            return await GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime).DeleteEntityAsync(partitionKey, rowKey, GetTableEntityName());
        }

        public static Task<List<AzStorageResponse<IReadOnlyList<Response>>>> DeleteEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities)
            where TIn : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(CreateResourcePolicy.OnlyFirstTime)
                .DeleteEntitiesTransactionallyAsync(entities);
        }

        #endregion

        #region UpdateKeys

        public static AzStorageResponse UpdateKeys<T>(
            string partitionKey, string rowKey,
            string newPartitionKey, string newRowKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateKeys<T>(partitionKey,
                rowKey, newPartitionKey, newRowKey, tableName: tableName);
        }
        
        public static AzStorageResponse UpdatePartitionKey<T>(
            string partitionKey, string rowKey,
            string newPartitionKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdatePartitionKey<T>(partitionKey,
                rowKey, newPartitionKey, tableName: tableName);
        }
        
        public static AzStorageResponse UpdateRowKey<T>(
            string partitionKey, string rowKey,
            string newRowKey,
            string tableName = null,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
            where T : class, ITableEntity, new()
        {
            return GetOrCreateAzTableRepository(optionCreateIfNotExist).UpdateRowKey<T>(partitionKey,
                rowKey, newRowKey, tableName: tableName);
        }

        #endregion

        #endregion
    }
}
