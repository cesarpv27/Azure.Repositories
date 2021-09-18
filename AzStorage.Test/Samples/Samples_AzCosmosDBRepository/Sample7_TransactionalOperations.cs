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
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

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
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);

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
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
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
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesAsStringTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesAsStringTransactionallyAsync(partialEntities).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesAsStringTransactionallyAsyncResponseAct);
        }

        [Fact, TestPriority(330)]
        public void GetEntitiesResponsesTransactionallyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesResponsesTransactionallyResponseAct = AzCosmosUnitTestHelper
                .GetEntitiesResponsesTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesResponsesTransactionallyResponseAct);
        }
        
        [Fact, TestPriority(332)]
        public void GetEntitiesResponsesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAssertSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true, true);
            
            var partialEntities = entities.Select(entt => new CustomCosmosEntity(entt.PartitionKey, entt.Id)).ToList();

            // Act
            var _getEntitiesResponsesTransactionallyResponseAct = AzCosmosUnitTestHelper.GetEntitiesResponsesTransactionally(partialEntities);

            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesResponsesTransactionallyResponseAct);
        }

        #endregion

        #region Update entities transactionally

        [Fact, TestPriority(330)]
        public void UpdateEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);
            entities.ForEach(entt =>
            {
                entt.Prop1 = AzCosmosUnitTestHelper.GenerateProp(1, "Cosmos_");
                entt.Prop2 = null;
            });

            var _addEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper.AddEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();
            AzCosmosUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);

            entities.ForEach(entt => 
            { 
                entt.Prop1 = null;
                entt.Prop2 = AzCosmosUnitTestHelper.GenerateProp(2, "Cosmos_"); 
            });

            // Act
            var _getEntitiesTransactionallyAsyncResponseAct = AzCosmosUnitTestHelper
                .UpdateEntitiesTransactionallyAsync(entities).WaitAndUnwrapException();
            
            // Assert
            AzCosmosUnitTestHelper.AssertSucceededResponses(_getEntitiesTransactionallyAsyncResponseAct);
        }

        #endregion
    }
}
