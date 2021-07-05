using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AzStorage.Test.Utilities
{
    [CollectionDefinition(nameof(DisableParallelizationCollection), DisableParallelization = true)]
    public class DisableParallelizationCollection { }
}
