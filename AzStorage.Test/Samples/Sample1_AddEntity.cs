using System;
using Xunit;
using AzStorage.Test.Utilities;
using AzStorage.Test.Helpers;
using Azure.Data.Tables;
using System.Threading.Tasks;
using CoreTools.Extensions;
using CoreTools.Helpers;
using System.Linq;

namespace AzStorage.Test.Samples
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_AddEntity
    {
        [Fact, TestPriority(100)]
        public void AddEntityTest()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityResponseAct = UnitTestHelper.AddEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            UnitTestHelper.AssertByGetEntity(entity);
        }

        [Fact, TestPriority(100)]
        public void AddEntityTest2()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();
            UnitTestHelper.AddTestProp(entity, 1);

            // Act
            var _addEntityResponseAct = UnitTestHelper.AddEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var resultingEntity = UnitTestHelper.AssertByGetEntity(entity);
            
            Assert.Equal(resultingEntity.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
        }

        [Fact, TestPriority(100)]
        public void AddExistingEntityTest()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = UnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _addEntityResponseAct = UnitTestHelper.AddEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponse(_addEntityResponseAct, ConstProvider.The_specified_entity_already_exists);
        }

        [Fact, TestPriority(300)]
        public void AddEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyResponsesAct = UnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyResponsesAct);

            UnitTestHelper.AssertByGetEntity(entities);
        }

        [Fact, TestPriority(300)]
        public void AddExistingEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _addEntitiesTransactionallyResponsesAct = UnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            UnitTestHelper.AssertExpectedFailedResponses(_addEntitiesTransactionallyResponsesAct, ConstProvider.The_specified_entity_already_exists);
        }

        #region Async, parallel tests

        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = Helper.RunAsSync(entity, UnitTestHelper.AddEntityAsync);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityAsyncResponseAct);

            UnitTestHelper.AssertByGetEntity(entity);
        }

        [Fact, TestPriority(100)]
        public void AddEntitiesParallelForEachTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue(), true);

            // Act
            var _addEntitiesParallelForEachResponseAct = UnitTestHelper.AddEntitiesParallelForEach(entities);

            // Assert
            Assert.Equal(_addEntitiesParallelForEachResponseAct, entities.Count());

            UnitTestHelper.AssertByGetEntity(entities);
        }

        [Fact, TestPriority(300)]
        public void AddEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyAsyncResponseAct = Helper.RunAsSync(entities, UnitTestHelper.AddEntitiesTransactionallyAsync);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);

            UnitTestHelper.AssertByGetEntity(entities);
        }

        #endregion
    }
}
