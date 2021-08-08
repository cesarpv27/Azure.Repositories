using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample4_UpsertEntity
    {
        [Fact, TestPriority(100)]
        public void UpsertEntity_UpdateTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpsertEntity,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertEntity_UpdateTest2()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpsertEntity2,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertEntity_AddTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(false,
                AzCosmosUnitTestHelper.UpsertEntity,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        #region Async

        [Fact, TestPriority(100)]
        public void UpsertEntityAsync_UpdateTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpsertEntityAsync,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertEntityAsync_UpdateTest2()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpsertEntityAsync2,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        [Fact, TestPriority(100)]
        public void UpsertEntityAsync_AddTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(false,
                AzCosmosUnitTestHelper.UpsertEntityAsync,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

        #endregion

    }
}
