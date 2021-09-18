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
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpdateMergeEntity,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpdateReplaceEntityTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpdateReplaceEntity,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #region Async

        [Fact, TestPriority(100)]
        public void UpdateMergeEntityAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpdateMergeEntityAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpdateReplaceEntityAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzTableUnitTestHelper.UpdateReplaceEntityAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion
    }
}
