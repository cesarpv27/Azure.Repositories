using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Blobs.Models;

namespace AzStorage.Core.Blobs
{
    public class DeleteBlobContainerMetadata : BlobContainerMetadata
    {
        /// <summary>
        /// Conditions to be added on deletion of the container.
        /// </summary>
        public virtual BlobRequestConditions Conditions { get; set; }
    }
}
