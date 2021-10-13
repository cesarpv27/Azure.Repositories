using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_Create_Upload
    {
        [Fact, TestPriority(100)]
        public void CreateBlobContainerTest()
        {
            //// Arrange
            //var entity = AzBlobUnitTestHelper.CreateSomeEntity();

            //// Act
            //var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntity(entity);

            //// Assert
            //UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

    }
}
