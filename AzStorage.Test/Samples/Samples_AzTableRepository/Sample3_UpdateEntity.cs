using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_UpdateEntity
    {
        [Fact, TestPriority(100)]
        public void UpdateMergeEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateMergeEntity,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpdateReplaceEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateReplaceEntity,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpdateMergeEntitiesTransactionallyTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateMergeEntitiesTransactionally,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpdateReplaceEntitiesTransactionallyTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateReplaceEntitiesTransactionally,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #region Async

        [Fact, TestPriority(100)]
        public void UpdateMergeEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateMergeEntityAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpdateReplaceEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateReplaceEntityAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpdateMergeEntitiesTransactionallyAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateMergeEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(300)]
        public void UpdateReplaceEntitiesTransactionallyAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateReplaceEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion
    }
}
