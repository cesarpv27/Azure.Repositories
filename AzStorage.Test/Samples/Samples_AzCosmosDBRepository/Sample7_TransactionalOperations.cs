using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using CoreTools.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample7_TransactionalOperations
    {
        #region Add entities transactionally (sync & async)

        [Fact, TestPriority(300)]
        public void AddEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), false);

            // Act
            var _addEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyResponseAct);
        }

        [Fact, TestPriority(302)]
        public void AddEntitiesTransactionallyTest2()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(304)]
        public void AddEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper.AddEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);
        }

        #endregion

        #region Get entities transactionally (sync & async)

        [Fact, TestPriority(326)]
        public void GetEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(326)]
        public void GetEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesTransactionallyAsync(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(
                _getEntitiesTransactionallyAsyncResponseAct.WaitAndUnwrapException());
        }
        
        [Fact, TestPriority(326)]
        public void GetEntitiesAsStringTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesAsStringTransactionallyResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesAsStringTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesAsStringTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(328)]
        public void GetEntitiesAsStringTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesAsStringTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesAsStringTransactionallyAsync(partialEntities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesAsStringTransactionallyAsyncResponseAct);
        }

        [Fact, TestPriority(328)]
        public void GetEntitiesResponsesTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesResponsesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesResponsesTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesResponsesTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(328)]
        public void GetEntitiesResponsesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesResponsesTransactionallyResponseAct = AzCosmosUnitTestHelper.GetEntitiesResponsesTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesResponsesTransactionallyResponseAct);
        }

        #endregion

        #region Update entities transactionally (sync & async)

        [Fact, TestPriority(330)]
        public void UpdateEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSetPropsAddAssertSetPropsSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);
            
            // Act
            var _updateEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .UpdateEntitiesTransactionally(entities);
            
            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_updateEntitiesTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(330)]
        public void UpdateEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSetPropsAddAssertSetPropsSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);
            
            // Act
            var _updateEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .UpdateEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();
            
            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_updateEntitiesTransactionallyAsyncResponseAct);
        }

        #endregion

        #region Upsert entities transactionally (sync & async)

        [Fact, TestPriority(340)]
        public void UpsertEntitiesTransactionallyTest1()// adds
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), false);

            // Act
            var _upsertEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .UpsertEntitiesTransactionally(entities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_upsertEntitiesTransactionallyResponseAct);
        }

        [Fact, TestPriority(340)]
        public void UpsertEntitiesTransactionallyAsyncTest1()// adds
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), false);

            // Act
            var _upsertEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .UpsertEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_upsertEntitiesTransactionallyAsyncResponseAct);
        }
        
        [Fact, TestPriority(340)]
        public void UpsertEntitiesTransactionallyTest2()// updates
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSetPropsAddAssertSetPropsSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _upsertEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .UpsertEntitiesTransactionally(entities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_upsertEntitiesTransactionallyResponseAct);
        }

        [Fact, TestPriority(340)]
        public void UpsertEntitiesTransactionallyAsyncTest2()// updates
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSetPropsAddAssertSetPropsSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _upsertEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .UpsertEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_upsertEntitiesTransactionallyAsyncResponseAct);
        }

        #endregion

        #region Delete entities transactionally (sync & async)

        [Fact, TestPriority(350)]
        public void DeleteEntitiesTransactionallyTest1()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

            // Act
            var _deleteEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .DeleteEntitiesTransactionally(entities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyResponseAct);
        }

        [Fact, TestPriority(350)]
        public void DeleteEntitiesTransactionallyAsyncTest1()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

            // Act
            var _deleteEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .DeleteEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyAsyncResponseAct);
        }
        
        [Fact, TestPriority(350)]
        public void DeleteEntitiesTransactionallyTest2()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            var entitiesToDelete = entities.Select(ent => new CustomCosmosEntity(ent.PartitionKey, ent.Id));

            // Act
            var _deleteEntitiesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .DeleteEntitiesTransactionally(entitiesToDelete);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyResponseAct);
        }

        [Fact, TestPriority(350)]
        public void DeleteEntitiesTransactionallyAsyncTest2()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            var entitiesToDelete = entities.Select(ent => new CustomCosmosEntity(ent.PartitionKey, ent.Id));

            // Act
            var _deleteEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .DeleteEntitiesTransactionallyAsync(entitiesToDelete).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyAsyncResponseAct);
        }

        #endregion
    }
}
