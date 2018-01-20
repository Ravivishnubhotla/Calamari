using System;
using System.Net;
using Octopus.Versioning;
using Octopus.Versioning.Metadata;

namespace Calamari.Integration.Packages.Download
{
    /// <summary>
    /// This class knows how to interpret a package id and request a download
    /// from a specific downloader implementation. 
    /// </summary>
    public class PackageDownloaderStrategy : IPackageDownloader
    {
        public void DownloadPackage(
            string packageId,
            IVersion version,
            string feedId,
            Uri feedUri,
            FeedType feedType,
            ICredentials feedCredentials,
            bool forcePackageDownload,
            int maxDownloadAttempts,
            TimeSpan downloadAttemptBackoff,
            out string downloadedTo,
            out string hash,
            out long size)
        {


            GetDownloader(feedType)
                .DownloadPackage(
                    packageId,
                    version,
                    feedId,
                    feedUri,
                    feedType, //TODO: Remove... Dont think we need the type at this point.
                    feedCredentials,
                    forcePackageDownload,
                    maxDownloadAttempts,
                    downloadAttemptBackoff,
                    out downloadedTo,
                    out hash,
                    out size);
        }

        IPackageDownloader GetDownloader(FeedType feedType)
        {
            switch (feedType)
            {
                case FeedType.Maven:
                    return new MavenPackageDownloader();
                case FeedType.NuGet:
                    return new NuGetPackageDownloader();
                case FeedType.GitHub:
                    return new GitHubPackageDownloader();
                default:
                    throw new NotImplementedException($"Feed type {feedType} does not support downloading on the Target.");
            }
        }
    }
}