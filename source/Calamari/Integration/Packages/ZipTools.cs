using System;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;

namespace Calamari.Integration.Packages
{
    public static class ZipTools
    {
        /// <summary>
        /// Takes files from the root inner directory, and moves down to root.
        /// Currently only relevent for Git archives.
        ///  e.g. /Dir/MyFile => /MyFile
        /// This was created indpependantly from the version in Octopus.Server since we need to support .net 4.0 here which does not have `System.IO.Compression` library.
        /// The reason this library is preferred over `SharpCompress` is that it can update zips in-place and it preserves empty directories.
        /// https://github.com/adamhathcock/sharpcompress/issues/62
        /// https://github.com/adamhathcock/sharpcompress/issues/34
        /// https://github.com/adamhathcock/sharpcompress/issues/242
        /// </summary>
        /// <param name="fileName"></param>
        public static void DeNestContents(string src, string dest)
        {
            int rootPathSeperator = -1;
            using (var readerStram = File.Open(src, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = ReaderFactory.Open(readerStram))
            {
                using (var writerStream = File.Open(dest, FileMode.CreateNew, FileAccess.ReadWrite))
                using (var writer = WriterFactory.Open(writerStream, ArchiveType.Zip, new ZipWriterOptions(CompressionType.Deflate)))
                {
                    while (reader.MoveToNextEntry())
                    {
                        var entry = reader.Entry;
                        if (!reader.Entry.IsDirectory)
                        {
                            if (rootPathSeperator == -1)
                            {
                                rootPathSeperator = entry.Key.IndexOf('/') + 1;
                            }

                            var newFilePath = entry.Key.Substring(rootPathSeperator);
                            if (newFilePath != String.Empty)
                            {
                                writer.Write(newFilePath, reader.OpenEntryStream());
                            }
                        }
                    }
                }
            }
        }
    }
}