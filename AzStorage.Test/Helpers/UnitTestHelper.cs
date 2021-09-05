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

        public static void AssertExpectedSuccessfulGenResponse<T>(IAzDetailedResponse<T> response)
        {
            AssertExpectedSuccessfulResponse(response);

            Assert.NotNull(response.Value);
        }

        public static void AssertExpectedFailedResponse(IAzDetailedResponse failedResponse, string errorMessage)
        {
            Assert.False(failedResponse.Succeeded);

            Assert.NotNull(failedResponse.Exception);
            Assert.NotNull(failedResponse.Message);
            Assert.Contains(errorMessage, failedResponse.Message);
        }

        public static void AssertExpectedFailedGenResponse<T>(
            IAzDetailedResponse<T> failedResponse,
            string errorMessage,
            bool assertValue = true)
        {
            AssertExpectedFailedResponse(failedResponse, errorMessage);

            if (assertValue)
                Assert.Null(failedResponse.Value);
        }

        #endregion


        public static int GetUnderOneHundredRandomValue()
        {
            return new Random().Next(Utilities.ConstProvider.RandomMinValue,
                Utilities.ConstProvider.RandomMaxValue);
        }

        public static int GetOverOneHundredRandomValue()
        {
            return new Random().Next(Utilities.ConstProvider.Hundreds_RandomMinValue,
                Utilities.ConstProvider.Hundreds_RandomMaxValue);
        }

    }
}
