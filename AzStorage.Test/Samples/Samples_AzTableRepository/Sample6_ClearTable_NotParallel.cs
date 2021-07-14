using System;
using System.Collections.Generic;
using Xunit;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using AzStorage.Test.Helpers;
using System.Linq;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    [Collection(nameof(DisableParallelizationCollection))]
    public class Sample6_ClearTable_NotParallel
    {
        [Fact, TestPriority(500)]
        public void ClearTableTest1()
        {
            // Arrange
            AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _clearTableResponseAct = AzTableUnitTestHelper.ClearTable<TableEntity>();

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_clearTableResponseAct);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, new List<TableEntity>(0),
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(505)]
        public void ClearTableTest2()
        {
            // Arrange
            AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue * 2, ConstProvider.Hundreds_RandomMaxValue * 5);

            // Act
            var _clearTableResponseAct = AzTableUnitTestHelper.ClearTable(AzTableUnitTestHelper.GetTableEntityName());

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_clearTableResponseAct);

            var _queryAllResponse = AzTableUnitTestHelper.QueryAll<TableEntity>();
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponse, new List<TableEntity>(0),
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(500)]
        public void ClearTableTest3()
        {
            Assert.Throws<ArgumentNullException>(() => AzTableUnitTestHelper.ClearTable(string.Empty));
        }
    }
}
