using AzStorage.Core.Extensions;
using AzStorage.Core.Tables;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample10_AzTableTransactionStore
    {
        [Fact, TestPriority(700)]
        public void AzTableTransactionStoreTest()
        {
            // Arrange
            var entitiesToAdd = AzTableUnitTestHelper.CreateSomeEntities(ConstProvider.Hundreds_RandomMaxValue + 10, false);
            
            var entitiesToUpdate = AzTableUnitTestHelper.CreateAddAssertSomeEntities(false,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            var entitiesToAddNDelete = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Add(entitiesToAdd);
            _azTableTransactionStore.Update(entitiesToUpdate, TableUpdateMode.Replace);
            _azTableTransactionStore.Delete(entitiesToAddNDelete);

            var _tableClient = AzTableUnitTestHelper.GetExistingTableClient(AzTableUnitTestHelper.GetTableEntityName());

            // Act
            var responses = _tableClient.SubmitTransaction<AzTableTransactionStore>(_azTableTransactionStore, default);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(responses);
        }
    }
}
