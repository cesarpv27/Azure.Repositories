using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Helpers;
using CoreTools.Extensions;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_AddEntity
    {
        //[Fact, TestPriority(100)]
        //public void AddEntityTest()
        //{
        //    // Arrange
        //    var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

        //    // Act
        //    var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity);

        //    // Assert
        //    AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

        //    AzTableUnitTestHelper.AssertByGetEntity(entity);
        //}

        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityAsyncResponseAct);

            //AzCosmosUnitTestHelper.AssertByGetEntity(entity);
        }

    }
}
