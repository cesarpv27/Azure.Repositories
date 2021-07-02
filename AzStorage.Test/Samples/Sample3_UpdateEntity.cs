using Xunit;
using AzStorage.Test.Helpers;

namespace AzStorage.Test.Samples
{
    public class Sample3_UpdateEntity
    {
        [Fact]
        public void UpdateMergeEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateMergeEntity,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpdateReplaceEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateReplaceEntity,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpdateMergeEntitiesTransactionallyTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateMergeEntitiesTransactionally,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
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

        [Fact]
        public void UpdateMergeEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateMergeEntityAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpdateReplaceEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpdateReplaceEntityAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpdateMergeEntitiesTransactionallyAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpdateMergeEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
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
