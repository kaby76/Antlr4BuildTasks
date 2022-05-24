using SharpCompress.Common;
using SharpCompress.Readers;

public class Program
{
    public static void Main(string[] args)
    {
        Read(@"c:\temp\", @"c:\temp\jdk-18_linux-x64_bin.tar.gz", new CompressionType(), null);
    }

    private static void Read(string destination, string testArchive, CompressionType expectedCompression, ReaderOptions options = null)
    {
        Stream stream = File.OpenRead(testArchive);
        var reader = ReaderFactory.Open(stream);
        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                Console.WriteLine(reader.Entry.Key);
                reader.WriteEntryToDirectory(destination, new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true,
//                    PreserveAttributes = true,
//                    PreserveFileTime = true,
                });
                var artime = reader.Entry.Attrib;
            }
        }
        reader.Dispose();
        stream.Dispose();
    }

}