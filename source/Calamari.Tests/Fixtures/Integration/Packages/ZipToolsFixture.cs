using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Calamari.Integration.Packages;
using Calamari.Tests.Helpers;
using NUnit.Framework;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;

namespace Calamari.Tests.Fixtures.Integration.Packages
{
    [TestFixture]
    public class ZipToolsFixture : CalamariFixture
    {
        [Test]
        public void DeNestContents()
        {
            var zipOriginal = GetFixtureResouce("Samples", "GitHubArchive.zip");
            var zipCopy = Path.GetTempFileName();
            if (File.Exists(zipCopy))
            {
                File.Delete(zipCopy);
            }

            var before = ListFileContents(zipOriginal);
            ZipTools.DeNestContents(zipOriginal, zipCopy);
            var after = ListFileContents(zipCopy);

            //Assert.AreEqual(before.Length, after.Length); //SharpCompress does not preserve directory entries

            var expected = before
                .Where(file => !file.EndsWith("/")) //SharpCompress does not preserve directory entries
                .Select(file => file.Substring(file.IndexOf('/') + 1)).ToArray();
            CollectionAssert.AreEquivalent(expected, after);
        }

        private static string[] ListFileContents(string fileName)
        {
            using (var fileStream = File.OpenRead(fileName))
            using (var reader = ZipArchive.Open(fileStream, new ReaderOptions()))
            {
                return reader.Entries.Select(entry => entry.Key).ToArray();
            }
        }
        
    }
}
