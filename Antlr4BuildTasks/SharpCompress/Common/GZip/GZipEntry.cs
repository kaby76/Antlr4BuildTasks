using System;
using System.Collections.Generic;
using System.IO;

namespace SharpCompress.Common.GZip
{
    public class GZipEntry : Entry
    {
        private readonly GZipFilePart _filePart;

        internal GZipEntry(GZipFilePart filePart)
        {
            _filePart = filePart;
        }

        public override CompressionType CompressionType => CompressionType.GZip;

        public override long Crc => _filePart.Crc ?? 0;

        public override string Key => _filePart.FilePartName;

        public override string LinkTarget => null;

        public override long CompressedSize => 0;

        public override long Size => _filePart.UncompressedSize ?? 0;

        public override Nullable<DateTime> LastModifiedTime => _filePart.DateModified;

        public override Nullable<DateTime> CreatedTime => null;

        public override Nullable<DateTime> LastAccessedTime => null;

        public override Nullable<DateTime> ArchivedTime => null;

        public override bool IsEncrypted => false;

        public override bool IsDirectory => false;

        public override bool IsSplitAfter => false;

        public override Nullable<long> Attrib => throw new NotImplementedException();

        internal override IEnumerable<FilePart> Parts => _filePart.AsEnumerable<FilePart>();

        internal static IEnumerable<GZipEntry> GetEntries(Stream stream, OptionsBase options)
        {
            yield return new GZipEntry(new GZipFilePart(stream, options.ArchiveEncoding));
        }
    }
}