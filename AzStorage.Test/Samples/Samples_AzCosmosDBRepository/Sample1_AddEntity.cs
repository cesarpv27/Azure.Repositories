using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using System;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_AddEntity
    {
        [Fact, TestPriority(100)]
        public void AddEntityTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

        [Fact, TestPriority(100)]
        public void AddEntityTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntity2(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

        [Fact, TestPriority(100)]
        public void AddEntityTest3()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            entity.PartitionKey = default;

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => AzCosmosUnitTestHelper.AddEntity(entity));
        }

        [Fact, TestPriority(100)]
        public void AddEntityTest4()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            entity.Id = default;

            // Act

            // Assert
            Assert.Throws<ArgumentNullException>(() => AzCosmosUnitTestHelper.AddEntity(entity));
        }

        #region Async

        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync2(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

        #endregion
    }
}
