using System;

namespace SharpCompress.Common
{
    public interface IEntry
    {
        CompressionType CompressionType { get; }
        Nullable<DateTime> ArchivedTime { get; }
        long CompressedSize { get; }
        long Crc { get; }
        Nullable<DateTime> CreatedTime { get; }
        string Key { get; }
        string LinkTarget { get; }
        bool IsDirectory { get; }
        bool IsEncrypted { get; }
        bool IsSplitAfter { get; }
        bool IsSolid { get; }
        Nullable<DateTime> LastAccessedTime { get; }
        Nullable<DateTime> LastModifiedTime { get; }
        long Size { get; }
        int? Attrib { get; }
    }
}