using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Blobs
{
    public class BlobContainerMetadata
    {
        /// <summary>
        /// The name of the container.
        /// </summary>
        public virtual string BlobContainerName { get; set; }

        /// <summary>
        /// Specifies whether data in the container may be accessed publicly and
        /// the level of access. Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer
        /// specifies full public read access for container and blob data. Clients can enumerate
        /// blobs within the container via anonymous request, but cannot enumerate containers
        /// within the storage account. Azure.Storage.Blobs.Models.PublicAccessType.Blob
        /// specifies public read access for blobs. Blob data within this container can be
        /// read via anonymous request, but container data is not available. Clients cannot
        /// enumerate blobs within the container via anonymous request. Azure.Storage.Blobs.Models.PublicAccessType.None
        /// specifies that the container data is private to the account owner.
        /// </summary>
        public virtual PublicAccessType PublicAccessType { get; set; } = PublicAccessType.None;

        /// <summary>
        /// Custom metadata of the container.
        /// </summary>
        public virtual IDictionary<string, string> Metadata { get; set; }
    }
}
