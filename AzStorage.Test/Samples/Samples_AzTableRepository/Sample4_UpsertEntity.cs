using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample4_UpsertEntity
    {
        #region Existing entities

        [Fact, TestPriority(100)]
        public void UpsertMergeExistingEntityTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpsertMergeEntity,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertReplaceExistingEntityTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpsertReplaceEntity,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpsertMergeExistingEntitiesTransactionallyTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpsertMergeEntitiesTransactionally,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpsertReplaceExistingEntitiesTransactionallyTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpsertReplaceEntitiesTransactionally,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion

        #region Existing entities async

        [Fact, TestPriority(100)]
        public void UpsertMergeExistingEntityAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpsertMergeEntityAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertReplaceExistingEntityAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpsertReplaceEntityAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpsertMergeExistingEntitiesTransactionallyAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpsertMergeEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpsertReplaceExistingEntitiesTransactionallyAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpsertReplaceEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion

        #region New entities

        [Fact, TestPriority(100)]
        public void UpsertNewEntityTest()
        {
            // Arrange
            var entityA = AzTableUnitTestHelper.CreateSomeEntity();
            var entityB = AzTableUnitTestHelper.CreateSomeEntity();

            // Act
            var _upsertMergeEntityResponseAct = AzTableUnitTestHelper.UpsertMergeEntity(entityA);
            var _upsertReplaceEntityResponseAct = AzTableUnitTestHelper.UpsertReplaceEntity(entityB);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertMergeEntityResponseAct);
            AzTableUnitTestHelper.AssertByGetEntity(entityA);

            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertReplaceEntityResponseAct);
            AzTableUnitTestHelper.AssertByGetEntity(entityB);
        }

        #endregion

        #region New entities async

        [Fact, TestPriority(100)]
        public void UpsertNewEntityAsyncTest()
        {
            // Arrange
            var entityA = AzTableUnitTestHelper.CreateSomeEntity();
            var entityB = AzTableUnitTestHelper.CreateSomeEntity();

            // Act
            var _upsertMergeEntityResponseAct = AzTableUnitTestHelper.UpsertMergeEntityAsync(entityA);
            var _upsertReplaceEntityResponseAct = AzTableUnitTestHelper.UpsertReplaceEntityAsync(entityB);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertMergeEntityResponseAct);
            AzTableUnitTestHelper.AssertByGetEntity(entityA);

            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertReplaceEntityResponseAct);
            AzTableUnitTestHelper.AssertByGetEntity(entityB);
        }

        #endregion
    }
}
