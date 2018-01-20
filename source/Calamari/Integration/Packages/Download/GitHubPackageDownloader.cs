using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using Calamari.Integration.FileSystem;
using Calamari.Util;
using Octopus.Versioning;
using Octopus.Versioning.Metadata;

namespace Calamari.Integration.Packages.Download
{
    class GitHubPackageDownloader : IPackageDownloader
    {
        private static readonly IPackageDownloaderUtils PackageDownloaderUtils = new PackageDownloaderUtils();
        readonly CalamariPhysicalFileSystem fileSystem = CalamariPhysicalFileSystem.GetPhysicalFileSystem();
        public static readonly string DownloadingExtension = ".downloading";
        const string Extension = ".zip";

        public void DownloadPackage(string packageId, IVersion version, string feedId, Uri feedUri, FeedType feedType,
            ICredentials feedCredentials, bool forcePackageDownload, int maxDownloadAttempts, TimeSpan downloadAttemptBackoff,
            out string downloadedTo, out string hash, out long size)
        {
            var cacheDirectory = PackageDownloaderUtils.GetPackageRoot(feedId);

            downloadedTo = null;
            if (!forcePackageDownload)
            {
                downloadedTo = AttemptToGetPackageFromCache(packageId, version, cacheDirectory);
            }

            if (downloadedTo == null)
            {
                downloadedTo = DownloadPackage(packageId, version, feedUri, feedCredentials, cacheDirectory, maxDownloadAttempts, downloadAttemptBackoff);
            }

            using (var file = File.OpenRead(downloadedTo))
            {
                size = file.Length;
                hash = HashCalculator.Hash(file);
            }
            /*
             

            if (downloaded == null)
            {
                DownloadPackage(packageId, version, feedUri, feedCredentials, cacheDirectory, maxDownloadAttempts,
                    downloadAttemptBackoff, out downloaded, out downloadedTo);
            }
            else
            {
                Log.VerboseFormat("Package was found in cache. No need to download. Using file: '{0}'", downloadedTo);
            }

            size = fileSystem.GetFileSize(downloadedTo);
            string packageHash = null;
            downloaded.GetStream(stream => packageHash = HashCalculator.Hash(stream));
            hash = packageHash;
             
             */
        }

        private void SplitPackageId(string packageId, out string owner, out string repo)
        {
            var parts = packageId.Split(OwnerRepoSeperator);
            if (parts.Length > 1)
            {
                owner = parts[0];
                repo = parts[1];
            }
            else
            {
                owner = null;
                repo = packageId;
            }
        }

        private string AttemptToGetPackageFromCache(string packageId, IVersion version, string cacheDirectory)
        {
            Log.VerboseFormat("Checking package cache for package {0} {1}", packageId, version.ToString());

            
            fileSystem.EnsureDirectoryExists(cacheDirectory);

            var name = GetBaseFileName(packageId, version.OriginalString);
            var files = fileSystem.EnumerateFilesRecursively(cacheDirectory, $"{name}{CacheBustSeperator}*");
            var sanitizedPackageId = packageId.Replace(OwnerRepoSeperator, '+');
            foreach (var file in files)
            {
                if (!new NuGetPackageIDParser().TryGetMetadataFromPackageName(Path.GetFileName(file), new[] {Extension}, out var metaData))
                {
                    continue;
                }

                var idMatches = string.Equals(metaData.PackageId, sanitizedPackageId, StringComparison.OrdinalIgnoreCase);
                var versionExactMatch = string.Equals(metaData.Version.ToString(), version.ToString(),
                    StringComparison.OrdinalIgnoreCase);

                if (idMatches && versionExactMatch)
                {
                    Log.VerboseFormat("Package was found in cache. No need to download. Using file: '{0}'", file);
                    return file;
                }
            }

            return null;
        }


        private string DownloadPackage(
            string packageId,
            IVersion version,
            Uri feedUri,
            ICredentials feedCredentials,
            string cacheDirectory,
            int maxDownloadAttempts,
            TimeSpan downloadAttemptBackoff)
        {
            Log.Info("Downloading GitHub package {0} {1} from feed: '{2}'", packageId, version, feedUri);
            Log.VerboseFormat("Downloaded package will be stored in: '{0}'", cacheDirectory);
            fileSystem.EnsureDirectoryExists(cacheDirectory);
            fileSystem.EnsureDiskHasEnoughFreeSpace(cacheDirectory);

            SplitPackageId(packageId, out string owner, out string repository);
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repository))
            {
                throw new InvalidOperationException("Invalid PackageId for GitHub feed. Expecting format `<owner>/<repo>`");
            }

            var fullPathToDownloadTo = GetFilePathToDownloadPackageTo(cacheDirectory, packageId, version.ToString());

            var tempPath = Path.GetTempFileName();
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            var uri = $"https://api.github.com/repos/{Uri.EscapeUriString(owner)}/{Uri.EscapeUriString(repository)}/zipball/{Uri.EscapeUriString(version.OriginalString)}";

            using (var client = new WebClient())
            {
                client.CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
                client.Headers.Set(HttpRequestHeader.UserAgent, "Sample");
                client.Credentials = feedCredentials;
                client.DownloadFile(uri, tempPath);
                
            }
            ZipTools.DeNestContents(tempPath, fullPathToDownloadTo);
            return fullPathToDownloadTo;
        }


        string GetBaseFileName(string packageId, string version)
        {
            return $"{packageId.Replace(OwnerRepoSeperator, '+')}{PhysicalPackageMetadata.DEFAULT_VERSION_DELIMITER}{version}{Extension}";
        }
        string GetFilePathToDownloadPackageTo(string cacheDirectory, string packageId, string version)
        {
            var name = $"{GetBaseFileName(packageId, version)}{CacheBustSeperator}{Guid.NewGuid()}";
            return Path.Combine(cacheDirectory, name);
        }

        const char OwnerRepoSeperator = '/';
        const char CacheBustSeperator = '-';
        
        //"https://api.github.com/repos/OctopusDeploy/Calamari/zipball/4.1.1",
    }
}