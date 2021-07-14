using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using CoreTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample6_DeleteEntity
    {
        [Fact, TestPriority(100)]
        public void DeleteEntityTest1()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = AzTableUnitTestHelper.DeleteEntity(entity);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityTest2()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = AzTableUnitTestHelper.DeleteEntity<TableEntity>(entity.PartitionKey, entity.RowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityTest3()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = AzTableUnitTestHelper.DeleteEntity(entity.PartitionKey, entity.RowKey, AzTableUnitTestHelper.GetTableEntityName());

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyResponses = AzTableUnitTestHelper.DeleteEntitiesByPartitionKey<TableEntity>(commonPartitionKey);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyResponses = AzTableUnitTestHelper.DeleteEntitiesByPartitionKey(commonPartitionKey, AzTableUnitTestHelper.GetTableEntityName());

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyStartPatternTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyStartPatternResponses = AzTableUnitTestHelper
                .DeleteEntitiesByPartitionKeyStartPattern<TableEntity>(commonPartitionKey.Substring(0, 5));

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyStartPatternResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyStartPatternTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyStartPatternResponses = AzTableUnitTestHelper
                .DeleteEntitiesByPartitionKeyStartPattern<TableEntity>(commonPartitionKey.Substring(0, 5), AzTableUnitTestHelper.GetTableEntityName());

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyStartPatternResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyRowKeyStartPatternTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey).ToList();
            var selectedEntity = entities[entities.Count - 1];

            // Act
            var _deleteEntitiesByPartitionKeyRowKeyStartPatternResponses = AzTableUnitTestHelper
                .DeleteEntitiesByPartitionKeyRowKeyStartPattern<TableEntity>(selectedEntity.PartitionKey,
                selectedEntity.RowKey.Substring(0, 5));

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyRowKeyStartPatternResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(new List<TableEntity> { selectedEntity },
                ConstProvider.The_specified_resource_does_not_exist);

            entities.RemoveAt(entities.Count - 1);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyRowKeyStartPatternTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey).ToList();
            var selectedEntity = entities[entities.Count - 1];

            // Act
            var _deleteEntitiesByPartitionKeyRowKeyStartPatternResponses = AzTableUnitTestHelper
                .DeleteEntitiesByPartitionKeyRowKeyStartPattern(selectedEntity.PartitionKey,
                selectedEntity.RowKey.Substring(0, 5), AzTableUnitTestHelper.GetTableEntityName());

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyRowKeyStartPatternResponses);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(new List<TableEntity> { selectedEntity },
                ConstProvider.The_specified_resource_does_not_exist);

            entities.RemoveAt(entities.Count - 1);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesParallelForEachTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities();

            // Act
            var _deleteEntitiesParallelForEachArc = AzTableUnitTestHelper.DeleteEntitiesParallelForEach(entities);

            // Assert
            Assert.Equal(_deleteEntitiesParallelForEachArc, entities.Count());

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.DeleteEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyResponsesAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        #region Async

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest1()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity, AzTableUnitTestHelper.DeleteEntityAsync);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest2()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, AzTableUnitTestHelper.DeleteEntityAsync<TableEntity>);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest3()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, AzTableUnitTestHelper.DeleteEntityAsync);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyAsyncResponsesAct = Helper.RunAsSync(entities, AzTableUnitTestHelper.DeleteEntitiesTransactionallyAsync);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyAsyncResponsesAct);

            AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        #endregion
    }
}
