using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_UpdateEntity
    {
        [Fact, TestPriority(100)]
        public void UpdateEntityAsyncTest()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzCosmosUnitTestHelper.UpdateEntityAsync,
                cmsEntt => {
                    Assert.Equal(cmsEntt.Prop1, AzCosmosUnitTestHelper.GenerateUpdatedProp(1));
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }
        
        [Fact, TestPriority(100)]
        public void UpdateEntityAsyncTest2()
        {
            AzCosmosUnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                AzCosmosUnitTestHelper.UpdateEntityAsync2,
                cmsEntt => {
                    Assert.Equal(cmsEntt.Prop1, AzCosmosUnitTestHelper.GenerateUpdatedProp(1));
                    Assert.Equal(cmsEntt.Prop2, AzCosmosUnitTestHelper.GenerateUpdatedProp(2));
                });
        }

    }
}
