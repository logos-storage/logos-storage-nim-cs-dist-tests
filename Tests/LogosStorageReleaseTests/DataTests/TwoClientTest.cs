using LogosStorageClient;
using StoragePlugin;
using LogosStorageTests;
using NUnit.Framework;
using Utils;

namespace LogosStorageReleaseTests.DataTests
{
    [TestFixture]
    public class TwoClientTests : LogosStorageDistTest
    {
        [Test]
        public void TwoClientTest()
        {
            var uploader = StartLogosStorage(s => s.WithName("Uploader"));
            var downloader = StartLogosStorage(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        [Ignore("Location selection is currently unavailable.")]
        public void TwoClientsTwoLocationsTest()
        {
            var locations = Ci.GetKnownLocations();
            if (locations.NumberOfLocations < 2)
            {
                Assert.Inconclusive("Two-locations test requires 2 nodes to be available in the cluster.");
                return;
            }

            var uploader = Ci.StartStorageNode(s => s.WithName("Uploader").At(locations.Get(0)));
            var downloader = Ci.StartStorageNode(s => s.WithName("Downloader").WithBootstrapNode(uploader).At(locations.Get(1)));

            PerformTwoClientTest(uploader, downloader);
        }

        private void PerformTwoClientTest(IStorageNode uploader, IStorageNode downloader)
        {
            PerformTwoClientTest(uploader, downloader, 10.MB());
        }

        private void PerformTwoClientTest(IStorageNode uploader, IStorageNode downloader, ByteSize size)
        {
            var testFile = GenerateTestFile(size);

            var contentId = uploader.UploadFile(testFile);
            AssertNodesContainFile(contentId, uploader);

            var downloadedFile = downloader.DownloadContent(contentId);
            AssertNodesContainFile(contentId, uploader, downloader);

            testFile.AssertIsEqual(downloadedFile);
            CheckLogForErrors(uploader, downloader);
        }
    }
}
