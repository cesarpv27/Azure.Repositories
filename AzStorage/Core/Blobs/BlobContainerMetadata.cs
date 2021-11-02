using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Blobs
{
    public abstract class BlobContainerMetadata
    {
        /// <summary>
        /// The name of the container.
        /// </summary>
        public virtual string BlobContainerName { get; set; }
    }
}
