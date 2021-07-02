using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Core.Tables;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using AzStorage.Repositories;
using Azure.Data.Tables;
using AzStorage.Core.Extensions;

namespace AzStorage.Test.Samples
{
    public class Sample10_AzTableTransactionStore
    {
        [Fact]
        public void AzTableTransactionStoreTest()
        {
            // Arrange
            var entitiesToAdd = UnitTestHelper.CreateSomeEntities(ConstProvider.Hundreds_RandomMaxValue + 10, false);
            
            var entitiesToUpdate = UnitTestHelper.CreateAddAssertSomeEntities(false,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            var entitiesToAddNDelete = UnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Add(entitiesToAdd);
            _azTableTransactionStore.Update(entitiesToUpdate, TableUpdateMode.Replace);
            _azTableTransactionStore.Delete(entitiesToAddNDelete);

            var _tableClient = UnitTestHelper.GetExistingTableClient(UnitTestHelper.GetTableEntityName());

            // Act
            var responses = _tableClient.SubmitTransaction<AzTableTransactionStore>(_azTableTransactionStore, default);

            // Assert
            UnitTestHelper.AssertSucceededResponses(responses);
        }
    }
}
