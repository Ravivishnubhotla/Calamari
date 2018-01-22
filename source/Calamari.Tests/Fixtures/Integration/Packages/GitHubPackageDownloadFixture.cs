using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Calamari.Integration.Packages;
using Calamari.Integration.Packages.Download;
using NUnit.Framework;
using Octopus.Versioning.Semver;

namespace Calamari.Tests.Fixtures.Integration.Packages
{
    [TestFixture]
    public class GitHubPackageDownloadFixture
    {
        private static string home = Path.GetTempPath();

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            Environment.SetEnvironmentVariable("TentacleHome", home);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            Environment.SetEnvironmentVariable("TentacleHome", null);
        }
        

        [SetUp]
        public void SetUp()
        {
            var downloadPath = new PackageDownloaderUtils().RootDirectory;
            if (Directory.Exists(downloadPath))
            {
                Directory.Delete(downloadPath, true);
            }

            Directory.CreateDirectory(downloadPath);
        }

        [Test]
        public void DownloadsPackageFromGitHub()
        {
            var downloader = new GitHubPackageDownloader();

            downloader.DownloadPackage("OctopusDeploy/Octostache", new SemanticVersion("2.1.8"), "feed-github", 
                new Uri("https://api.github.com/"), 
                FeedType.GitHub, null, true, 3, 
                TimeSpan.FromSeconds(3), 
                out string path, 
                out string hash, 
                out long size);

            StringAssert.StartsWith("OctopusDeploy+Octostache.2.1.8", Path.GetFileName(path));
            Assert.Greater(size, 0);
            Assert.IsFalse(String.IsNullOrWhiteSpace(hash));
        }

        [Test]
        public void WillReUseFileIfItExists()
        {
            var downloader = new GitHubPackageDownloader();

            downloader.DownloadPackage("OctopusDeploy/Octostache", new SemanticVersion("2.1.7"), "feed-github",
                new Uri("https://api.github.com/"),
                FeedType.GitHub, null, true, 3,
                TimeSpan.FromSeconds(3),
                out string path1,
                out string hash1,
                out long size1);

            Assert.Greater(size1, 0);

            downloader.DownloadPackage("OctopusDeploy/Octostache", new SemanticVersion("2.1.7"), "feed-github",
                new Uri("https://WillFailIfInvoked"),
                FeedType.GitHub, null, false, 3,
                TimeSpan.FromSeconds(3),
                out string path2,
                out string hash2,
                out long size2);

            Assert.AreEqual(path1, path2);
            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(size1, size2);
        }

    }
}
