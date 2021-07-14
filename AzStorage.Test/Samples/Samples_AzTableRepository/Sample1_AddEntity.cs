using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Helpers;
using System.Linq;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_AddEntity
    {
        [Fact, TestPriority(100)]
        public void AddEntityTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            AzTableUnitTestHelper.AssertByGetEntity(entity);
        }

        [Fact, TestPriority(100)]
        public void AddEntityTest2()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();
            AzTableUnitTestHelper.AddTestProp(entity, 1);

            // Act
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var resultingEntity = AzTableUnitTestHelper.AssertByGetEntity(entity);
            
            Assert.Equal(resultingEntity.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
        }

        [Fact, TestPriority(100)]
        public void AddExistingEntityTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = AzTableUnitTestHelper.AddEntity(entity);
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity);

            // Assert
            AzTableUnitTestHelper.AssertExpectedFailedResponse(_addEntityResponseAct, ConstProvider.The_specified_entity_already_exists);
        }

        [Fact, TestPriority(300)]
        public void AddEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateSomeEntities(AzTableUnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyResponsesAct);

            AzTableUnitTestHelper.AssertByGetEntity(entities);
        }

        [Fact, TestPriority(300)]
        public void AddExistingEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _addEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertExpectedFailedResponses(_addEntitiesTransactionallyResponsesAct, ConstProvider.The_specified_entity_already_exists);
        }

        #region Async, parallel tests

        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = Helper.RunAsSync(entity, AzTableUnitTestHelper.AddEntityAsync);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityAsyncResponseAct);

            AzTableUnitTestHelper.AssertByGetEntity(entity);
        }

        [Fact, TestPriority(100)]
        public void AddEntitiesParallelForEachTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateSomeEntities(AzTableUnitTestHelper.GetUnderOneHundredRandomValue(), true);

            // Act
            var _addEntitiesParallelForEachResponseAct = AzTableUnitTestHelper.AddEntitiesParallelForEach(entities);

            // Assert
            Assert.Equal(_addEntitiesParallelForEachResponseAct, entities.Count());

            AzTableUnitTestHelper.AssertByGetEntity(entities);
        }

        [Fact, TestPriority(300)]
        public void AddEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateSomeEntities(AzTableUnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyAsyncResponseAct = Helper.RunAsSync(entities, AzTableUnitTestHelper.AddEntitiesTransactionallyAsync);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);

            AzTableUnitTestHelper.AssertByGetEntity(entities);
        }

        #endregion
    }
}
