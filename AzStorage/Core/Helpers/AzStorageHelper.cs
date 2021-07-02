using Azure;
using Azure.Data.Tables;

namespace AzStorage.Core.Helpers
{
    public class AzStorageHelper
    {
        public static ETag GetValidETag<T>(T entity = null) where T : class, ITableEntity, new()
        {
            if (entity != null)
                return GetValidETag(entity.ETag);

            return GetValidETag();
        }

        public static ETag GetValidETag(ETag eTag = default)
        {
            if (eTag != default)
                return eTag;

            return ETag.All;
        }
    }
}
