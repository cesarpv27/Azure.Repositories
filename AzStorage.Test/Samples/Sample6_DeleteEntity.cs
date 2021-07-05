using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using AzStorage.Test.Helpers;
using System.Linq;
using CoreTools.Helpers;

namespace AzStorage.Test.Samples
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample6_DeleteEntity
    {
        [Fact, TestPriority(100)]
        public void DeleteEntityTest1()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = UnitTestHelper.DeleteEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityTest2()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = UnitTestHelper.DeleteEntity<TableEntity>(entity.PartitionKey, entity.RowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityTest3()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = UnitTestHelper.DeleteEntity(entity.PartitionKey, entity.RowKey, UnitTestHelper.GetTableEntityName());

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyResponses = UnitTestHelper.DeleteEntitiesByPartitionKey<TableEntity>(commonPartitionKey);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyResponses = UnitTestHelper.DeleteEntitiesByPartitionKey(commonPartitionKey, UnitTestHelper.GetTableEntityName());

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyStartPatternTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyStartPatternResponses = UnitTestHelper
                .DeleteEntitiesByPartitionKeyStartPattern<TableEntity>(commonPartitionKey.Substring(0, 5));

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyStartPatternResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyStartPatternTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _deleteEntitiesByPartitionKeyStartPatternResponses = UnitTestHelper
                .DeleteEntitiesByPartitionKeyStartPattern<TableEntity>(commonPartitionKey.Substring(0, 5), UnitTestHelper.GetTableEntityName());

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyStartPatternResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, others,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyRowKeyStartPatternTest1()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey).ToList();
            var selectedEntity = entities[entities.Count - 1];

            // Act
            var _deleteEntitiesByPartitionKeyRowKeyStartPatternResponses = UnitTestHelper
                .DeleteEntitiesByPartitionKeyRowKeyStartPattern<TableEntity>(selectedEntity.PartitionKey,
                selectedEntity.RowKey.Substring(0, 5));

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyRowKeyStartPatternResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(new List<TableEntity> { selectedEntity },
                ConstProvider.The_specified_resource_does_not_exist);

            entities.RemoveAt(entities.Count - 1);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesByPartitionKeyRowKeyStartPatternTest2()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey).ToList();
            var selectedEntity = entities[entities.Count - 1];

            // Act
            var _deleteEntitiesByPartitionKeyRowKeyStartPatternResponses = UnitTestHelper
                .DeleteEntitiesByPartitionKeyRowKeyStartPattern(selectedEntity.PartitionKey,
                selectedEntity.RowKey.Substring(0, 5), UnitTestHelper.GetTableEntityName());

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesByPartitionKeyRowKeyStartPatternResponses);

            UnitTestHelper.AssertByExpectedFailedGetEntity(new List<TableEntity> { selectedEntity },
                ConstProvider.The_specified_resource_does_not_exist);

            entities.RemoveAt(entities.Count - 1);

            var _queryAllResponse = UnitTestHelper.QueryAll<TableEntity>();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesParallelForEachTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateAddAssertSomeEntities();

            // Act
            var _deleteEntitiesParallelForEachArc = UnitTestHelper.DeleteEntitiesParallelForEach(entities);

            // Assert
            Assert.Equal(_deleteEntitiesParallelForEachArc, entities.Count());

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyResponsesAct = UnitTestHelper.DeleteEntitiesTransactionally(entities);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyResponsesAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        #region Async

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest1()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity, UnitTestHelper.DeleteEntityAsync);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest2()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, UnitTestHelper.DeleteEntityAsync<TableEntity>);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest3()
        {
            // Arrange
            var entity = UnitTestHelper.CreateAddAssertSomeEntity();

            // Act
            var _deleteEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, UnitTestHelper.DeleteEntityAsync);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entity, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(300)]
        public void DeleteEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyAsyncResponsesAct = Helper.RunAsSync(entities, UnitTestHelper.DeleteEntitiesTransactionallyAsync);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyAsyncResponsesAct);

            UnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        #endregion
    }
}
