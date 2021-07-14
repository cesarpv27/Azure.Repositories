using System;
using System.Collections.Generic;
using System.Text;
using AzCoreTools.Core.Interfaces;
using Xunit;

namespace AzStorage.Test.Helpers
{
    internal class UnitTestHelper
    {
        #region Assert response

        public static void AssertExpectedSuccessfulResponse(IAzDetailedResponse failedResponse)
        {
            Assert.True(failedResponse.Succeeded);

            Assert.Null(failedResponse.Exception);
            Assert.Null(failedResponse.Message);
        }

        #endregion

    }
}
