using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using CoreTools.Helpers;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample0_Miscellaneous
    {
        [Fact, TestPriority(100)]
        public void InitializeTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            var rdm = new Random();

            // Act
            _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity,
                $"Database{rdm.Next(1, 1000000)}", $"Container{rdm.Next(1, 1000000)}").WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }
    }
}
