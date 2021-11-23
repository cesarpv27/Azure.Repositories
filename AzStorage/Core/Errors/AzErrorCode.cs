using AzStorage.Core.Errors.Blob;
using AzStorage.Core.Errors.File;
using AzStorage.Core.Errors.Queue;
using AzStorage.Core.Errors.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors
{
    public class AzErrorCode
    {
        private static BlobErrorCode _BlobErrorCode;
        public static BlobErrorCode BlobErrorCode
        {
            get
            {
                if (_BlobErrorCode == null)
                    _BlobErrorCode = new BlobErrorCode();

                return _BlobErrorCode;
            }
        }

        private static QueueErrorCode _QueueErrorCode;
        public static QueueErrorCode QueueErrorCode
        {
            get
            {
                if (_QueueErrorCode == null)
                    _QueueErrorCode = new QueueErrorCode();

                return _QueueErrorCode;
            }
        }

        private static TableErrorCode _TableErrorCode;
        public static TableErrorCode TableErrorCode
        {
            get
            {
                if (_TableErrorCode == null)
                    _TableErrorCode = new TableErrorCode();

                return _TableErrorCode;
            }
        }

        private static FileErrorCode _FileErrorCode;
        public static FileErrorCode FileErrorCode
        {
            get
            {
                if (_FileErrorCode == null)
                    _FileErrorCode = new FileErrorCode();

                return _FileErrorCode;
            }
        }
    }
}
