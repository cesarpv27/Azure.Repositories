using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_UpdateEntity
    {
        [Fact, TestPriority(100)]
        public void UpdateEntityAsyncTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpdateEntityAsync,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }
        
        [Fact, TestPriority(100)]
        public void UpdateEntityAsyncTest2()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(true,
                AzCosmosUnitTestHelper.UpdateEntityAsync2,
                cmsEntt => {
                    Assert.Null(cmsEntt.Prop1);
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }
    }
}
